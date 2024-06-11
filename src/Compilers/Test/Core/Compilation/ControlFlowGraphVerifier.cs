// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class ControlFlowGraphVerifier
    {
        public static (ControlFlowGraph graph, ISymbol associatedSymbol) GetControlFlowGraph(SyntaxNode syntaxNode, SemanticModel model)
        {
            IOperation operationRoot = model.GetOperation(syntaxNode);

            // Workaround for unit tests designed to work on IBlockOperation with ConstructorBodyOperation/MethodBodyOperation parent.
            operationRoot = operationRoot.Kind == OperationKind.Block &&
                (operationRoot.Parent?.Kind == OperationKind.ConstructorBody ||
                operationRoot.Parent?.Kind == OperationKind.MethodBody) ?
                    operationRoot.Parent :
                    operationRoot;

            TestOperationVisitor.VerifySubTree(operationRoot);

            ControlFlowGraph graph;
            switch (operationRoot)
            {
                case IBlockOperation blockOperation:
                    graph = ControlFlowGraph.Create(blockOperation);
                    break;

                case IMethodBodyOperation methodBodyOperation:
                    graph = ControlFlowGraph.Create(methodBodyOperation);
                    break;

                case IConstructorBodyOperation constructorBodyOperation:
                    graph = ControlFlowGraph.Create(constructorBodyOperation);
                    break;

                case IFieldInitializerOperation fieldInitializerOperation:
                    graph = ControlFlowGraph.Create(fieldInitializerOperation);
                    break;

                case IPropertyInitializerOperation propertyInitializerOperation:
                    graph = ControlFlowGraph.Create(propertyInitializerOperation);
                    break;

                case IParameterInitializerOperation parameterInitializerOperation:
                    graph = ControlFlowGraph.Create(parameterInitializerOperation);
                    break;

                case IAttributeOperation attributeOperation:
                    graph = ControlFlowGraph.Create(attributeOperation);
                    break;

                default:
                    return default;
            }

            Assert.NotNull(graph);
            Assert.Same(operationRoot, graph.OriginalOperation);
            var declaredSymbol = model.GetDeclaredSymbol(operationRoot.Syntax);
            return (graph, declaredSymbol);
        }

        public static void VerifyGraph(Compilation compilation, string expectedFlowGraph, ControlFlowGraph graph, ISymbol associatedSymbol)
        {
            var actualFlowGraph = GetFlowGraph(compilation, graph, associatedSymbol);
            OperationTreeVerifier.Verify(expectedFlowGraph, actualFlowGraph);

            // Basic block reachability analysis verification using a test-only dataflow analyzer
            // that uses the dataflow analysis engine linked from the Workspaces layer.
            // This provides test coverage for Workspace layer dataflow analysis engine
            // for all ControlFlowGraphs created in compiler layer's flow analysis unit tests.
            var reachabilityVector = BasicBlockReachabilityDataFlowAnalyzer.Run(graph);
            for (int i = 0; i < graph.Blocks.Length; i++)
            {
                Assert.Equal(graph.Blocks[i].IsReachable, reachabilityVector[i]);
            }
        }

        public static string GetFlowGraph(Compilation compilation, ControlFlowGraph graph, ISymbol associatedSymbol)
        {
            var pooledBuilder = PooledObjects.PooledStringBuilder.GetInstance();
            var stringBuilder = pooledBuilder.Builder;

            GetFlowGraph(pooledBuilder.Builder, compilation, graph, enclosing: null, idSuffix: "", indent: 0, associatedSymbol);

            return pooledBuilder.ToStringAndFree();
        }

        private static void GetFlowGraph(System.Text.StringBuilder stringBuilder, Compilation compilation, ControlFlowGraph graph,
                                         ControlFlowRegion enclosing, string idSuffix, int indent, ISymbol associatedSymbol)
        {
            ImmutableArray<BasicBlock> blocks = graph.Blocks;

            var visitor = TestOperationVisitor.Singleton;
            ControlFlowRegion currentRegion = graph.Root;
            bool lastPrintedBlockIsInCurrentRegion = true;
            PooledDictionary<ControlFlowRegion, int> regionMap = buildRegionMap();
            var localFunctionsMap = PooledDictionary<IMethodSymbol, ControlFlowGraph>.GetInstance();
            var anonymousFunctionsMap = PooledDictionary<IFlowAnonymousFunctionOperation, ControlFlowGraph>.GetInstance();
            var referencedLocalsAndMethods = PooledHashSet<ISymbol>.GetInstance();
            var referencedCaptureIds = PooledHashSet<CaptureId>.GetInstance();

            for (int i = 0; i < blocks.Length; i++)
            {
                var block = blocks[i];

                Assert.Equal(i, block.Ordinal);

                switch (block.Kind)
                {
                    case BasicBlockKind.Block:
                        Assert.NotEqual(0, i);
                        Assert.NotEqual(blocks.Length - 1, i);
                        break;

                    case BasicBlockKind.Entry:
                        Assert.Equal(0, i);
                        Assert.Empty(block.Operations);
                        Assert.Empty(block.Predecessors);
                        Assert.Null(block.BranchValue);
                        Assert.NotNull(block.FallThroughSuccessor);
                        Assert.NotNull(block.FallThroughSuccessor.Destination);
                        Assert.Null(block.ConditionalSuccessor);
                        Assert.Same(graph.Root, currentRegion);
                        Assert.Same(currentRegion, block.EnclosingRegion);
                        Assert.Equal(0, currentRegion.FirstBlockOrdinal);
                        Assert.Same(enclosing, currentRegion.EnclosingRegion);
                        Assert.Null(currentRegion.ExceptionType);
                        Assert.Empty(currentRegion.Locals);
                        Assert.Empty(currentRegion.LocalFunctions);
                        Assert.Empty(currentRegion.CaptureIds);
                        Assert.Equal(ControlFlowRegionKind.Root, currentRegion.Kind);
                        Assert.True(block.IsReachable);
                        break;

                    case BasicBlockKind.Exit:
                        Assert.Equal(blocks.Length - 1, i);
                        Assert.Empty(block.Operations);
                        Assert.Null(block.FallThroughSuccessor);
                        Assert.Null(block.ConditionalSuccessor);
                        Assert.Null(block.BranchValue);
                        Assert.Same(graph.Root, currentRegion);
                        Assert.Same(currentRegion, block.EnclosingRegion);
                        Assert.Equal(i, currentRegion.LastBlockOrdinal);
                        break;

                    default:
                        Assert.False(true, $"Unexpected block kind {block.Kind}");
                        break;
                }

                if (block.EnclosingRegion != currentRegion)
                {
                    enterRegions(block.EnclosingRegion, block.Ordinal);
                }

                if (!lastPrintedBlockIsInCurrentRegion)
                {
                    stringBuilder.AppendLine();
                }

                appendLine($"Block[{getBlockId(block)}] - {block.Kind}{(block.IsReachable ? "" : " [UnReachable]")}");

                var predecessors = block.Predecessors;

                if (!predecessors.IsEmpty)
                {
                    appendIndent();
                    stringBuilder.Append("    Predecessors:");
                    int previousPredecessorOrdinal = -1;
                    for (var predecessorIndex = 0; predecessorIndex < predecessors.Length; predecessorIndex++)
                    {
                        var predecessorBranch = predecessors[predecessorIndex];
                        Assert.Same(block, predecessorBranch.Destination);
                        var predecessor = predecessorBranch.Source;
                        Assert.True(previousPredecessorOrdinal < predecessor.Ordinal);
                        previousPredecessorOrdinal = predecessor.Ordinal;
                        Assert.Same(blocks[predecessor.Ordinal], predecessor);

                        if (predecessorBranch.IsConditionalSuccessor)
                        {
                            Assert.Same(predecessor.ConditionalSuccessor, predecessorBranch);
                            Assert.NotEqual(ControlFlowConditionKind.None, predecessor.ConditionKind);
                        }
                        else
                        {
                            Assert.Same(predecessor.FallThroughSuccessor, predecessorBranch);
                        }

                        stringBuilder.Append($" [{getBlockId(predecessor)}");

                        if (predecessorIndex < predecessors.Length - 1 && predecessors[predecessorIndex + 1].Source == predecessor)
                        {
                            // Multiple branches from same predecessor - one must be conditional and other fall through.
                            Assert.True(predecessorBranch.IsConditionalSuccessor);
                            predecessorIndex++;
                            predecessorBranch = predecessors[predecessorIndex];
                            Assert.Same(predecessor.FallThroughSuccessor, predecessorBranch);
                            Assert.False(predecessorBranch.IsConditionalSuccessor);

                            stringBuilder.Append("*2");
                        }

                        stringBuilder.Append("]");
                    }

                    stringBuilder.AppendLine();
                }
                else if (block.Kind != BasicBlockKind.Entry)
                {
                    appendLine("    Predecessors (0)");
                }

                var statements = block.Operations;
                appendLine($"    Statements ({statements.Length})");
                foreach (var statement in statements)
                {
                    validateRoot(statement);
                    stringBuilder.AppendLine(getOperationTree(statement));
                }

                ControlFlowBranch conditionalBranch = block.ConditionalSuccessor;

                if (block.ConditionKind != ControlFlowConditionKind.None)
                {
                    Assert.NotNull(conditionalBranch);
                    Assert.True(conditionalBranch.IsConditionalSuccessor);

                    Assert.Same(block, conditionalBranch.Source);
                    if (conditionalBranch.Destination != null)
                    {
                        Assert.Same(blocks[conditionalBranch.Destination.Ordinal], conditionalBranch.Destination);
                    }

                    Assert.NotEqual(ControlFlowBranchSemantics.Return, conditionalBranch.Semantics);
                    Assert.NotEqual(ControlFlowBranchSemantics.Throw, conditionalBranch.Semantics);
                    Assert.NotEqual(ControlFlowBranchSemantics.StructuredExceptionHandling, conditionalBranch.Semantics);

                    Assert.True(block.ConditionKind == ControlFlowConditionKind.WhenTrue || block.ConditionKind == ControlFlowConditionKind.WhenFalse);
                    string jumpIfTrue = block.ConditionKind == ControlFlowConditionKind.WhenTrue ? "True" : "False";
                    appendLine($"    Jump if {jumpIfTrue} ({conditionalBranch.Semantics}) to Block[{getDestinationString(ref conditionalBranch)}]");

                    IOperation value = block.BranchValue;
                    Assert.NotNull(value);
                    validateRoot(value);
                    stringBuilder.Append(getOperationTree(value));
                    validateBranch(block, conditionalBranch);
                    stringBuilder.AppendLine();
                }
                else
                {
                    Assert.Null(conditionalBranch);
                    Assert.Equal(ControlFlowConditionKind.None, block.ConditionKind);
                }

                ControlFlowBranch nextBranch = block.FallThroughSuccessor;

                if (block.Kind == BasicBlockKind.Exit)
                {
                    Assert.Null(nextBranch);
                    Assert.Null(block.BranchValue);
                }
                else
                {
                    Assert.NotNull(nextBranch);
                    Assert.False(nextBranch.IsConditionalSuccessor);

                    Assert.Same(block, nextBranch.Source);
                    if (nextBranch.Destination != null)
                    {
                        Assert.Same(blocks[nextBranch.Destination.Ordinal], nextBranch.Destination);
                    }

                    if (nextBranch.Semantics == ControlFlowBranchSemantics.StructuredExceptionHandling)
                    {
                        Assert.Null(nextBranch.Destination);
                        Assert.Equal(block.EnclosingRegion.LastBlockOrdinal, block.Ordinal);
                        Assert.True(block.EnclosingRegion.Kind == ControlFlowRegionKind.Filter || block.EnclosingRegion.Kind == ControlFlowRegionKind.Finally);
                    }

                    appendLine($"    Next ({nextBranch.Semantics}) Block[{getDestinationString(ref nextBranch)}]");
                    IOperation value = block.ConditionKind == ControlFlowConditionKind.None ? block.BranchValue : null;

                    if (value != null)
                    {
                        Assert.True(ControlFlowBranchSemantics.Return == nextBranch.Semantics || ControlFlowBranchSemantics.Throw == nextBranch.Semantics);
                        validateRoot(value);
                        stringBuilder.Append(getOperationTree(value));
                    }
                    else
                    {
                        Assert.NotEqual(ControlFlowBranchSemantics.Return, nextBranch.Semantics);
                        Assert.NotEqual(ControlFlowBranchSemantics.Throw, nextBranch.Semantics);
                    }

                    validateBranch(block, nextBranch);
                }

                if (currentRegion.LastBlockOrdinal == block.Ordinal && i != blocks.Length - 1)
                {
                    leaveRegions(block.EnclosingRegion, block.Ordinal);
                }
                else
                {
                    lastPrintedBlockIsInCurrentRegion = true;
                }
            }

            foreach (IMethodSymbol m in graph.LocalFunctions)
            {
                ControlFlowGraph g = localFunctionsMap[m];
                Assert.Same(g, graph.GetLocalFunctionControlFlowGraph(m));
                Assert.Same(g, graph.GetLocalFunctionControlFlowGraphInScope(m));
                Assert.Same(graph, g.Parent);
            }

            Assert.Equal(graph.LocalFunctions.Length, localFunctionsMap.Count);

            foreach (KeyValuePair<IFlowAnonymousFunctionOperation, ControlFlowGraph> pair in anonymousFunctionsMap)
            {
                Assert.Same(pair.Value, graph.GetAnonymousFunctionControlFlowGraph(pair.Key));
                Assert.Same(pair.Value, graph.GetAnonymousFunctionControlFlowGraphInScope(pair.Key));
                Assert.Same(graph, pair.Value.Parent);
            }

            bool doCaptureVerification = true;

            if (graph.OriginalOperation.Language == LanguageNames.VisualBasic)
            {
                var model = compilation.GetSemanticModel(graph.OriginalOperation.Syntax.SyntaxTree);
                if (model.GetDiagnostics(graph.OriginalOperation.Syntax.Span).
                        Any(d => d.Code == (int)VisualBasic.ERRID.ERR_GotoIntoWith ||
                                 d.Code == (int)VisualBasic.ERRID.ERR_GotoIntoFor ||
                                 d.Code == (int)VisualBasic.ERRID.ERR_GotoIntoSyncLock ||
                                 d.Code == (int)VisualBasic.ERRID.ERR_GotoIntoUsing))
                {
                    // Invalid branches like that are often causing reports about
                    // using captures before they are initialized.
                    doCaptureVerification = false;
                }
            }

            Func<string> finalGraph = () => stringBuilder.ToString();
            if (doCaptureVerification)
            {
                verifyCaptures(finalGraph);
            }

            foreach (var block in blocks)
            {
                validateLifetimeOfReferences(block, finalGraph);
            }

            regionMap.Free();
            localFunctionsMap.Free();
            anonymousFunctionsMap.Free();
            referencedLocalsAndMethods.Free();
            referencedCaptureIds.Free();
            return;

            void verifyCaptures(Func<string> finalGraph)
            {
                var longLivedIds = PooledHashSet<CaptureId>.GetInstance();
                var referencedIds = PooledHashSet<CaptureId>.GetInstance();
                var entryStates = ArrayBuilder<PooledHashSet<CaptureId>>.GetInstance(blocks.Length, fillWithValue: null);
                var regions = ArrayBuilder<ControlFlowRegion>.GetInstance();

                for (int i = 1; i < blocks.Length - 1; i++)
                {
                    BasicBlock block = blocks[i];
                    PooledHashSet<CaptureId> currentState = entryStates[i] ?? PooledHashSet<CaptureId>.GetInstance();
                    entryStates[i] = null;

                    foreach (ControlFlowBranch predecessor in block.Predecessors)
                    {
                        if (predecessor.Source.Ordinal >= i)
                        {
                            foreach (ControlFlowRegion region in predecessor.EnteringRegions)
                            {
                                if (region.FirstBlockOrdinal != block.Ordinal)
                                {
                                    foreach (CaptureId id in region.CaptureIds)
                                    {
                                        AssertTrueWithGraph(currentState.Contains(id), $"Backward branch from [{getBlockId(predecessor.Source)}] to [{getBlockId(block)}] before capture [{id.Value}] is initialized.", finalGraph);
                                    }
                                }
                            }
                        }
                    }

                    for (var j = 0; j < block.Operations.Length; j++)
                    {
                        var operation = block.Operations[j];
                        if (operation is IFlowCaptureOperation capture)
                        {
                            assertCaptureReferences(currentState, capture.Value, block, j, longLivedIds, referencedIds, finalGraph);
                            AssertTrueWithGraph(currentState.Add(capture.Id), $"Operation [{j}] in [{getBlockId(block)}] re-initialized capture [{capture.Id.Value}]", finalGraph);
                        }
                        else
                        {
                            assertCaptureReferences(currentState, operation, block, j, longLivedIds, referencedIds, finalGraph);
                        }
                    }

                    if (block.BranchValue != null)
                    {
                        assertCaptureReferences(currentState, block.BranchValue, block, block.Operations.Length, longLivedIds, referencedIds, finalGraph);

                        if (block.ConditionalSuccessor != null)
                        {
                            adjustEntryStateForDestination(entryStates, block.ConditionalSuccessor, currentState);
                        }
                    }

                    adjustEntryStateForDestination(entryStates, block.FallThroughSuccessor, currentState);

                    if (blocks[i + 1].Predecessors.IsEmpty)
                    {
                        adjustAndGetEntryState(entryStates, blocks[i + 1], currentState);
                    }

                    verifyLeftRegions(block, longLivedIds, referencedIds, regions, finalGraph);

                    currentState.Free();
                }

                foreach (PooledHashSet<CaptureId> state in entryStates)
                {
                    state?.Free();
                }

                entryStates.Free();
                longLivedIds.Free();
                referencedIds.Free();
                regions.Free();
            }

            void verifyLeftRegions(BasicBlock block, PooledHashSet<CaptureId> longLivedIds, PooledHashSet<CaptureId> referencedIds, ArrayBuilder<ControlFlowRegion> regions, Func<string> finalGraph)
            {
                regions.Clear();

                {
                    ControlFlowRegion region = block.EnclosingRegion;

                    while (region.LastBlockOrdinal == block.Ordinal)
                    {
                        regions.Add(region);
                        region = region.EnclosingRegion;
                    }
                }

                if (block.ConditionalSuccessor != null && block.ConditionalSuccessor.LeavingRegions.Length > regions.Count)
                {
                    regions.Clear();
                    regions.AddRange(block.ConditionalSuccessor.LeavingRegions);
                }

                if (block.FallThroughSuccessor.LeavingRegions.Length > regions.Count)
                {
                    regions.Clear();
                    regions.AddRange(block.FallThroughSuccessor.LeavingRegions);
                }

                if (regions.Count > 0)
                {
                    IOperation lastOperation = null;
                    for (int i = block.Ordinal; i > 0 && lastOperation == null; i--)
                    {
                        lastOperation = blocks[i].BranchValue ?? blocks[i].Operations.LastOrDefault();
                    }

                    var referencedInLastOperation = PooledHashSet<CaptureId>.GetInstance();

                    if (lastOperation != null)
                    {
                        foreach (IFlowCaptureReferenceOperation reference in lastOperation.DescendantsAndSelf().OfType<IFlowCaptureReferenceOperation>())
                        {
                            referencedInLastOperation.Add(reference.Id);
                        }
                    }

                    foreach (ControlFlowRegion region in regions)
                    {
                        foreach (CaptureId id in region.CaptureIds)
                        {
                            if (referencedInLastOperation.Contains(id) ||
                                longLivedIds.Contains(id) ||
                                isCSharpEmptyObjectInitializerCapture(region, block, id) ||
                                isWithStatementTargetCapture(region, block, id) ||
                                isSwitchTargetCapture(region, block, id) ||
                                isForEachEnumeratorCapture(region, block, id) ||
                                isConditionalXMLAccessReceiverCapture(region, block, id) ||
                                isConditionalAccessCaptureUsedAfterNullCheck(lastOperation, region, block, id) ||
                                (referencedIds.Contains(id) && isAggregateGroupCapture(lastOperation, region, block, id)))
                            {
                                continue;
                            }

                            if (region.LastBlockOrdinal != block.Ordinal && referencedIds.Contains(id))
                            {
                                continue;
                            }

                            IFlowCaptureReferenceOperation[] referencesAfter = getFlowCaptureReferenceOperationsInRegion(region, block.Ordinal + 1).Where(r => r.Id.Equals(id)).ToArray();

                            AssertTrueWithGraph(referencesAfter.Length > 0 &&
                                        referencesAfter.All(r => isLongLivedCaptureReferenceSyntax(r.Syntax)),
                                $"Capture [{id.Value}] is not used in region [{getRegionId(region)}] before leaving it after block [{getBlockId(block)}]", finalGraph);
                        }
                    }

                    referencedInLastOperation.Free();
                }
            }

            bool isCSharpEmptyObjectInitializerCapture(ControlFlowRegion region, BasicBlock block, CaptureId id)
            {
                if (graph.OriginalOperation.Language != LanguageNames.CSharp)
                {
                    return false;
                }

                foreach (IFlowCaptureOperation candidate in getFlowCaptureOperationsFromBlocksInRegion(region, block.Ordinal))
                {
                    if (candidate.Id.Equals(id))
                    {
                        CSharpSyntaxNode syntax = applyParenthesizedOrNullSuppressionIfAnyCS((CSharpSyntaxNode)candidate.Syntax);
                        CSharpSyntaxNode parent = syntax;

                        do
                        {
                            parent = parent.Parent;
                        }
                        while (parent != null && parent.Kind() != CSharp.SyntaxKind.SimpleAssignmentExpression);

                        if (parent is AssignmentExpressionSyntax assignment &&
                            assignment.Parent?.Kind() == CSharp.SyntaxKind.ObjectInitializerExpression &&
                            assignment.Left.DescendantNodesAndSelf().Contains(syntax) &&
                            assignment.Right is InitializerExpressionSyntax initializer &&
                            initializer.Kind() == CSharp.SyntaxKind.ObjectInitializerExpression &&
                            !initializer.Expressions.Any())
                        {
                            return true;
                        }

                        break;
                    }
                }

                return false;
            }

            bool isWithStatementTargetCapture(ControlFlowRegion region, BasicBlock block, CaptureId id)
            {
                if (graph.OriginalOperation.Language != LanguageNames.VisualBasic)
                {
                    return false;
                }

                foreach (IFlowCaptureOperation candidate in getFlowCaptureOperationsFromBlocksInRegion(region, block.Ordinal))
                {
                    if (candidate.Id.Equals(id))
                    {
                        VisualBasicSyntaxNode syntax = applyParenthesizedIfAnyVB((VisualBasicSyntaxNode)candidate.Syntax);
                        VisualBasicSyntaxNode parent = syntax.Parent;

                        if (parent is WithStatementSyntax with &&
                            with.Expression == syntax)
                        {
                            return true;
                        }

                        break;
                    }
                }

                return false;
            }

            bool isConditionalXMLAccessReceiverCapture(ControlFlowRegion region, BasicBlock block, CaptureId id)
            {
                if (graph.OriginalOperation.Language != LanguageNames.VisualBasic)
                {
                    return false;
                }

                foreach (IFlowCaptureOperation candidate in getFlowCaptureOperationsFromBlocksInRegion(region, block.Ordinal))
                {
                    if (candidate.Id.Equals(id))
                    {
                        VisualBasicSyntaxNode syntax = applyParenthesizedIfAnyVB((VisualBasicSyntaxNode)candidate.Syntax);
                        VisualBasicSyntaxNode parent = syntax.Parent;

                        if (parent is VisualBasic.Syntax.ConditionalAccessExpressionSyntax conditional &&
                            conditional.Expression == syntax &&
                            conditional.WhenNotNull.DescendantNodesAndSelf().
                                Any(n =>
                                         n.IsKind(VisualBasic.SyntaxKind.XmlElementAccessExpression) ||
                                         n.IsKind(VisualBasic.SyntaxKind.XmlDescendantAccessExpression) ||
                                         n.IsKind(VisualBasic.SyntaxKind.XmlAttributeAccessExpression)))
                        {
                            // https://github.com/dotnet/roslyn/issues/27564: It looks like there is a bug in IOperation tree around XmlMemberAccessExpressionSyntax,
                            // a None operation is created and all children are dropped.
                            return true;
                        }

                        break;
                    }
                }

                return false;
            }

            bool isEmptySwitchExpressionResult(IFlowCaptureReferenceOperation reference)
            {
                return reference.Syntax is CSharp.Syntax.SwitchExpressionSyntax switchExpr && switchExpr.Arms.Count == 0;
            }

            bool isSwitchTargetCapture(ControlFlowRegion region, BasicBlock block, CaptureId id)
            {
                foreach (IFlowCaptureOperation candidate in getFlowCaptureOperationsFromBlocksInRegion(region, block.Ordinal))
                {
                    if (candidate.Id.Equals(id))
                    {
                        switch (candidate.Language)
                        {
                            case LanguageNames.CSharp:
                                {
                                    CSharpSyntaxNode syntax = applyParenthesizedOrNullSuppressionIfAnyCS((CSharpSyntaxNode)candidate.Syntax);
                                    if (syntax.Parent is CSharp.Syntax.SwitchStatementSyntax switchStmt && switchStmt.Expression == syntax)
                                    {
                                        return true;
                                    }

                                    if (syntax.Parent is CSharp.Syntax.SwitchExpressionSyntax switchExpr && switchExpr.GoverningExpression == syntax)
                                    {
                                        return true;
                                    }
                                }

                                break;

                            case LanguageNames.VisualBasic:
                                {
                                    VisualBasicSyntaxNode syntax = applyParenthesizedIfAnyVB((VisualBasicSyntaxNode)candidate.Syntax);
                                    if (syntax.Parent is VisualBasic.Syntax.SelectStatementSyntax switchStmt && switchStmt.Expression == syntax)
                                    {
                                        return true;
                                    }
                                }

                                break;
                        }

                        break;
                    }
                }

                return false;
            }

            bool isForEachEnumeratorCapture(ControlFlowRegion region, BasicBlock block, CaptureId id)
            {
                foreach (IFlowCaptureOperation candidate in getFlowCaptureOperationsFromBlocksInRegion(region, block.Ordinal))
                {
                    if (candidate.Id.Equals(id))
                    {
                        switch (candidate.Language)
                        {
                            case LanguageNames.CSharp:
                                {
                                    CSharpSyntaxNode syntax = applyParenthesizedOrNullSuppressionIfAnyCS((CSharpSyntaxNode)candidate.Syntax);
                                    if (syntax.Parent is CSharp.Syntax.CommonForEachStatementSyntax forEach && forEach.Expression == syntax)
                                    {
                                        return true;
                                    }
                                }

                                break;

                            case LanguageNames.VisualBasic:
                                {
                                    VisualBasicSyntaxNode syntax = applyParenthesizedIfAnyVB((VisualBasicSyntaxNode)candidate.Syntax);
                                    if (syntax.Parent is VisualBasic.Syntax.ForEachStatementSyntax forEach && forEach.Expression == syntax)
                                    {
                                        return true;
                                    }
                                }

                                break;
                        }

                        break;
                    }
                }

                return false;
            }

            bool isAggregateGroupCapture(IOperation operation, ControlFlowRegion region, BasicBlock block, CaptureId id)
            {
                if (graph.OriginalOperation.Language != LanguageNames.VisualBasic)
                {
                    return false;
                }

                foreach (IFlowCaptureOperation candidate in getFlowCaptureOperationsFromBlocksInRegion(region, block.Ordinal))
                {
                    if (candidate.Id.Equals(id))
                    {
                        VisualBasicSyntaxNode syntax = applyParenthesizedIfAnyVB((VisualBasicSyntaxNode)candidate.Syntax);

                        foreach (ITranslatedQueryOperation query in operation.DescendantsAndSelf().OfType<ITranslatedQueryOperation>())
                        {
                            if (query.Syntax is VisualBasic.Syntax.QueryExpressionSyntax querySyntax &&
                                querySyntax.Clauses.AsSingleton() is VisualBasic.Syntax.AggregateClauseSyntax aggregate &&
                                aggregate.AggregateKeyword.SpanStart < candidate.Syntax.SpanStart &&
                                aggregate.IntoKeyword.SpanStart > candidate.Syntax.SpanStart &&
                                query.Operation.Kind == OperationKind.AnonymousObjectCreation)
                            {
                                return true;
                            }
                        }

                        break;
                    }
                }

                return false;
            }

            void adjustEntryStateForDestination(ArrayBuilder<PooledHashSet<CaptureId>> entryStates, ControlFlowBranch branch, PooledHashSet<CaptureId> state)
            {
                if (branch.Destination != null)
                {
                    if (branch.Destination.Ordinal > branch.Source.Ordinal)
                    {
                        PooledHashSet<CaptureId> entryState = adjustAndGetEntryState(entryStates, branch.Destination, state);

                        foreach (ControlFlowRegion region in branch.LeavingRegions)
                        {
                            entryState.RemoveAll(region.CaptureIds);
                        }
                    }
                }
                else if (branch.Semantics == ControlFlowBranchSemantics.Throw ||
                         branch.Semantics == ControlFlowBranchSemantics.Rethrow ||
                         branch.Semantics == ControlFlowBranchSemantics.Error ||
                         branch.Semantics == ControlFlowBranchSemantics.StructuredExceptionHandling)
                {
                    ControlFlowRegion region = branch.Source.EnclosingRegion;

                    while (region.Kind != ControlFlowRegionKind.Root)
                    {
                        if (region.Kind == ControlFlowRegionKind.Try && region.EnclosingRegion.Kind == ControlFlowRegionKind.TryAndFinally)
                        {
                            Debug.Assert(region.EnclosingRegion.NestedRegions[1].Kind == ControlFlowRegionKind.Finally);
                            adjustAndGetEntryState(entryStates, blocks[region.EnclosingRegion.NestedRegions[1].FirstBlockOrdinal], state);
                        }

                        region = region.EnclosingRegion;
                    }
                }

                foreach (ControlFlowRegion @finally in branch.FinallyRegions)
                {
                    adjustAndGetEntryState(entryStates, blocks[@finally.FirstBlockOrdinal], state);
                }
            }

            PooledHashSet<CaptureId> adjustAndGetEntryState(ArrayBuilder<PooledHashSet<CaptureId>> entryStates, BasicBlock block, PooledHashSet<CaptureId> state)
            {
                PooledHashSet<CaptureId> entryState = entryStates[block.Ordinal];
                if (entryState == null)
                {
                    entryState = PooledHashSet<CaptureId>.GetInstance();
                    entryState.AddAll(state);
                    entryStates[block.Ordinal] = entryState;
                }
                else
                {
                    entryState.RemoveWhere(id => !state.Contains(id));
                }

                return entryState;
            }

            void assertCaptureReferences(
                PooledHashSet<CaptureId> state, IOperation operation, BasicBlock block, int operationIndex,
                PooledHashSet<CaptureId> longLivedIds, PooledHashSet<CaptureId> referencedIds, Func<string> finalGraph)
            {
                foreach (IFlowCaptureReferenceOperation reference in operation.DescendantsAndSelf().OfType<IFlowCaptureReferenceOperation>())
                {
                    CaptureId id = reference.Id;

                    if (reference.IsInitialization)
                    {
                        AssertTrueWithGraph(state.Add(id), $"Multiple initialization of [{id}]", finalGraph);
                        AssertTrueWithGraph(block.EnclosingRegion.CaptureIds.Contains(id), $"Flow capture initialization [{id}] should come from the containing region.", finalGraph);
                        continue;
                    }

                    referencedIds.Add(id);

                    if (isLongLivedCaptureReference(reference, block.EnclosingRegion))
                    {
                        longLivedIds.Add(id);
                    }

                    AssertTrueWithGraph(state.Contains(id) || isCaptureFromEnclosingGraph(id) || isEmptySwitchExpressionResult(reference),
                        $"Operation [{operationIndex}] in [{getBlockId(block)}] uses not initialized capture [{id.Value}].", finalGraph);

                    // Except for a few specific scenarios, any references to captures should either be long-lived capture references,
                    // or they should come from the enclosing region.
                    if (block.EnclosingRegion.CaptureIds.Contains(id) || longLivedIds.Contains(id))
                    {
                        continue;
                    }

                    if (block.EnclosingRegion.EnclosingRegion.CaptureIds.Contains(id))
                    {
                        AssertTrueWithGraph(
                            isFirstOperandOfDynamicOrUserDefinedLogicalOperator(reference)
                            || isIncrementedNullableForToLoopControlVariable(reference)
                            || isConditionalAccessReceiver(reference)
                            || isCoalesceAssignmentTarget(reference)
                            || isObjectInitializerInitializedObjectTarget(reference)
                            || isInterpolatedStringArgumentCapture(reference)
                            || isInterpolatedStringHandlerCapture(reference),
                            $"Operation [{operationIndex}] in [{getBlockId(block)}] uses capture [{id.Value}] from another region. Should the regions be merged?", finalGraph);
                    }
                    else if (block.EnclosingRegion.EnclosingRegion?.EnclosingRegion.CaptureIds.Contains(id) ?? false)
                    {
                        AssertTrueWithGraph(
                            isInterpolatedStringArgumentCapture(reference)
                            || isInterpolatedStringHandlerCapture(reference),
                            $"Operation [{operationIndex}] in [{getBlockId(block)}] uses capture [{id.Value}] from another region. Should the regions be merged?", finalGraph);
                    }
                    else
                    {
                        AssertTrueWithGraph(false, $"Operation [{operationIndex}] in [{getBlockId(block)}] uses capture [{id.Value}] from another region. Should the regions be merged?", finalGraph);
                    }
                }
            }

            bool isConditionalAccessReceiver(IFlowCaptureReferenceOperation reference)
            {
                SyntaxNode captureReferenceSyntax = reference.Syntax;

                switch (captureReferenceSyntax.Language)
                {
                    case LanguageNames.CSharp:
                        {
                            CSharpSyntaxNode syntax = applyParenthesizedOrNullSuppressionIfAnyCS((CSharpSyntaxNode)captureReferenceSyntax);
                            if (syntax.Parent is CSharp.Syntax.ConditionalAccessExpressionSyntax access &&
                                access.Expression == syntax)
                            {
                                return true;
                            }
                        }
                        break;

                    case LanguageNames.VisualBasic:
                        {
                            VisualBasicSyntaxNode syntax = applyParenthesizedIfAnyVB((VisualBasicSyntaxNode)captureReferenceSyntax);
                            if (syntax.Parent is VisualBasic.Syntax.ConditionalAccessExpressionSyntax access &&
                                access.Expression == syntax)
                            {
                                return true;
                            }
                        }

                        break;
                }

                return false;
            }

            bool isCoalesceAssignmentTarget(IFlowCaptureReferenceOperation reference)
            {
                if (reference.Language != LanguageNames.CSharp)
                {
                    return false;
                }

                CSharpSyntaxNode referenceSyntax = applyParenthesizedOrNullSuppressionIfAnyCS((CSharpSyntaxNode)reference.Syntax);
                return referenceSyntax.Parent is AssignmentExpressionSyntax conditionalAccess &&
                       conditionalAccess.IsKind(CSharp.SyntaxKind.CoalesceAssignmentExpression) &&
                       conditionalAccess.Left == referenceSyntax;
            }

            bool isObjectInitializerInitializedObjectTarget(IFlowCaptureReferenceOperation reference)
            {
                if (reference.Language != LanguageNames.CSharp)
                {
                    return false;
                }

                CSharpSyntaxNode referenceSyntax = applyParenthesizedOrNullSuppressionIfAnyCS((CSharpSyntaxNode)reference.Syntax);
                return referenceSyntax.Parent is CSharp.Syntax.AssignmentExpressionSyntax
                {
                    RawKind: (int)CSharp.SyntaxKind.SimpleAssignmentExpression,
                    Parent: InitializerExpressionSyntax { Parent: CSharp.Syntax.ObjectCreationExpressionSyntax },
                    Left: var left
                } && left == referenceSyntax;
            }

            bool isInterpolatedStringArgumentCapture(IFlowCaptureReferenceOperation reference)
            {
                if (reference.Language != LanguageNames.CSharp)
                {
                    return false;
                }

                IOperation containingArgument = reference;
                do
                {
                    containingArgument = containingArgument.Parent;
                }
                while (containingArgument is not (null or IArgumentOperation));

#pragma warning disable IDE0055 // Fix formatting
                return containingArgument is
                       {
                           Parent: IObjectCreationOperation
                           {
                               Parent: IFlowCaptureOperation,
                               Constructor.ContainingType: INamedTypeSymbol ctorContainingType,
                               Arguments: { Length: >= 3 } arguments,
                               Syntax: CSharpSyntaxNode syntax
                           }
                       }
                       && applyParenthesizedOrNullSuppressionIfAnyCS(syntax) is CSharp.Syntax.InterpolatedStringExpressionSyntax or CSharp.Syntax.BinaryExpressionSyntax
                       && ctorContainingType.GetSymbol().IsInterpolatedStringHandlerType
                       && arguments[0].Value.Type.SpecialType == SpecialType.System_Int32
                       && arguments[1].Value.Type.SpecialType == SpecialType.System_Int32;
#pragma warning restore IDE0055
            }

            bool isInterpolatedStringHandlerCapture(IFlowCaptureReferenceOperation reference)
            {
                if (reference.Language != LanguageNames.CSharp)
                {
                    return false;
                }

#pragma warning disable IDE0055 // Fix formatting
                return reference is
                       {
                           Parent: IInvocationOperation
                           {
                               Instance: { } instance,
                               TargetMethod: { Name: BoundInterpolatedString.AppendFormattedMethod or BoundInterpolatedString.AppendLiteralMethod, ContainingType: INamedTypeSymbol containingType }
                           }
                       }
                       && ReferenceEquals(instance, reference)
                       && containingType.GetSymbol().IsInterpolatedStringHandlerType;
#pragma warning restore IDE0055
            }

            bool isFirstOperandOfDynamicOrUserDefinedLogicalOperator(IFlowCaptureReferenceOperation reference)
            {
                if (reference.Parent is IBinaryOperation binOp)
                {
                    if (binOp.LeftOperand == reference &&
                        (binOp.OperatorKind == Operations.BinaryOperatorKind.And || binOp.OperatorKind == Operations.BinaryOperatorKind.Or) &&
                        (binOp.OperatorMethod != null ||
                         (ITypeSymbolHelpers.IsDynamicType(binOp.Type) &&
                          (ITypeSymbolHelpers.IsDynamicType(binOp.LeftOperand.Type) || ITypeSymbolHelpers.IsDynamicType(binOp.RightOperand.Type)))))
                    {
                        if (reference.Language == LanguageNames.CSharp)
                        {
                            if (binOp.Syntax is CSharp.Syntax.BinaryExpressionSyntax binOpSyntax &&
                                (binOpSyntax.Kind() == CSharp.SyntaxKind.LogicalAndExpression || binOpSyntax.Kind() == CSharp.SyntaxKind.LogicalOrExpression) &&
                                binOpSyntax.Left == applyParenthesizedOrNullSuppressionIfAnyCS((CSharpSyntaxNode)reference.Syntax) &&
                                binOpSyntax.Right == applyParenthesizedOrNullSuppressionIfAnyCS((CSharpSyntaxNode)binOp.RightOperand.Syntax))
                            {
                                return true;
                            }
                        }
                        else if (reference.Language == LanguageNames.VisualBasic)
                        {
                            var referenceSyntax = applyParenthesizedIfAnyVB((VisualBasicSyntaxNode)reference.Syntax);
                            if (binOp.Syntax is VisualBasic.Syntax.BinaryExpressionSyntax binOpSyntax &&
                                (binOpSyntax.Kind() == VisualBasic.SyntaxKind.AndAlsoExpression || binOpSyntax.Kind() == VisualBasic.SyntaxKind.OrElseExpression) &&
                                binOpSyntax.Left == referenceSyntax &&
                                binOpSyntax.Right == applyParenthesizedIfAnyVB((VisualBasicSyntaxNode)binOp.RightOperand.Syntax))
                            {
                                return true;
                            }
                            else if (binOp.Syntax is VisualBasic.Syntax.RangeCaseClauseSyntax range &&
                                binOp.OperatorKind == Operations.BinaryOperatorKind.And &&
                                range.LowerBound == referenceSyntax &&
                                range.UpperBound == applyParenthesizedIfAnyVB((VisualBasicSyntaxNode)binOp.RightOperand.Syntax))
                            {
                                return true;
                            }
                            else if (binOp.Syntax is VisualBasic.Syntax.CaseStatementSyntax caseStmt &&
                                binOp.OperatorKind == Operations.BinaryOperatorKind.Or &&
                                caseStmt.Cases.Count > 1 &&
                                (caseStmt == referenceSyntax || caseStmt.Cases.Contains(referenceSyntax as CaseClauseSyntax)) &&
                                caseStmt.Cases.Contains(applyParenthesizedIfAnyVB((VisualBasicSyntaxNode)binOp.RightOperand.Syntax) as CaseClauseSyntax))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            bool isIncrementedNullableForToLoopControlVariable(IFlowCaptureReferenceOperation reference)
            {
                if (reference.Parent is ISimpleAssignmentOperation assignment &&
                    assignment.IsImplicit &&
                    assignment.Target == reference &&
                    ITypeSymbolHelpers.IsNullableType(reference.Type) &&
                    assignment.Syntax.Parent is VisualBasic.Syntax.ForStatementSyntax forStmt &&
                    assignment.Syntax == forStmt.ControlVariable &&
                    reference.Syntax == assignment.Syntax &&
                    assignment.Value.Syntax == forStmt.StepClause.StepValue)
                {
                    return true;
                }

                return false;
            }

            bool isLongLivedCaptureReference(IFlowCaptureReferenceOperation reference, ControlFlowRegion region)
            {
                if (isLongLivedCaptureReferenceSyntax(reference.Syntax))
                {
                    return true;
                }

                return isCaptureFromEnclosingGraph(reference.Id);
            }

            bool isCaptureFromEnclosingGraph(CaptureId id)
            {
                ControlFlowRegion region = graph.Root.EnclosingRegion;

                while (region != null)
                {
                    if (region.CaptureIds.Contains(id))
                    {
                        return true;
                    }

                    region = region.EnclosingRegion;
                }

                return false;
            }

            bool isConditionalAccessCaptureUsedAfterNullCheck(IOperation operation, ControlFlowRegion region, BasicBlock block, CaptureId id)
            {
                SyntaxNode whenNotNull = null;

                if (operation.Parent == null && operation is IsNullOperation isNull && isNull.Operand.Kind == OperationKind.FlowCaptureReference)
                {
                    switch (isNull.Operand.Language)
                    {
                        case LanguageNames.CSharp:
                            {
                                CSharpSyntaxNode syntax = applyParenthesizedOrNullSuppressionIfAnyCS((CSharpSyntaxNode)isNull.Operand.Syntax);
                                if (syntax.Parent is CSharp.Syntax.ConditionalAccessExpressionSyntax access &&
                                    access.Expression == syntax)
                                {
                                    whenNotNull = access.WhenNotNull;
                                }
                            }
                            break;

                        case LanguageNames.VisualBasic:
                            {
                                VisualBasicSyntaxNode syntax = applyParenthesizedIfAnyVB((VisualBasicSyntaxNode)isNull.Operand.Syntax);
                                if (syntax.Parent is VisualBasic.Syntax.ConditionalAccessExpressionSyntax access &&
                                    access.Expression == syntax)
                                {
                                    whenNotNull = access.WhenNotNull;
                                }
                            }

                            break;
                    }
                }

                if (whenNotNull == null)
                {
                    return false;
                }

                foreach (IFlowCaptureOperation candidate in getFlowCaptureOperationsFromBlocksInRegion(region, region.LastBlockOrdinal))
                {
                    if (candidate.Id.Equals(id))
                    {
                        if (whenNotNull.Contains(candidate.Syntax))
                        {
                            return true;
                        }

                        break;
                    }
                }

                return false;
            }

            bool isLongLivedCaptureReferenceSyntax(SyntaxNode captureReferenceSyntax)
            {
                switch (captureReferenceSyntax.Language)
                {
                    case LanguageNames.CSharp:
                        {
                            var syntax = (CSharpSyntaxNode)captureReferenceSyntax;
                            switch (syntax.Kind())
                            {
                                case CSharp.SyntaxKind.ObjectCreationExpression:
                                case CSharp.SyntaxKind.ImplicitObjectCreationExpression:
                                    if (((CSharp.Syntax.BaseObjectCreationExpressionSyntax)syntax).Initializer?.Expressions.Any() == true)
                                    {
                                        return true;
                                    }
                                    break;
                                case CSharp.SyntaxKind.CollectionExpression:
                                    if (((CSharp.Syntax.CollectionExpressionSyntax)syntax).Elements.Any())
                                    {
                                        return true;
                                    }
                                    break;
                            }

                            if (syntax.Parent is CSharp.Syntax.WithExpressionSyntax withExpr
                                && withExpr.Initializer.Expressions.Any()
                                && withExpr.Expression == (object)syntax)
                            {
                                return true;
                            }

                            syntax = applyParenthesizedOrNullSuppressionIfAnyCS(syntax);

                            if (syntax.Parent?.Parent is CSharp.Syntax.UsingStatementSyntax usingStmt &&
                                usingStmt.Declaration == syntax.Parent)
                            {
                                return true;
                            }

                            CSharpSyntaxNode parent = syntax.Parent;

                            switch (parent?.Kind())
                            {
                                case CSharp.SyntaxKind.ForEachStatement:
                                case CSharp.SyntaxKind.ForEachVariableStatement:
                                    if (((CommonForEachStatementSyntax)parent).Expression == syntax)
                                    {
                                        return true;
                                    }
                                    break;

                                case CSharp.SyntaxKind.Argument:
                                    if ((parent = parent.Parent)?.Kind() == CSharp.SyntaxKind.BracketedArgumentList &&
                                        (parent = parent.Parent)?.Kind() == CSharp.SyntaxKind.ImplicitElementAccess &&
                                        parent.Parent is AssignmentExpressionSyntax assignment && assignment.Kind() == CSharp.SyntaxKind.SimpleAssignmentExpression &&
                                        assignment.Left == parent &&
                                        assignment.Parent?.Kind() == CSharp.SyntaxKind.ObjectInitializerExpression &&
                                        (assignment.Right.Kind() == CSharp.SyntaxKind.CollectionInitializerExpression ||
                                        assignment.Right.Kind() == CSharp.SyntaxKind.ObjectInitializerExpression))
                                    {
                                        return true;
                                    }
                                    break;

                                case CSharp.SyntaxKind.LockStatement:
                                    if (((LockStatementSyntax)syntax.Parent).Expression == syntax)
                                    {
                                        return true;
                                    }
                                    break;

                                case CSharp.SyntaxKind.UsingStatement:
                                    if (((CSharp.Syntax.UsingStatementSyntax)syntax.Parent).Expression == syntax)
                                    {
                                        return true;
                                    }
                                    break;

                                case CSharp.SyntaxKind.SwitchStatement:
                                    if (((CSharp.Syntax.SwitchStatementSyntax)syntax.Parent).Expression == syntax)
                                    {
                                        return true;
                                    }
                                    break;

                                case CSharp.SyntaxKind.SwitchExpression:
                                    if (((CSharp.Syntax.SwitchExpressionSyntax)syntax.Parent).GoverningExpression == syntax)
                                    {
                                        return true;
                                    }
                                    break;

                                case CSharp.SyntaxKind.CoalesceAssignmentExpression:
                                    if (((AssignmentExpressionSyntax)syntax.Parent).Left == syntax)
                                    {
                                        return true;
                                    }
                                    break;
                            }
                        }

                        break;

                    case LanguageNames.VisualBasic:
                        {
                            VisualBasicSyntaxNode syntax = applyParenthesizedIfAnyVB((VisualBasicSyntaxNode)captureReferenceSyntax);

                            switch (syntax.Kind())
                            {
                                case VisualBasic.SyntaxKind.ForStatement:
                                case VisualBasic.SyntaxKind.ForBlock:
                                    return true;

                                case VisualBasic.SyntaxKind.ObjectCreationExpression:
                                    var objCreation = (VisualBasic.Syntax.ObjectCreationExpressionSyntax)syntax;
                                    if ((objCreation.Initializer is VisualBasic.Syntax.ObjectMemberInitializerSyntax memberInit && memberInit.Initializers.Any()) ||
                                        (objCreation.Initializer is VisualBasic.Syntax.ObjectCollectionInitializerSyntax collectionInit && collectionInit.Initializer.Initializers.Any()))
                                    {
                                        return true;
                                    }
                                    break;
                            }

                            VisualBasicSyntaxNode parent = syntax.Parent;
                            switch (parent?.Kind())
                            {
                                case VisualBasic.SyntaxKind.ForEachStatement:
                                    if (((VisualBasic.Syntax.ForEachStatementSyntax)parent).Expression == syntax)
                                    {
                                        return true;
                                    }
                                    break;

                                case VisualBasic.SyntaxKind.ForStatement:
                                    if (((VisualBasic.Syntax.ForStatementSyntax)parent).ToValue == syntax)
                                    {
                                        return true;
                                    }
                                    break;

                                case VisualBasic.SyntaxKind.ForStepClause:
                                    if (((ForStepClauseSyntax)parent).StepValue == syntax)
                                    {
                                        return true;
                                    }
                                    break;

                                case VisualBasic.SyntaxKind.SyncLockStatement:
                                    if (((VisualBasic.Syntax.SyncLockStatementSyntax)parent).Expression == syntax)
                                    {
                                        return true;
                                    }
                                    break;

                                case VisualBasic.SyntaxKind.UsingStatement:
                                    if (((VisualBasic.Syntax.UsingStatementSyntax)parent).Expression == syntax)
                                    {
                                        return true;
                                    }
                                    break;

                                case VisualBasic.SyntaxKind.WithStatement:
                                    if (((VisualBasic.Syntax.WithStatementSyntax)parent).Expression == syntax)
                                    {
                                        return true;
                                    }
                                    break;

                                case VisualBasic.SyntaxKind.SelectStatement:
                                    if (((VisualBasic.Syntax.SelectStatementSyntax)parent).Expression == syntax)
                                    {
                                        return true;
                                    }
                                    break;
                            }
                        }

                        break;
                }

                return false;
            }

            CSharpSyntaxNode applyParenthesizedOrNullSuppressionIfAnyCS(CSharpSyntaxNode syntax)
            {
                while (syntax.Parent is CSharp.Syntax.ParenthesizedExpressionSyntax or
                                        PostfixUnaryExpressionSyntax { OperatorToken: { RawKind: (int)CSharp.SyntaxKind.ExclamationToken } })
                {
                    syntax = syntax.Parent;
                }

                return syntax;
            }

            VisualBasicSyntaxNode applyParenthesizedIfAnyVB(VisualBasicSyntaxNode syntax)
            {
                while (syntax.Parent?.Kind() == VisualBasic.SyntaxKind.ParenthesizedExpression)
                {
                    syntax = syntax.Parent;
                }

                return syntax;
            }

            IEnumerable<IFlowCaptureOperation> getFlowCaptureOperationsFromBlocksInRegion(ControlFlowRegion region, int lastBlockOrdinal)
            {
                Debug.Assert(lastBlockOrdinal <= region.LastBlockOrdinal);
                for (int i = lastBlockOrdinal; i >= region.FirstBlockOrdinal; i--)
                {
                    for (var j = blocks[i].Operations.Length - 1; j >= 0; j--)
                    {
                        if (blocks[i].Operations[j] is IFlowCaptureOperation capture)
                        {
                            yield return capture;
                        }
                    }
                }
            }

            IEnumerable<IFlowCaptureReferenceOperation> getFlowCaptureReferenceOperationsInRegion(ControlFlowRegion region, int firstBlockOrdinal)
            {
                Debug.Assert(firstBlockOrdinal >= region.FirstBlockOrdinal);
                for (int i = firstBlockOrdinal; i <= region.LastBlockOrdinal; i++)
                {
                    BasicBlock block = blocks[i];
                    foreach (IOperation operation in block.Operations)
                    {
                        foreach (IFlowCaptureReferenceOperation reference in operation.DescendantsAndSelf().OfType<IFlowCaptureReferenceOperation>())
                        {
                            yield return reference;
                        }
                    }

                    if (block.BranchValue != null)
                    {
                        foreach (IFlowCaptureReferenceOperation reference in block.BranchValue.DescendantsAndSelf().OfType<IFlowCaptureReferenceOperation>())
                        {
                            yield return reference;
                        }
                    }
                }
            }

            string getDestinationString(ref ControlFlowBranch branch)
            {
                return branch.Destination != null ? getBlockId(branch.Destination) : "null";
            }

            PooledObjects.PooledDictionary<ControlFlowRegion, int> buildRegionMap()
            {
                var result = PooledObjects.PooledDictionary<ControlFlowRegion, int>.GetInstance();
                int ordinal = 0;
                visit(graph.Root);

                void visit(ControlFlowRegion region)
                {
                    result.Add(region, ordinal++);

                    foreach (ControlFlowRegion r in region.NestedRegions)
                    {
                        visit(r);
                    }
                }

                return result;
            }

            void appendLine(string line)
            {
                appendIndent();
                stringBuilder.AppendLine(line);
            }

            void appendIndent()
            {
                stringBuilder.Append(' ', indent);
            }

            void printLocals(ControlFlowRegion region)
            {
                if (!region.Locals.IsEmpty)
                {
                    appendIndent();
                    stringBuilder.Append("Locals:");
                    foreach (ILocalSymbol local in region.Locals)
                    {
                        stringBuilder.Append($" [{local.ToTestDisplayString()}]");
                    }
                    stringBuilder.AppendLine();
                }

                if (!region.LocalFunctions.IsEmpty)
                {
                    appendIndent();
                    stringBuilder.Append("Methods:");
                    foreach (IMethodSymbol method in region.LocalFunctions)
                    {
                        stringBuilder.Append($" [{method.ToTestDisplayString()}]");
                    }
                    stringBuilder.AppendLine();
                }

                if (!region.CaptureIds.IsEmpty)
                {
                    appendIndent();
                    stringBuilder.Append("CaptureIds:");
                    foreach (CaptureId id in region.CaptureIds)
                    {
                        stringBuilder.Append($" [{id.Value}]");
                    }
                    stringBuilder.AppendLine();
                }
            }

            void enterRegions(ControlFlowRegion region, int firstBlockOrdinal)
            {
                if (region.FirstBlockOrdinal != firstBlockOrdinal)
                {
                    Assert.Same(currentRegion, region);

                    if (lastPrintedBlockIsInCurrentRegion)
                    {
                        stringBuilder.AppendLine();
                    }

                    return;
                }

                enterRegions(region.EnclosingRegion, firstBlockOrdinal);
                currentRegion = region;
                lastPrintedBlockIsInCurrentRegion = true;

                switch (region.Kind)
                {
                    case ControlFlowRegionKind.Filter:
                        Assert.Empty(region.Locals);
                        Assert.Empty(region.LocalFunctions);
                        Assert.Equal(firstBlockOrdinal, region.EnclosingRegion.FirstBlockOrdinal);
                        Assert.Same(region.ExceptionType, region.EnclosingRegion.ExceptionType);
                        enterRegion($".filter {{{getRegionId(region)}}}");
                        break;
                    case ControlFlowRegionKind.Try:
                        Assert.Null(region.ExceptionType);
                        Assert.Equal(firstBlockOrdinal, region.EnclosingRegion.FirstBlockOrdinal);
                        enterRegion($".try {{{getRegionId(region.EnclosingRegion)}, {getRegionId(region)}}}");
                        break;
                    case ControlFlowRegionKind.FilterAndHandler:
                        enterRegion($".catch {{{getRegionId(region)}}} ({region.ExceptionType?.ToTestDisplayString() ?? "null"})");
                        break;
                    case ControlFlowRegionKind.Finally:
                        Assert.Null(region.ExceptionType);
                        enterRegion($".finally {{{getRegionId(region)}}}");
                        break;
                    case ControlFlowRegionKind.Catch:
                        switch (region.EnclosingRegion.Kind)
                        {
                            case ControlFlowRegionKind.FilterAndHandler:
                                Assert.Same(region.ExceptionType, region.EnclosingRegion.ExceptionType);
                                enterRegion($".handler {{{getRegionId(region)}}}");
                                break;
                            case ControlFlowRegionKind.TryAndCatch:
                                enterRegion($".catch {{{getRegionId(region)}}} ({region.ExceptionType?.ToTestDisplayString() ?? "null"})");
                                break;
                            default:
                                Assert.False(true, $"Unexpected region kind {region.EnclosingRegion.Kind}");
                                break;
                        }
                        break;
                    case ControlFlowRegionKind.LocalLifetime:
                        Assert.Null(region.ExceptionType);
                        Assert.False(region.Locals.IsEmpty && region.LocalFunctions.IsEmpty && region.CaptureIds.IsEmpty);
                        enterRegion($".locals {{{getRegionId(region)}}}");
                        break;

                    case ControlFlowRegionKind.TryAndCatch:
                    case ControlFlowRegionKind.TryAndFinally:
                        Assert.Empty(region.Locals);
                        Assert.Empty(region.LocalFunctions);
                        Assert.Empty(region.CaptureIds);
                        Assert.Null(region.ExceptionType);
                        break;

                    case ControlFlowRegionKind.StaticLocalInitializer:
                        Assert.Null(region.ExceptionType);
                        Assert.Empty(region.Locals);
                        enterRegion($".static initializer {{{getRegionId(region)}}}");
                        break;

                    case ControlFlowRegionKind.ErroneousBody:
                        Assert.Null(region.ExceptionType);
                        enterRegion($".erroneous body {{{getRegionId(region)}}}");
                        break;

                    default:
                        Assert.False(true, $"Unexpected region kind {region.Kind}");
                        break;
                }

                void enterRegion(string header)
                {
                    appendLine(header);
                    appendLine("{");
                    indent += 4;
                    printLocals(region);
                }
            }

            void leaveRegions(ControlFlowRegion region, int lastBlockOrdinal)
            {
                if (region.LastBlockOrdinal != lastBlockOrdinal)
                {
                    currentRegion = region;
                    lastPrintedBlockIsInCurrentRegion = false;
                    return;
                }

                string regionId = getRegionId(region);
                for (var i = 0; i < region.LocalFunctions.Length; i++)
                {
                    var method = region.LocalFunctions[i];
                    appendLine("");
                    appendLine("{   " + method.ToTestDisplayString());
                    appendLine("");
                    var g = graph.GetLocalFunctionControlFlowGraph(method);
                    localFunctionsMap.Add(method, g);
                    Assert.Equal(OperationKind.LocalFunction, g.OriginalOperation.Kind);
                    GetFlowGraph(stringBuilder, compilation, g, region, $"#{i}{regionId}", indent + 4, associatedSymbol);
                    appendLine("}");
                }

                switch (region.Kind)
                {
                    case ControlFlowRegionKind.LocalLifetime:
                    case ControlFlowRegionKind.Filter:
                    case ControlFlowRegionKind.Try:
                    case ControlFlowRegionKind.Finally:
                    case ControlFlowRegionKind.FilterAndHandler:
                    case ControlFlowRegionKind.StaticLocalInitializer:
                    case ControlFlowRegionKind.ErroneousBody:
                        indent -= 4;
                        appendLine("}");
                        break;
                    case ControlFlowRegionKind.Catch:
                        switch (region.EnclosingRegion.Kind)
                        {
                            case ControlFlowRegionKind.FilterAndHandler:
                            case ControlFlowRegionKind.TryAndCatch:
                                goto endRegion;

                            default:
                                Assert.False(true, $"Unexpected region kind {region.EnclosingRegion.Kind}");
                                break;
                        }

                        break;

endRegion:
                        goto case ControlFlowRegionKind.Filter;

                    case ControlFlowRegionKind.TryAndCatch:
                    case ControlFlowRegionKind.TryAndFinally:
                        break;
                    default:
                        Assert.False(true, $"Unexpected region kind {region.Kind}");
                        break;
                }

                leaveRegions(region.EnclosingRegion, lastBlockOrdinal);
            }

            void validateBranch(BasicBlock fromBlock, ControlFlowBranch branch)
            {
                if (branch.Destination == null)
                {
                    Assert.Empty(branch.FinallyRegions);
                    Assert.Empty(branch.LeavingRegions);
                    Assert.Empty(branch.EnteringRegions);
                    Assert.True(ControlFlowBranchSemantics.None == branch.Semantics || ControlFlowBranchSemantics.Throw == branch.Semantics ||
                                ControlFlowBranchSemantics.Rethrow == branch.Semantics || ControlFlowBranchSemantics.StructuredExceptionHandling == branch.Semantics ||
                                ControlFlowBranchSemantics.ProgramTermination == branch.Semantics || ControlFlowBranchSemantics.Error == branch.Semantics);
                    return;
                }

                Assert.True(ControlFlowBranchSemantics.Regular == branch.Semantics || ControlFlowBranchSemantics.Return == branch.Semantics);
                Assert.True(branch.Destination.Predecessors.Contains(p => p.Source == fromBlock));

                if (!branch.FinallyRegions.IsEmpty)
                {
                    appendLine($"        Finalizing:" + buildList(branch.FinallyRegions));
                }

                ControlFlowRegion remainedIn1 = fromBlock.EnclosingRegion;
                if (!branch.LeavingRegions.IsEmpty)
                {
                    appendLine($"        Leaving:" + buildList(branch.LeavingRegions));
                    foreach (ControlFlowRegion r in branch.LeavingRegions)
                    {
                        Assert.Same(remainedIn1, r);
                        remainedIn1 = r.EnclosingRegion;
                    }
                }

                ControlFlowRegion remainedIn2 = branch.Destination.EnclosingRegion;
                if (!branch.EnteringRegions.IsEmpty)
                {
                    appendLine($"        Entering:" + buildList(branch.EnteringRegions));
                    for (int j = branch.EnteringRegions.Length - 1; j >= 0; j--)
                    {
                        ControlFlowRegion r = branch.EnteringRegions[j];
                        Assert.Same(remainedIn2, r);
                        remainedIn2 = r.EnclosingRegion;
                    }
                }

                Assert.Same(remainedIn1.EnclosingRegion, remainedIn2.EnclosingRegion);

                string buildList(ImmutableArray<ControlFlowRegion> list)
                {
                    var builder = PooledObjects.PooledStringBuilder.GetInstance();

                    foreach (ControlFlowRegion r in list)
                    {
                        builder.Builder.Append($" {{{getRegionId(r)}}}");
                    }

                    return builder.ToStringAndFree();
                }
            }

            void validateRoot(IOperation root)
            {
                visitor.Visit(root);
                Assert.Null(root.Parent);
                Assert.Null(((Operation)root).OwningSemanticModel);
                Assert.Null(root.SemanticModel);
                Assert.True(CanBeInControlFlowGraph(root), $"Unexpected node kind OperationKind.{root.Kind}");

                foreach (var operation in root.Descendants())
                {
                    visitor.Visit(operation);
                    Assert.NotNull(operation.Parent);
                    Assert.Null(((Operation)operation).OwningSemanticModel);
                    Assert.Null(operation.SemanticModel);
                    Assert.True(CanBeInControlFlowGraph(operation), $"Unexpected node kind OperationKind.{operation.Kind}");
                }
            }

            void validateLifetimeOfReferences(BasicBlock block, Func<string> finalGraph)
            {
                referencedCaptureIds.Clear();
                referencedLocalsAndMethods.Clear();

                foreach (IOperation operation in block.Operations)
                {
                    recordReferences(operation);
                }

                if (block.BranchValue != null)
                {
                    recordReferences(block.BranchValue);
                }

                ControlFlowRegion region = block.EnclosingRegion;

                while ((referencedCaptureIds.Count != 0 || referencedLocalsAndMethods.Count != 0) && region != null)
                {
                    foreach (ILocalSymbol l in region.Locals)
                    {
                        referencedLocalsAndMethods.Remove(l);
                    }

                    foreach (IMethodSymbol m in region.LocalFunctions)
                    {
                        referencedLocalsAndMethods.Remove(m);
                    }

                    foreach (CaptureId id in region.CaptureIds)
                    {
                        referencedCaptureIds.Remove(id);
                    }

                    region = region.EnclosingRegion;
                }

                if (referencedLocalsAndMethods.Count != 0)
                {
                    ISymbol symbol = referencedLocalsAndMethods.First();
                    Assert.True(false, $"{(symbol.Kind == SymbolKind.Local ? "Local" : "Method")} without owning region {symbol.ToTestDisplayString()} in [{getBlockId(block)}]\n{finalGraph()}");
                }

                if (referencedCaptureIds.Count != 0)
                {
                    Assert.True(false, $"Capture [{referencedCaptureIds.First().Value}] without owning region in [{getBlockId(block)}]\n{finalGraph()}");
                }
            }

            void recordReferences(IOperation operation)
            {
                foreach (IOperation node in operation.DescendantsAndSelf())
                {
                    IMethodSymbol method;

                    switch (node)
                    {
                        case ILocalReferenceOperation localReference:
                            if (localReference.Local.ContainingSymbol.IsTopLevelMainMethod() && !isInAssociatedSymbol(localReference.Local.ContainingSymbol, associatedSymbol))
                            {
                                // Top-level locals can be referenced from locations in the same file that are not actually the top
                                // level main. For these cases, we want to treat them like fields for the purposes of references,
                                // as they are not declared in this method and have no owning region
                                break;
                            }

                            referencedLocalsAndMethods.Add(localReference.Local);
                            break;
                        case IMethodReferenceOperation methodReference:
                            method = methodReference.Method;
                            if (method.MethodKind == MethodKind.LocalFunction)
                            {
                                if (method.ContainingSymbol.IsTopLevelMainMethod() && !isInAssociatedSymbol(method.ContainingSymbol, associatedSymbol))
                                {
                                    // Top-level local functions can be referenced from locations in the same file that are not actually the top
                                    // level main. For these cases, we want to treat them like class methods for the purposes of references,
                                    // as they are not declared in this method and have no owning region
                                    break;
                                }

                                referencedLocalsAndMethods.Add(method.OriginalDefinition);
                            }
                            break;
                        case IInvocationOperation invocation:
                            method = invocation.TargetMethod;
                            if (method.MethodKind == MethodKind.LocalFunction)
                            {
                                if (method.ContainingSymbol.IsTopLevelMainMethod() && !associatedSymbol.IsTopLevelMainMethod())
                                {
                                    // Top-level local functions can be referenced from locations in the same file that are not actually the top
                                    // level main. For these cases, we want to treat them like class methods for the purposes of references,
                                    // as they are not declared in this method and have no owning region
                                    break;
                                }

                                referencedLocalsAndMethods.Add(method.OriginalDefinition);
                            }
                            break;
                        case IFlowCaptureOperation flowCapture:
                            referencedCaptureIds.Add(flowCapture.Id);
                            break;
                        case IFlowCaptureReferenceOperation flowCaptureReference:
                            referencedCaptureIds.Add(flowCaptureReference.Id);
                            break;
                    }
                }

                static bool isInAssociatedSymbol(ISymbol symbol, ISymbol associatedSymbol)
                {
                    while (symbol is IMethodSymbol m)
                    {
                        if ((object)m == associatedSymbol)
                        {
                            return true;
                        }

                        symbol = m.ContainingSymbol;
                    }

                    return false;
                }
            }

            string getBlockId(BasicBlock block)
            {
                return $"B{block.Ordinal}{idSuffix}";
            }

            string getRegionId(ControlFlowRegion region)
            {
                return $"R{regionMap[region]}{idSuffix}";
            }

            string getOperationTree(IOperation operation)
            {
                var walker = new OperationTreeSerializer(graph, currentRegion, idSuffix, anonymousFunctionsMap, compilation, operation, initialIndent: 8 + indent, associatedSymbol);
                walker.Visit(operation);
                return walker.Builder.ToString();
            }
        }

        private static void AssertTrueWithGraph([DoesNotReturnIf(false)] bool value, string message, Func<string> finalGraph)
        {
            if (!value)
            {
                Assert.True(value, $"{message}\n{finalGraph()}");
            }
        }

        private sealed class OperationTreeSerializer : OperationTreeVerifier
        {
            private readonly ControlFlowGraph _graph;
            private readonly ControlFlowRegion _region;
            private readonly string _idSuffix;
            private readonly Dictionary<IFlowAnonymousFunctionOperation, ControlFlowGraph> _anonymousFunctionsMap;
            private readonly ISymbol _associatedSymbol;

            public OperationTreeSerializer(ControlFlowGraph graph, ControlFlowRegion region, string idSuffix,
                                           Dictionary<IFlowAnonymousFunctionOperation, ControlFlowGraph> anonymousFunctionsMap,
                                           Compilation compilation, IOperation root, int initialIndent, ISymbol associatedSymbol) :
                base(compilation, root, initialIndent)
            {
                _graph = graph;
                _region = region;
                _idSuffix = idSuffix;
                _anonymousFunctionsMap = anonymousFunctionsMap;
                _associatedSymbol = associatedSymbol;
            }

            public System.Text.StringBuilder Builder => _builder;

            public override void VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation)
            {
                base.VisitFlowAnonymousFunction(operation);

                LogString("{");
                LogNewLine();
                var g = _graph.GetAnonymousFunctionControlFlowGraph(operation);
                int id = _anonymousFunctionsMap.Count;
                _anonymousFunctionsMap.Add(operation, g);
                Assert.Equal(OperationKind.AnonymousFunction, g.OriginalOperation.Kind);
                GetFlowGraph(_builder, _compilation, g, _region, $"#A{id}{_idSuffix}", _currentIndent.Length + 4, _associatedSymbol);
                LogString("}");
                LogNewLine();
            }
        }

        private static bool CanBeInControlFlowGraph(IOperation n)
        {
            switch (n.Kind)
            {
                case OperationKind.Block:
                case OperationKind.Switch:
                case OperationKind.Loop:
                case OperationKind.Branch:
                case OperationKind.Lock:
                case OperationKind.Try:
                case OperationKind.Using:
                case OperationKind.Conditional:
                case OperationKind.Coalesce:
                case OperationKind.ConditionalAccess:
                case OperationKind.ConditionalAccessInstance:
                case OperationKind.MemberInitializer:
                case OperationKind.FieldInitializer:
                case OperationKind.PropertyInitializer:
                case OperationKind.ParameterInitializer:
                case OperationKind.CatchClause:
                case OperationKind.SwitchCase:
                case OperationKind.CaseClause:
                case OperationKind.VariableDeclarationGroup:
                case OperationKind.VariableDeclaration:
                case OperationKind.VariableDeclarator:
                case OperationKind.VariableInitializer:
                case OperationKind.Return:
                case OperationKind.YieldBreak:
                case OperationKind.Labeled:
                case OperationKind.Throw:
                case OperationKind.End:
                case OperationKind.Empty:
                case OperationKind.NameOf:
                case OperationKind.AnonymousFunction:
                case OperationKind.ObjectOrCollectionInitializer:
                case OperationKind.LocalFunction:
                case OperationKind.CoalesceAssignment:
                case OperationKind.SwitchExpression:
                case OperationKind.SwitchExpressionArm:
                    return false;

                case OperationKind.Binary:
                    var binary = (IBinaryOperation)n;
                    return (binary.OperatorKind != Operations.BinaryOperatorKind.ConditionalAnd && binary.OperatorKind != Operations.BinaryOperatorKind.ConditionalOr) ||
                            (binary.OperatorMethod == null &&
                             !ITypeSymbolHelpers.IsBooleanType(binary.Type) &&
                             !ITypeSymbolHelpers.IsNullableOfBoolean(binary.Type) &&
                             !ITypeSymbolHelpers.IsObjectType(binary.Type) &&
                             !ITypeSymbolHelpers.IsDynamicType(binary.Type));

                case OperationKind.InstanceReference:
                    // Implicit instance receivers, except for anonymous type creations, are expected to have been removed when dealing with creations.
                    var instanceReference = (IInstanceReferenceOperation)n;
                    return instanceReference.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance ||
                        instanceReference.ReferenceKind == InstanceReferenceKind.PatternInput ||
                        // Will be removed when CFG support for interpolated string handlers is implemented, tracked by
                        // https://github.com/dotnet/roslyn/issues/54718
                        instanceReference.ReferenceKind == InstanceReferenceKind.InterpolatedStringHandler ||
                        (instanceReference.ReferenceKind == InstanceReferenceKind.ImplicitReceiver &&
                         n.Type.IsAnonymousType &&
                         n.Parent is IPropertyReferenceOperation propertyReference &&
                         propertyReference.Instance == n &&
                         propertyReference.Parent is ISimpleAssignmentOperation simpleAssignment &&
                         simpleAssignment.Target == propertyReference &&
                         simpleAssignment.Parent.Kind == OperationKind.AnonymousObjectCreation);

                case OperationKind.None:
                    return !(n is IPlaceholderOperation);

                case OperationKind.FunctionPointerInvocation:
                case OperationKind.Invalid:
                case OperationKind.YieldReturn:
                case OperationKind.ExpressionStatement:
                case OperationKind.Stop:
                case OperationKind.RaiseEvent:
                case OperationKind.Literal:
                case OperationKind.Utf8String:
                case OperationKind.Conversion:
                case OperationKind.Invocation:
                case OperationKind.ArrayElementReference:
                case OperationKind.LocalReference:
                case OperationKind.ParameterReference:
                case OperationKind.FieldReference:
                case OperationKind.MethodReference:
                case OperationKind.PropertyReference:
                case OperationKind.EventReference:
                case OperationKind.FlowAnonymousFunction:
                case OperationKind.ObjectCreation:
                case OperationKind.TypeParameterObjectCreation:
                case OperationKind.ArrayCreation:
                case OperationKind.ArrayInitializer:
                case OperationKind.IsType:
                case OperationKind.Await:
                case OperationKind.SimpleAssignment:
                case OperationKind.CompoundAssignment:
                case OperationKind.Parenthesized:
                case OperationKind.EventAssignment:
                case OperationKind.InterpolatedString:
                case OperationKind.AnonymousObjectCreation:
                case OperationKind.Tuple:
                case OperationKind.TupleBinary:
                case OperationKind.DynamicObjectCreation:
                case OperationKind.DynamicMemberReference:
                case OperationKind.DynamicInvocation:
                case OperationKind.DynamicIndexerAccess:
                case OperationKind.TranslatedQuery:
                case OperationKind.DelegateCreation:
                case OperationKind.DefaultValue:
                case OperationKind.TypeOf:
                case OperationKind.SizeOf:
                case OperationKind.AddressOf:
                case OperationKind.IsPattern:
                case OperationKind.Increment:
                case OperationKind.Decrement:
                case OperationKind.DeconstructionAssignment:
                case OperationKind.DeclarationExpression:
                case OperationKind.OmittedArgument:
                case OperationKind.Argument:
                case OperationKind.InterpolatedStringText:
                case OperationKind.Interpolation:
                case OperationKind.ConstantPattern:
                case OperationKind.DeclarationPattern:
                case OperationKind.Unary:
                case OperationKind.FlowCapture:
                case OperationKind.FlowCaptureReference:
                case OperationKind.IsNull:
                case OperationKind.CaughtException:
                case OperationKind.StaticLocalInitializationSemaphore:
                case OperationKind.Discard:
                case OperationKind.ReDim:
                case OperationKind.ReDimClause:
                case OperationKind.Range:
                case OperationKind.RecursivePattern:
                case OperationKind.DiscardPattern:
                case OperationKind.PropertySubpattern:
                case OperationKind.RelationalPattern:
                case OperationKind.NegatedPattern:
                case OperationKind.BinaryPattern:
                case OperationKind.TypePattern:
                case OperationKind.InterpolatedStringAppendFormatted:
                case OperationKind.InterpolatedStringAppendLiteral:
                case OperationKind.InterpolatedStringAppendInvalid:
                case OperationKind.SlicePattern:
                case OperationKind.ListPattern:
                case OperationKind.ImplicitIndexerReference:
                case OperationKind.Attribute:
                case OperationKind.InlineArrayAccess:
                    return true;
            }

            Assert.True(false, $"Unhandled node kind OperationKind.{n.Kind}");
            return false;
        }

#nullable enable
        private static bool IsTopLevelMainMethod([NotNullWhen(true)] this ISymbol? symbol)
        {
            return symbol is IMethodSymbol
            {
                Name: WellKnownMemberNames.TopLevelStatementsEntryPointMethodName,
                ContainingType: INamedTypeSymbol
                {
                    Name: WellKnownMemberNames.TopLevelStatementsEntryPointTypeName,
                    ContainingType: null,
                    ContainingNamespace: { IsGlobalNamespace: true }
                }
            };
        }
    }
}
