// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class ControlFlowGraphVerifier
    {
        public static void VerifyGraph(Compilation compilation, string expectedFlowGraph, ImmutableArray<BasicBlock> graph)
        {
            var actualFlowGraph = GetFlowGraph(compilation, graph);
            OperationTreeVerifier.Verify(expectedFlowGraph, actualFlowGraph);
        }

        public static string GetFlowGraph(Compilation compilation, ImmutableArray<BasicBlock> graph)
        {
            var map = new Dictionary<Operations.BasicBlock, int>();

            for (int i = 0; i < graph.Length; i++)
            {
                map.Add(graph[i], i);
            }

            var visitor = TestOperationVisitor.Singleton;
            var stringBuilder = PooledObjects.PooledStringBuilder.GetInstance();

            for (int i = 0; i < graph.Length; i++)
            {
                var block = graph[i];
                stringBuilder.Builder.AppendLine($"Block[{i}] - {block.Kind}");

                var predecessors = block.Predecessors;

                if (!predecessors.IsEmpty)
                {
                    stringBuilder.Builder.AppendLine($"    Predecessors ({predecessors.Count})");
                    foreach (int j in predecessors.Select(b => map[b]).OrderBy(ii => ii))
                    {
                        stringBuilder.Builder.AppendLine($"        [{j}]");
                    }
                }

                var statements = block.Statements;
                stringBuilder.Builder.AppendLine($"    Statements ({statements.Length})");
                foreach (var statement in statements)
                {
                    validateRoot(statement);
                    stringBuilder.Builder.AppendLine(OperationTreeVerifier.GetOperationTree(compilation, statement, initialIndent: 8));
                }

                if (block.Conditional.Value != null)
                {
                    Assert.True(map.TryGetValue(block.Conditional.Destination, out int index));
                    stringBuilder.Builder.AppendLine($"    Jump if {branchKindDisplay(block.Conditional.Kind)} to Block[{index}]");

                    string branchKindDisplay(ConditionalBranchKind kind)
                    {
                        switch (kind)
                        {
                            case ConditionalBranchKind.IfTrue:
                                return "True";
                            case ConditionalBranchKind.IfFalse:
                                return "False";
                            case ConditionalBranchKind.IfNull:
                                return "Null";
                            default:
                                Assert.False(true, $"Unexpected branch kind {kind}");
                                return "invalid";
                        }
                    }

                    IOperation value = block.Conditional.Value;
                    validateRoot(value);
                    stringBuilder.Builder.AppendLine(OperationTreeVerifier.GetOperationTree(compilation, value, initialIndent: 8));
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

                if (block.Next != null)
                {
                    Assert.True(map.TryGetValue(block.Next, out var index));
                    stringBuilder.Builder.AppendLine($"    Next Block[{index}]");
                }
            }

            return stringBuilder.ToStringAndFree();
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
                case OperationKind.ObjectOrCollectionInitializer:
                case OperationKind.MemberInitializer:
                case OperationKind.CollectionElementInitializer:
                case OperationKind.FieldInitializer:
                case OperationKind.PropertyInitializer:
                case OperationKind.ParameterInitializer:
                case OperationKind.ArrayInitializer:
                case OperationKind.CatchClause:
                case OperationKind.SwitchCase:
                case OperationKind.CaseClause:
                    return false;

                case OperationKind.Labeled:
                    return true; // PROTOTYPE(dataflow): should be replaced with the underlying statemen

                case OperationKind.VariableDeclarationGroup:
                case OperationKind.VariableInitializer:
                    return true; // PROTOTYPE(dataflow): should be translated into assignments

                case OperationKind.BinaryOperator:
                    var binary = (IBinaryOperation)n;
                    return binary.OperatorKind != Operations.BinaryOperatorKind.ConditionalAnd && binary.OperatorKind != Operations.BinaryOperatorKind.ConditionalOr;

                case OperationKind.None:
                case OperationKind.Invalid:
                case OperationKind.Empty:
                case OperationKind.Return:
                case OperationKind.YieldBreak:
                case OperationKind.YieldReturn:
                case OperationKind.ExpressionStatement:
                case OperationKind.LocalFunction:
                case OperationKind.Stop:
                case OperationKind.End:
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
                case OperationKind.InstanceReference:
                case OperationKind.IsType:
                case OperationKind.Await:
                case OperationKind.SimpleAssignment:
                case OperationKind.CompoundAssignment:
                case OperationKind.Parenthesized:
                case OperationKind.EventAssignment:
                case OperationKind.InterpolatedString:
                case OperationKind.AnonymousObjectCreation:
                case OperationKind.NameOf:
                case OperationKind.Tuple:
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
                case OperationKind.Throw:
                case OperationKind.Decrement:
                case OperationKind.DeconstructionAssignment:
                case OperationKind.DeclarationExpression:
                case OperationKind.OmittedArgument:
                case OperationKind.VariableDeclarator:
                case OperationKind.VariableDeclaration:
                case OperationKind.Argument:
                case OperationKind.InterpolatedStringText:
                case OperationKind.Interpolation:
                case OperationKind.ConstantPattern:
                case OperationKind.DeclarationPattern:
                case OperationKind.UnaryOperator:
                case OperationKind.FlowCapture:
                case OperationKind.FlowCaptureReference:
                    return true;
            }

            Assert.True(false, $"Unhandled node kind OperationKind.{n.Kind}");
            return false;
        }
    }
}
