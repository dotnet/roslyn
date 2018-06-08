// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Extensions;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class ControlFlowGraphVerifier
    {
        public static ControlFlowGraph GetControlFlowGraph(SyntaxNode syntaxNode, SemanticModel model)
        {
            IOperation operationRoot = model.GetOperation(syntaxNode);

            // Workaround for unit tests designed to work on IBlockOperation with ConstructorBodyOperation/MethodBodyOperation parent.
            operationRoot = operationRoot.Kind == OperationKind.Block &&
                (operationRoot.Parent?.Kind == OperationKind.ConstructorBodyOperation ||
                operationRoot.Parent?.Kind == OperationKind.MethodBodyOperation) ?
                    operationRoot.Parent :
                    operationRoot;

            TestOperationVisitor.VerifySubTree(operationRoot);

            ControlFlowGraph graph;
            switch (operationRoot)
            {
                case IBlockOperation blockOperation:
                    graph = SemanticModel.GetControlFlowGraph(blockOperation);
                    break;

                case IMethodBodyOperation methodBodyOperation:
                    graph = SemanticModel.GetControlFlowGraph(methodBodyOperation);
                    break;

                case IConstructorBodyOperation constructorBodyOperation:
                    graph = SemanticModel.GetControlFlowGraph(constructorBodyOperation);
                    break;

                case IFieldInitializerOperation fieldInitializerOperation:
                    graph = SemanticModel.GetControlFlowGraph(fieldInitializerOperation);
                    break;

                case IPropertyInitializerOperation propertyInitializerOperation:
                    graph = SemanticModel.GetControlFlowGraph(propertyInitializerOperation);
                    break;

                case IParameterInitializerOperation parameterInitializerOperation:
                    graph = SemanticModel.GetControlFlowGraph(parameterInitializerOperation);
                    break;

                default:
                    return null;
            }

            Assert.NotNull(graph);
            Assert.Same(operationRoot, graph.OriginalOperation);
            return graph;
        }

        public static void VerifyGraph(Compilation compilation, string expectedFlowGraph, ControlFlowGraph graph)
        {
            var actualFlowGraph = GetFlowGraph(compilation, graph);
            OperationTreeVerifier.Verify(expectedFlowGraph, actualFlowGraph);
        }

        public static string GetFlowGraph(Compilation compilation, ControlFlowGraph graph)
        {
            var pooledBuilder = PooledObjects.PooledStringBuilder.GetInstance();
            var stringBuilder = pooledBuilder.Builder;

            GetFlowGraph(pooledBuilder.Builder, compilation, graph, enclosing: null, idSuffix: "", indent: 0);

            return pooledBuilder.ToStringAndFree();
        }

        private static void GetFlowGraph(System.Text.StringBuilder stringBuilder, Compilation compilation, ControlFlowGraph graph,
                                         ControlFlowRegion enclosing, string idSuffix, int indent)
        {
            ImmutableArray<BasicBlock> blocks = graph.Blocks;

            var visitor = TestOperationVisitor.Singleton;
            ControlFlowRegion currentRegion = graph.Root;
            bool lastPrintedBlockIsInCurrentRegion = true;
            PooledDictionary<ControlFlowRegion, int> regionMap = buildRegionMap();
            var localFunctionsMap = PooledDictionary<IMethodSymbol, ControlFlowGraph>.GetInstance();
            var anonymousFunctionsMap = PooledDictionary<IFlowAnonymousFunctionOperation, ControlFlowGraph>.GetInstance();

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

                validateLocalsAndMethodsLifetime(block);

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
            }

            Assert.Equal(graph.LocalFunctions.Length, localFunctionsMap.Count);

            foreach (KeyValuePair<IFlowAnonymousFunctionOperation, ControlFlowGraph> pair in anonymousFunctionsMap)
            {
                Assert.Same(pair.Value, graph.GetAnonymousFunctionControlFlowGraph(pair.Key));
            }

            regionMap.Free();
            localFunctionsMap.Free();
            anonymousFunctionsMap.Free();
            return;

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
                        Assert.False(region.Locals.IsEmpty && region.LocalFunctions.IsEmpty);
                        enterRegion($".locals {{{getRegionId(region)}}}");
                        break;

                    case ControlFlowRegionKind.TryAndCatch:
                    case ControlFlowRegionKind.TryAndFinally:
                        Assert.Empty(region.Locals);
                        Assert.Empty(region.LocalFunctions);
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
                    GetFlowGraph(stringBuilder, compilation, g, region, $"#{i}{regionId}", indent + 4);
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
                Assert.Null(((Operation)root).SemanticModel);
                Assert.True(CanBeInControlFlowGraph(root), $"Unexpected node kind OperationKind.{root.Kind}");

                foreach (var operation in root.Descendants())
                {
                    visitor.Visit(operation);
                    Assert.NotNull(operation.Parent);
                    Assert.Null(((Operation)operation).SemanticModel);
                    Assert.True(CanBeInControlFlowGraph(operation), $"Unexpected node kind OperationKind.{operation.Kind}");
                }
            }

            void validateLocalsAndMethodsLifetime(BasicBlock block)
            {
                ISymbol[] localsOrMethodsInBlock = Enumerable.Concat(block.Operations, new[] { block.BranchValue }).
                                                   Where(o => o != null).
                                                   SelectMany(o => o.DescendantsAndSelf().
                                                                   Select(node =>
                                                                          {
                                                                              IMethodSymbol method;

                                                                              switch (node.Kind)
                                                                              {
                                                                                  case OperationKind.LocalReference:
                                                                                      return ((ILocalReferenceOperation)node).Local;
                                                                                  case OperationKind.MethodReference:
                                                                                      method = ((IMethodReferenceOperation)node).Method;
                                                                                      return method.MethodKind == MethodKind.LocalFunction ? method.OriginalDefinition : null;
                                                                                  case OperationKind.Invocation:
                                                                                      method = ((IInvocationOperation)node).TargetMethod;
                                                                                      return method.MethodKind == MethodKind.LocalFunction ? method.OriginalDefinition : null;
                                                                                  default:
                                                                                      return (ISymbol)null;
                                                                              }
                                                                          }).
                                                                    Where(s => s != null)).
                                                   Distinct().ToArray();

                if (localsOrMethodsInBlock.Length == 0)
                {
                    return;
                }

                var localsAndMethodsInRegions = PooledHashSet<ISymbol>.GetInstance();
                ControlFlowRegion region = block.EnclosingRegion;

                do
                {
                    foreach(ILocalSymbol l in region.Locals)
                    {
                        Assert.True(localsAndMethodsInRegions.Add(l));
                    }

                    foreach (IMethodSymbol m in region.LocalFunctions)
                    {
                        Assert.True(localsAndMethodsInRegions.Add(m));
                    }

                    region = region.EnclosingRegion;
                }
                while (region != null);

                foreach (ISymbol l in localsOrMethodsInBlock)
                {
                    Assert.False(localsAndMethodsInRegions.Add(l), $"Local/method without owning region {l.ToTestDisplayString()} in [{getBlockId(block)}]");
                }

                localsAndMethodsInRegions.Free();
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
                var walker = new OperationTreeSerializer(graph, currentRegion, idSuffix, anonymousFunctionsMap, compilation, operation, initialIndent: 8 + indent);
                walker.Visit(operation);
                return walker.Builder.ToString();
            }
        }

        private sealed class OperationTreeSerializer : OperationTreeVerifier
        {
            private readonly ControlFlowGraph _graph;
            private readonly ControlFlowRegion _region;
            private readonly string _idSuffix;
            private readonly Dictionary<IFlowAnonymousFunctionOperation, ControlFlowGraph> _anonymousFunctionsMap;

            public OperationTreeSerializer(ControlFlowGraph graph, ControlFlowRegion region, string idSuffix, 
                                           Dictionary<IFlowAnonymousFunctionOperation, ControlFlowGraph> anonymousFunctionsMap,
                                           Compilation compilation, IOperation root, int initialIndent) :
                base(compilation, root, initialIndent)
            {
                _graph = graph;
                _region = region;
                _idSuffix = idSuffix;
                _anonymousFunctionsMap = anonymousFunctionsMap;
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
                GetFlowGraph(_builder, _compilation, g, _region, $"#A{id}{_idSuffix}", _currentIndent.Length + 4);
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
                    return false;

                case OperationKind.BinaryOperator:
                    var binary = (IBinaryOperation)n;
                    return (binary.OperatorKind != Operations.BinaryOperatorKind.ConditionalAnd && binary.OperatorKind != Operations.BinaryOperatorKind.ConditionalOr) ||
                            (binary.OperatorMethod == null && 
                             !ITypeSymbolHelpers.IsBooleanType(binary.Type) &&
                             !ITypeSymbolHelpers.IsNullableOfBoolean(binary.Type) &&
                             !ITypeSymbolHelpers.IsObjectType(binary.Type) &&
                             !ITypeSymbolHelpers.IsDynamicType(binary.Type));

                case OperationKind.InstanceReference:
                    // Implicit instance receivers are expected to have been removed when dealing with creations.
                    return ((IInstanceReferenceOperation)n).ReferenceKind == InstanceReferenceKind.ContainingTypeInstance;

                case OperationKind.None:
                    return !(n is IPlaceholderOperation);

                case OperationKind.Invalid:
                case OperationKind.YieldReturn:
                case OperationKind.ExpressionStatement:
                case OperationKind.Stop:
                case OperationKind.RaiseEvent:
                case OperationKind.Literal:
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
                case OperationKind.TupleBinaryOperator:
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
                case OperationKind.UnaryOperator:
                case OperationKind.FlowCapture:
                case OperationKind.FlowCaptureReference:
                case OperationKind.IsNull:
                case OperationKind.CaughtException:
                case OperationKind.StaticLocalInitializationSemaphore:
                case OperationKind.Discard:
                    return true;
            }

            Assert.True(false, $"Unhandled node kind OperationKind.{n.Kind}");
            return false;
        }
    }
}
