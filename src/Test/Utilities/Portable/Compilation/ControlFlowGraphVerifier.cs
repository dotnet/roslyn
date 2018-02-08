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
        public static void VerifyGraph(Compilation compilation, string expectedFlowGraph, ControlFlowGraph graph)
        {
            var actualFlowGraph = GetFlowGraph(compilation, graph);
            OperationTreeVerifier.Verify(expectedFlowGraph, actualFlowGraph);
        }

        public static string GetFlowGraph(Compilation compilation, ControlFlowGraph graph)
        {
            ImmutableArray<BasicBlock> blocks = graph.Blocks;
            var map = new Dictionary<Operations.BasicBlock, int>();

            for (int i = 0; i < blocks.Length; i++)
            {
                map.Add(blocks[i], i);
            }

            var visitor = TestOperationVisitor.Singleton;
            var stringBuilder = PooledObjects.PooledStringBuilder.GetInstance();

            for (int i = 0; i < blocks.Length; i++)
            {
                var block = blocks[i];

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
                        Assert.NotNull(block.Next);
                        break;

                    case BasicBlockKind.Exit:
                        Assert.Equal(blocks.Length - 1, i);
                        Assert.Empty(block.Statements);
                        Assert.Null(block.Conditional.Condition);
                        Assert.Null(block.Next);
                        break;

                    default:
                        Assert.False(true, $"Unexpected block kind {block.Kind}");
                        break;
                }

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

                if (block.Conditional.Condition != null)
                {
                    Assert.True(map.TryGetValue(block.Conditional.Destination, out int index));
                    stringBuilder.Builder.AppendLine($"    Jump if {(block.Conditional.JumpIfTrue ? "True" : "False")} to Block[{index}]");

                    IOperation value = block.Conditional.Condition;
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
                case OperationKind.VariableDeclarationGroup:
                case OperationKind.VariableDeclaration:
                case OperationKind.VariableDeclarator:
                case OperationKind.VariableInitializer:
                    return false;

                case OperationKind.Labeled:
                    return true; // PROTOTYPE(dataflow): should be replaced with the underlying statemen

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
                case OperationKind.Argument:
                case OperationKind.InterpolatedStringText:
                case OperationKind.Interpolation:
                case OperationKind.ConstantPattern:
                case OperationKind.DeclarationPattern:
                case OperationKind.UnaryOperator:
                case OperationKind.FlowCapture:
                case OperationKind.FlowCaptureReference:
                case OperationKind.IsNull:
                    return true;
            }

            Assert.True(false, $"Unhandled node kind OperationKind.{n.Kind}");
            return false;
        }
    }
}
