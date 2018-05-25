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

            switch (operationRoot)
            {
                case IBlockOperation blockOperation:
                    return SemanticModel.GetControlFlowGraph(blockOperation);

                case IMethodBodyOperation methodBodyOperation:
                    return SemanticModel.GetControlFlowGraph(methodBodyOperation);

                case IConstructorBodyOperation constructorBodyOperation:
                    return SemanticModel.GetControlFlowGraph(constructorBodyOperation);

                case IFieldInitializerOperation fieldInitializerOperation:
                    return SemanticModel.GetControlFlowGraph(fieldInitializerOperation);

                case IPropertyInitializerOperation propertyInitializerOperation:
                    return SemanticModel.GetControlFlowGraph(propertyInitializerOperation);

                case IParameterInitializerOperation parameterInitializerOperation:
                    return SemanticModel.GetControlFlowGraph(parameterInitializerOperation);

                default:
                    return null;
            }
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
            PooledObjects.PooledDictionary<ControlFlowRegion, int> regionMap = buildRegionMap();
            var methodsMap = PooledObjects.PooledDictionary<IMethodSymbol, ControlFlowGraph>.GetInstance();

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
                        Assert.Null(block.Condition);
                        Assert.Null(block.ConditionalSuccessor);
                        Assert.NotNull(block.FallThroughSuccessor.Destination);
                        Assert.Null(block.FallThroughSuccessor.Value);
                        Assert.Same(graph.Root, currentRegion);
                        Assert.Same(currentRegion, block.Region);
                        Assert.Equal(0, currentRegion.FirstBlockOrdinal);
                        Assert.Same(enclosing, currentRegion.Enclosing);
                        Assert.Null(currentRegion.ExceptionType);
                        Assert.Empty(currentRegion.Locals);
                        Assert.Empty(currentRegion.Methods);
                        Assert.Equal(ControlFlowRegionKind.Root, currentRegion.Kind);
                        Assert.True(block.IsReachable);
                        break;

                    case BasicBlockKind.Exit:
                        Assert.Equal(blocks.Length - 1, i);
                        Assert.Empty(block.Operations);
                        Assert.Null(block.FallThroughSuccessor);
                        Assert.Null(block.ConditionalSuccessor);
                        Assert.Null(block.Condition);
                        Assert.Same(graph.Root, currentRegion);
                        Assert.Same(currentRegion, block.Region);
                        Assert.Equal(i, currentRegion.LastBlockOrdinal);
                        break;

                    default:
                        Assert.False(true, $"Unexpected block kind {block.Kind}");
                        break;
                }

                if (block.Region != currentRegion)
                {
                    enterRegions(block.Region, block.Ordinal);
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
                    var visitedPredecessors = new Dictionary<int, ControlFlowBranch>();
                    foreach (ControlFlowBranch predecessorBranch in predecessors)
                    {
                        Assert.Same(block, predecessorBranch.Destination);
                        var predecessor = predecessorBranch.Source;
                        Assert.Same(blocks[predecessor.Ordinal], predecessor);
                        Assert.True(predecessor.ConditionalSuccessor?.Destination == block || predecessor.FallThroughSuccessor?.Destination == block);

                        if (block.Kind != BasicBlockKind.Exit && predecessor.FallThroughSuccessor?.Destination == block)
                        {
                            Assert.Null(predecessor.FallThroughSuccessor.Value);
                        }

                        if (visitedPredecessors.TryGetValue(predecessor.Ordinal, out ControlFlowBranch visitedBranch))
                        {
                            // Multiple branches from same predecessor.
                            // Verify both these branches are conditional and have opposite BranchWhenTrue values.
                            Assert.True(visitedBranch.IsConditional);
                            Assert.True(predecessorBranch.IsConditional);
                            Assert.True(visitedBranch.BranchWhenTrue != predecessorBranch.BranchWhenTrue);
                        }
                        else
                        {
                            stringBuilder.Append($" [{getBlockId(predecessor)}]");
                            visitedPredecessors.Add(predecessor.Ordinal, predecessorBranch);
                        }
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
                    stringBuilder.AppendLine(OperationTreeVerifier.GetOperationTree(compilation, statement, initialIndent: 8 + indent));
                }

                ControlFlowBranch conditionalBranch = block.ConditionalSuccessor;

                if (block.Condition != null)
                {
                    Assert.NotNull(conditionalBranch);
                    Assert.Same(block, conditionalBranch.Source);
                    if (conditionalBranch.Destination != null)
                    {
                        Assert.Same(blocks[conditionalBranch.Destination.Ordinal], conditionalBranch.Destination);
                    }

                    Assert.NotEqual(ControlFlowBranchKind.Return, conditionalBranch.Kind);
                    Assert.NotEqual(ControlFlowBranchKind.Throw, conditionalBranch.Kind);
                    Assert.NotEqual(ControlFlowBranchKind.StructuredExceptionHandling, conditionalBranch.Kind);

                    Assert.True(conditionalBranch.IsConditional);
                    appendLine($"    Jump if {(conditionalBranch.BranchWhenTrue ? "True" : "False")} ({conditionalBranch.Kind}) to Block[{getDestinationString(ref conditionalBranch)}]");

                    Assert.Null(conditionalBranch.Value);
                    IOperation value = block.Condition;
                    validateRoot(value);
                    stringBuilder.Append(OperationTreeVerifier.GetOperationTree(compilation, value, initialIndent: 8 + indent));
                    validateBranch(block, conditionalBranch);
                    stringBuilder.AppendLine();
                }
                else
                {
                    Assert.Null(conditionalBranch);
                }

                ControlFlowBranch nextBranch = block.FallThroughSuccessor;

                if (block.Kind == BasicBlockKind.Exit)
                {
                    Assert.Null(nextBranch);
                }
                else
                {
                    Assert.NotNull(nextBranch);
                    Assert.Same(block, nextBranch.Source);
                    if (nextBranch.Destination != null)
                    {
                        Assert.Same(blocks[nextBranch.Destination.Ordinal], nextBranch.Destination);
                    }

                    if (nextBranch.Kind == ControlFlowBranchKind.StructuredExceptionHandling)
                    {
                        Assert.Null(nextBranch.Destination);
                        Assert.Equal(block.Region.LastBlockOrdinal, block.Ordinal);
                        Assert.True(block.Region.Kind == ControlFlowRegionKind.Filter || block.Region.Kind == ControlFlowRegionKind.Finally);
                    }

                    if (conditionalBranch != null)
                    {
                        Assert.True(nextBranch.IsConditional);
                        Assert.True(nextBranch.BranchWhenTrue != conditionalBranch.BranchWhenTrue);
                    }
                    else
                    {
                        Assert.False(nextBranch.IsConditional);
                    }

                    appendLine($"    Next ({nextBranch.Kind}) Block[{getDestinationString(ref nextBranch)}]");
                    IOperation value = nextBranch.Value;

                    if (value != null)
                    {
                        Assert.True(ControlFlowBranchKind.Return == nextBranch.Kind || ControlFlowBranchKind.Throw == nextBranch.Kind);
                        validateRoot(value);
                        stringBuilder.Append(OperationTreeVerifier.GetOperationTree(compilation, value, initialIndent: 8 + indent));
                    }
                    else
                    {
                        Assert.NotEqual(ControlFlowBranchKind.Return, nextBranch.Kind);
                        Assert.NotEqual(ControlFlowBranchKind.Throw, nextBranch.Kind);
                    }

                    validateBranch(block, nextBranch);
                }

                validateLocalsAndMethodsLifetime(block);

                if (currentRegion.LastBlockOrdinal == block.Ordinal && i != blocks.Length - 1)
                {
                    leaveRegions(block.Region, block.Ordinal);
                }
                else
                {
                    lastPrintedBlockIsInCurrentRegion = true;
                }
            }

            foreach (IMethodSymbol m in graph.Methods)
            {
                ControlFlowGraph g = methodsMap[m];
                Assert.Same(g, graph[m]);
            }

            Assert.Equal(graph.Methods.Length, methodsMap.Count);

            regionMap.Free();
            methodsMap.Free();
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

                    foreach (ControlFlowRegion r in region.Regions)
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

                if (!region.Methods.IsEmpty)
                {
                    appendIndent();
                    stringBuilder.Append("Methods:");
                    foreach (IMethodSymbol method in region.Methods)
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

                enterRegions(region.Enclosing, firstBlockOrdinal);
                currentRegion = region;
                lastPrintedBlockIsInCurrentRegion = true;

                switch (region.Kind)
                {
                    case ControlFlowRegionKind.Filter:
                        Assert.Empty(region.Locals);
                        Assert.Empty(region.Methods);
                        Assert.Equal(firstBlockOrdinal, region.Enclosing.FirstBlockOrdinal);
                        Assert.Same(region.ExceptionType, region.Enclosing.ExceptionType);
                        enterRegion($".filter {{{getRegionId(region)}}}");
                        break;
                    case ControlFlowRegionKind.Try:
                        Assert.Null(region.ExceptionType);
                        Assert.Equal(firstBlockOrdinal, region.Enclosing.FirstBlockOrdinal);
                        enterRegion($".try {{{getRegionId(region.Enclosing)}, {getRegionId(region)}}}");
                        break;
                    case ControlFlowRegionKind.FilterAndHandler:
                        enterRegion($".catch {{{getRegionId(region)}}} ({region.ExceptionType?.ToTestDisplayString() ?? "null"})");
                        break;
                    case ControlFlowRegionKind.Finally:
                        Assert.Null(region.ExceptionType);
                        enterRegion($".finally {{{getRegionId(region)}}}");
                        break;
                    case ControlFlowRegionKind.Catch:
                        switch (region.Enclosing.Kind)
                        {
                            case ControlFlowRegionKind.FilterAndHandler:
                                Assert.Same(region.ExceptionType, region.Enclosing.ExceptionType);
                                enterRegion($".handler {{{getRegionId(region)}}}");
                                break;
                            case ControlFlowRegionKind.TryAndCatch:
                                enterRegion($".catch {{{getRegionId(region)}}} ({region.ExceptionType?.ToTestDisplayString() ?? "null"})");
                                break;
                            default:
                                Assert.False(true, $"Unexpected region kind {region.Enclosing.Kind}");
                                break;
                        }
                        break;
                    case ControlFlowRegionKind.Locals:
                        Assert.Null(region.ExceptionType);
                        Assert.False(region.Locals.IsEmpty && region.Methods.IsEmpty);
                        enterRegion($".locals {{{getRegionId(region)}}}");
                        break;

                    case ControlFlowRegionKind.TryAndCatch:
                    case ControlFlowRegionKind.TryAndFinally:
                        Assert.Empty(region.Locals);
                        Assert.Empty(region.Methods);
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

                leaveRegions(region.Enclosing, lastBlockOrdinal);

                string regionId = getRegionId(region);
                for (var i = 0; i < region.Methods.Length; i++)
                {
                    var method = region.Methods[i];
                    appendLine("");
                    appendLine("{   " + method.ToTestDisplayString());
                    appendLine("");
                    var g = graph[method];
                    methodsMap.Add(method, g);
                    GetFlowGraph(stringBuilder, compilation, g, region, $"#{i}{regionId}", indent + 4);
                    appendLine("}");
                }

                switch (region.Kind)
                {
                    case ControlFlowRegionKind.Locals:
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
                        switch (region.Enclosing.Kind)
                        {
                            case ControlFlowRegionKind.FilterAndHandler:
                            case ControlFlowRegionKind.TryAndCatch:
                                goto endRegion;

                            default:
                                Assert.False(true, $"Unexpected region kind {region.Enclosing.Kind}");
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
            }

            void validateBranch(BasicBlock fromBlock, ControlFlowBranch branch)
            {
                if (branch.Destination == null)
                {
                    Assert.Empty(branch.FinallyRegions);
                    Assert.Empty(branch.LeavingRegions);
                    Assert.Empty(branch.EnteringRegions);
                    Assert.True(ControlFlowBranchKind.None == branch.Kind || ControlFlowBranchKind.Throw == branch.Kind ||
                                ControlFlowBranchKind.ReThrow == branch.Kind || ControlFlowBranchKind.StructuredExceptionHandling == branch.Kind ||
                                ControlFlowBranchKind.ProgramTermination == branch.Kind || ControlFlowBranchKind.Error == branch.Kind);
                    return;
                }

                Assert.True(ControlFlowBranchKind.Regular == branch.Kind || ControlFlowBranchKind.Return == branch.Kind);
                Assert.True(branch.Destination.Predecessors.Contains(p => p.Source == fromBlock));

                if (!branch.FinallyRegions.IsEmpty)
                {
                    appendLine($"        Finalizing:" + buildList(branch.FinallyRegions));
                }

                ControlFlowRegion remainedIn1 = fromBlock.Region;
                if (!branch.LeavingRegions.IsEmpty)
                {
                    appendLine($"        Leaving:" + buildList(branch.LeavingRegions));
                    foreach (ControlFlowRegion r in branch.LeavingRegions)
                    {
                        Assert.Same(remainedIn1, r);
                        remainedIn1 = r.Enclosing;
                    }
                }

                ControlFlowRegion remainedIn2 = branch.Destination.Region;
                if (!branch.EnteringRegions.IsEmpty)
                {
                    appendLine($"        Entering:" + buildList(branch.EnteringRegions));
                    for (int j = branch.EnteringRegions.Length - 1; j >= 0; j--)
                    {
                        ControlFlowRegion r = branch.EnteringRegions[j];
                        Assert.Same(remainedIn2, r);
                        remainedIn2 = r.Enclosing;
                    }
                }

                Assert.Same(remainedIn1.Enclosing, remainedIn2.Enclosing);

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
                ISymbol[] localsOrMethodsInBlock = Enumerable.Concat(block.Operations, new[] { block.Condition, block.FallThroughSuccessor?.Value }).
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
                ControlFlowRegion region = block.Region;

                do
                {
                    foreach(ILocalSymbol l in region.Locals)
                    {
                        Assert.True(localsAndMethodsInRegions.Add(l));
                    }

                    foreach (IMethodSymbol m in region.Methods)
                    {
                        Assert.True(localsAndMethodsInRegions.Add(m));
                    }

                    region = region.Enclosing;
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
        }

        private static bool CanBeInControlFlowGraph(IOperation n)
        {
            switch (n.Kind)
            {
                case OperationKind.Block:
                    if (((IBlockOperation)n).Operations.IsEmpty &&
                        (n.Parent?.Kind == OperationKind.AnonymousFunction))
                    {
                        // PROTOTYPE(dataflow): Temporary allow
                        return true;
                    }
                    return false;

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
                case OperationKind.Invalid:
                case OperationKind.YieldReturn:
                case OperationKind.ExpressionStatement:
                case OperationKind.LocalFunction:
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
                case OperationKind.AnonymousFunction:
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
                case OperationKind.ObjectOrCollectionInitializer: // PROTOTYPE(dataflow): it looks like this node is leaking through in some error scenarios, at least for now.
                                                                  // PROTOTYPE(dataflow): It looks like there is a bug in IOperation tree generation for non-error scenario in
                                                                  //                      Microsoft.CodeAnalysis.CSharp.UnitTests.SemanticModelGetSemanticInfoTests.ObjectCreation3
                    return true;
            }

            Assert.True(false, $"Unhandled node kind OperationKind.{n.Kind}");
            return false;
        }
    }
}
