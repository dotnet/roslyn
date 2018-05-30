﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
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
                                         ControlFlowGraph.Region enclosing, string idSuffix, int indent)
        {
            ImmutableArray<BasicBlock> blocks = graph.Blocks;

            var visitor = TestOperationVisitor.Singleton;
            ControlFlowGraph.Region currentRegion = graph.Root;
            bool lastPrintedBlockIsInCurrentRegion = true;
            PooledObjects.PooledDictionary<ControlFlowGraph.Region, int> regionMap = buildRegionMap();
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
                        Assert.Empty(block.Statements);
                        Assert.Null(block.Conditional.Condition);
                        Assert.NotNull(block.Next.Branch.Destination);
                        Assert.Null(block.Next.Value);
                        Assert.Same(graph.Root, currentRegion);
                        Assert.Same(currentRegion, block.Region);
                        Assert.Equal(0, currentRegion.FirstBlockOrdinal);
                        Assert.Same(enclosing, currentRegion.Enclosing);
                        Assert.Null(currentRegion.ExceptionType);
                        Assert.Empty(currentRegion.Locals);
                        Assert.Empty(currentRegion.Methods);
                        Assert.Equal(ControlFlowGraph.RegionKind.Root, currentRegion.Kind);
                        Assert.True(block.IsReachable);
                        break;

                    case BasicBlockKind.Exit:
                        Assert.Equal(blocks.Length - 1, i);
                        Assert.Empty(block.Statements);
                        Assert.Null(block.Conditional.Condition);
                        Assert.Null(block.Next.Branch.Destination);
                        Assert.Null(block.Next.Value);
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
                    foreach (BasicBlock predecessor in predecessors.OrderBy(p => p.Ordinal))
                    {
                        Assert.Same(blocks[predecessor.Ordinal], predecessor);
                        Assert.True(predecessor.Conditional.Branch.Destination == block || predecessor.Next.Branch.Destination == block);

                        if (block.Kind != BasicBlockKind.Exit && predecessor.Next.Branch.Destination == block)
                        {
                            Assert.Null(predecessor.Next.Value);
                        }

                        stringBuilder.Append($" [{getBlockId(predecessor)}]");
                    }

                    stringBuilder.AppendLine();
                }
                else if (block.Kind != BasicBlockKind.Entry)
                {
                    appendLine("    Predecessors (0)");
                }

                var statements = block.Statements;
                appendLine($"    Statements ({statements.Length})");
                foreach (var statement in statements)
                {
                    validateRoot(statement);
                    stringBuilder.AppendLine(OperationTreeVerifier.GetOperationTree(compilation, statement, initialIndent: 8 + indent));
                }

                BasicBlock.Branch conditionalBranch = block.Conditional.Branch;

                Assert.NotEqual(BasicBlock.BranchKind.Return, conditionalBranch.Kind);
                Assert.NotEqual(BasicBlock.BranchKind.Throw, conditionalBranch.Kind);

                if (block.Conditional.Condition != null)
                {
                    if (conditionalBranch.Destination != null)
                    {
                        Assert.Same(blocks[conditionalBranch.Destination.Ordinal], conditionalBranch.Destination);
                    }

                    Assert.NotEqual(BasicBlock.BranchKind.StructuredExceptionHandling, conditionalBranch.Kind);
                    appendLine($"    Jump if {(block.Conditional.JumpIfTrue ? "True" : "False")} ({conditionalBranch.Kind}) to Block[{getDestinationString(ref conditionalBranch)}]");

                    IOperation value = block.Conditional.Condition;
                    validateRoot(value);
                    stringBuilder.Append(OperationTreeVerifier.GetOperationTree(compilation, value, initialIndent: 8 + indent));
                    validateBranch(block, conditionalBranch);
                    stringBuilder.AppendLine();
                }
                else
                {
                    Assert.Null(conditionalBranch.Destination);
                    validateBranch(block, conditionalBranch);
                }

                BasicBlock.Branch nextBranch = block.Next.Branch;

                if (nextBranch.Destination != null || block.Kind != BasicBlockKind.Exit)
                {
                    if (nextBranch.Destination != null)
                    {
                        Assert.Same(blocks[nextBranch.Destination.Ordinal], nextBranch.Destination);
                    }

                    if (nextBranch.Kind == BasicBlock.BranchKind.StructuredExceptionHandling)
                    {
                        Assert.Null(nextBranch.Destination);
                        Assert.Equal(block.Region.LastBlockOrdinal, block.Ordinal);
                        Assert.True(block.Region.Kind == ControlFlowGraph.RegionKind.Filter || block.Region.Kind == ControlFlowGraph.RegionKind.Finally);
                    }

                    appendLine($"    Next ({nextBranch.Kind}) Block[{getDestinationString(ref nextBranch)}]");
                    IOperation value = block.Next.Value;

                    if (value != null)
                    {
                        Assert.True(BasicBlock.BranchKind.Return == nextBranch.Kind || BasicBlock.BranchKind.Throw == nextBranch.Kind);
                        validateRoot(value);
                        stringBuilder.Append(OperationTreeVerifier.GetOperationTree(compilation, value, initialIndent: 8 + indent));
                    }
                    else
                    {
                        Assert.NotEqual(BasicBlock.BranchKind.Return, nextBranch.Kind);
                        Assert.NotEqual(BasicBlock.BranchKind.Throw, nextBranch.Kind);
                    }
                }
                else
                {
                    Assert.Equal(BasicBlock.BranchKind.None, nextBranch.Kind);
                    Assert.Null(nextBranch.Destination);
                    Assert.Null(block.Next.Value);
                }

                validateBranch(block, nextBranch);

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

            string getDestinationString(ref BasicBlock.Branch branch)
            {
                return branch.Destination != null ? getBlockId(branch.Destination) : "null";
            }

            PooledObjects.PooledDictionary<ControlFlowGraph.Region, int> buildRegionMap()
            {
                var result = PooledObjects.PooledDictionary<ControlFlowGraph.Region, int>.GetInstance();
                int ordinal = 0;
                visit(graph.Root);

                void visit(ControlFlowGraph.Region region)
                {
                    result.Add(region, ordinal++);

                    foreach (ControlFlowGraph.Region r in region.Regions)
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

            void printLocals(ControlFlowGraph.Region region)
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

            void enterRegions(ControlFlowGraph.Region region, int firstBlockOrdinal)
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
                    case ControlFlowGraph.RegionKind.Filter:
                        Assert.Empty(region.Locals);
                        Assert.Empty(region.Methods);
                        Assert.Equal(firstBlockOrdinal, region.Enclosing.FirstBlockOrdinal);
                        Assert.Same(region.ExceptionType, region.Enclosing.ExceptionType);
                        enterRegion($".filter {{{getRegionId(region)}}}");
                        break;
                    case ControlFlowGraph.RegionKind.Try:
                        Assert.Null(region.ExceptionType);
                        Assert.Equal(firstBlockOrdinal, region.Enclosing.FirstBlockOrdinal);
                        enterRegion($".try {{{getRegionId(region.Enclosing)}, {getRegionId(region)}}}");
                        break;
                    case ControlFlowGraph.RegionKind.FilterAndHandler:
                        enterRegion($".catch {{{getRegionId(region)}}} ({region.ExceptionType?.ToTestDisplayString() ?? "null"})");
                        break;

                    case ControlFlowGraph.RegionKind.Finally:
                        Assert.Null(region.ExceptionType);
                        enterRegion($".finally {{{getRegionId(region)}}}");
                        break;

                    case ControlFlowGraph.RegionKind.Catch:
                        switch (region.Enclosing.Kind)
                        {
                            case ControlFlowGraph.RegionKind.FilterAndHandler:
                                Assert.Same(region.ExceptionType, region.Enclosing.ExceptionType);
                                enterRegion($".handler {{{getRegionId(region)}}}");
                                break;
                            case ControlFlowGraph.RegionKind.TryAndCatch:
                                enterRegion($".catch {{{getRegionId(region)}}} ({region.ExceptionType?.ToTestDisplayString() ?? "null"})");
                                break;
                            default:
                                Assert.False(true, $"Unexpected region kind {region.Enclosing.Kind}");
                                break;
                        }
                        break;
                    case ControlFlowGraph.RegionKind.Locals:
                        Assert.Null(region.ExceptionType);
                        Assert.False(region.Locals.IsEmpty && region.Methods.IsEmpty);
                        enterRegion($".locals {{{getRegionId(region)}}}");
                        break;

                    case ControlFlowGraph.RegionKind.TryAndCatch:
                    case ControlFlowGraph.RegionKind.TryAndFinally:
                        Assert.Empty(region.Locals);
                        Assert.Empty(region.Methods);
                        Assert.Null(region.ExceptionType);
                        break;

                    case ControlFlowGraph.RegionKind.StaticLocalInitializer:
                        Assert.Null(region.ExceptionType);
                        Assert.Empty(region.Locals);
                        enterRegion($".static initializer {{{getRegionId(region)}}}");
                        break;

                    case ControlFlowGraph.RegionKind.ErroneousBody:
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

            void leaveRegions(ControlFlowGraph.Region region, int lastBlockOrdinal)
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
                    case ControlFlowGraph.RegionKind.Locals:
                    case ControlFlowGraph.RegionKind.Filter:
                    case ControlFlowGraph.RegionKind.Try:
                    case ControlFlowGraph.RegionKind.Finally:
                    case ControlFlowGraph.RegionKind.FilterAndHandler:
                    case ControlFlowGraph.RegionKind.StaticLocalInitializer:
                    case ControlFlowGraph.RegionKind.ErroneousBody:
                        indent -= 4;
                        appendLine("}");
                        break;
                    case ControlFlowGraph.RegionKind.Catch:
                        switch (region.Enclosing.Kind)
                        {
                            case ControlFlowGraph.RegionKind.FilterAndHandler:
                            case ControlFlowGraph.RegionKind.TryAndCatch:
                                goto endRegion;

                            default:
                                Assert.False(true, $"Unexpected region kind {region.Enclosing.Kind}");
                                break;
                        }

                        break;

endRegion:
                        goto case ControlFlowGraph.RegionKind.Filter;

                    case ControlFlowGraph.RegionKind.TryAndCatch:
                    case ControlFlowGraph.RegionKind.TryAndFinally:
                        break;
                    default:
                        Assert.False(true, $"Unexpected region kind {region.Kind}");
                        break;
                }
            }

            void validateBranch(BasicBlock fromBlock, BasicBlock.Branch branch)
            {
                if (branch.Destination == null)
                {
                    Assert.Empty(branch.FinallyRegions);
                    Assert.Empty(branch.LeavingRegions);
                    Assert.Empty(branch.EnteringRegions);
                    Assert.True(BasicBlock.BranchKind.None == branch.Kind || BasicBlock.BranchKind.Throw == branch.Kind ||
                                BasicBlock.BranchKind.ReThrow == branch.Kind || BasicBlock.BranchKind.StructuredExceptionHandling == branch.Kind ||
                                BasicBlock.BranchKind.ProgramTermination == branch.Kind || BasicBlock.BranchKind.Error == branch.Kind);
                    return;
                }

                Assert.True(BasicBlock.BranchKind.Regular == branch.Kind || BasicBlock.BranchKind.Return == branch.Kind);
                Assert.True(branch.Destination.Predecessors.Contains(fromBlock));

                if (!branch.FinallyRegions.IsEmpty)
                {
                    appendLine($"        Finalizing:" + buildList(branch.FinallyRegions));
                }

                ControlFlowGraph.Region remainedIn1 = fromBlock.Region;
                if (!branch.LeavingRegions.IsEmpty)
                {
                    appendLine($"        Leaving:" + buildList(branch.LeavingRegions));
                    foreach (ControlFlowGraph.Region r in branch.LeavingRegions)
                    {
                        Assert.Same(remainedIn1, r);
                        remainedIn1 = r.Enclosing;
                    }
                }

                ControlFlowGraph.Region remainedIn2 = branch.Destination.Region;
                if (!branch.EnteringRegions.IsEmpty)
                {
                    appendLine($"        Entering:" + buildList(branch.EnteringRegions));
                    for (int j = branch.EnteringRegions.Length - 1; j >= 0; j--)
                    {
                        ControlFlowGraph.Region r = branch.EnteringRegions[j];
                        Assert.Same(remainedIn2, r);
                        remainedIn2 = r.Enclosing;
                    }
                }

                Assert.Same(remainedIn1.Enclosing, remainedIn2.Enclosing);

                string buildList(ImmutableArray<ControlFlowGraph.Region> list)
                {
                    var builder = PooledObjects.PooledStringBuilder.GetInstance();

                    foreach (ControlFlowGraph.Region r in list)
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

                ISymbol[] localsOrMethodsInBlock = Enumerable.Concat(block.Statements, new[] { block.Conditional.Condition, block.Next.Value }).
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
                ControlFlowGraph.Region region = block.Region;

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

            string getRegionId(ControlFlowGraph.Region region)
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
