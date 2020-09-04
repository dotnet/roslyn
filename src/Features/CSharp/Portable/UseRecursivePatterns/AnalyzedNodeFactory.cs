// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    using static AnalyzedNode;

    // Create a common tree from expressions as well as patterns.
    // Since the tree would be similar in both cases, we can later rewrite
    // the whole thing as either a pattern or expression if needed.
    internal static class AnalyzedNodeFactory
    {
        public static AnalyzedNode? Visit(IOperation operation)
        {
            return VisitCore(operation, input: null);
        }

        private static AnalyzedNode? VisitCore(IOperation operation, Evaluation? input)
        {
            Debug.Assert(input is null || operation is IPatternOperation || operation is IInvalidOperation);

            return operation switch
            {
                IBinaryOperation op => VisitBinaryOperator(op),
                IPatternOperation op => VisitPatternOperation(input, op),
                IIsTypeOperation op => new Type(VisitInput(op.ValueOperand), op.TypeOperand),
                IIsPatternOperation op => VisitPatternOperation(VisitInput(op.Value), op.Pattern),
                IPatternCaseClauseOperation op => VisitBinary(op.Pattern, op.Guard, disjunctive: false, input: null),
                ISwitchExpressionArmOperation op => VisitBinary(op.Pattern, op.Guard, disjunctive: false, input: null),
                IUnaryOperation { OperatorKind: UnaryOperatorKind.Not } op => VisitNotOperator(op),
                var op => VisitInput(op)
            };
        }

        [return: NotNullIfNotNull("operation")]
        private static Evaluation? VisitInput(IOperation? operation)
        {
            Debug.Assert(!(operation is IConditionalAccessInstanceOperation));

            if (operation is null)
                return null;

            return VisitNode(operation) ?? new OperationEvaluation(operation);

            static Evaluation? VisitNode(IOperation operation)
            {
                return operation switch
                {
                    IMemberReferenceOperation op when IsBaseExpressionMemberAccess(op) => null,
                    ILocalReferenceOperation op => new Variable(input: null, op.Local, op.Syntax),
                    IFieldReferenceOperation op => new MemberEvaluation(VisitInput(op.Instance), op),
                    IPropertyReferenceOperation op when op.Property.IsIndexer => VisitIndexerReference(op),
                    IPropertyReferenceOperation op => VisitNullableTypeMemberAccess(op) ??
                                                      new MemberEvaluation(VisitInput(op.Instance), op),
                    IConditionalAccessOperation op => VisitConditionalAccess(VisitInput(op.Operation), op.WhenNotNull),
                    IConversionOperation op when op.Conversion.IsUserDefined => null,
                    IConversionOperation op when op.Conversion.IsImplicit => VisitInput(op.Operand),
                    IConversionOperation op => new Type(VisitInput(op.Operand), op.Type),
                    _ => null,
                };
            }

            static bool IsBaseExpressionMemberAccess(IMemberReferenceOperation operation)
            {
                // We cannot use `base` as an standalone expression. So if we encounter `base.x.y.z` we
                // will walk down until the very last member reference, but not the `base` node itself,
                // resulting to the following graph:
                //
                //  base.x <- y <- z
                //
                return operation.Instance is IInstanceReferenceOperation instance &&
                       instance.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance &&
                       instance.Syntax.IsKind(SyntaxKind.BaseExpression);
            }

            static Evaluation? VisitConditionalAccess(Evaluation? input, IOperation operation)
            {
                if (input is null)
                    return null;

                return operation switch
                {
                    IFieldReferenceOperation op => new MemberEvaluation(input, op),
                    IPropertyReferenceOperation op => new MemberEvaluation(input, op),
                    IConditionalAccessOperation op => VisitConditionalAccess(
                        VisitConditionalAccess(input, op.Operation), op.WhenNotNull),
                    _ => null
                };
            }

            static Evaluation? VisitIndexerReference(IPropertyReferenceOperation op)
            {
                // If this is the `ITuple.this[int]` indexer with a constant argument,
                // record it as a separate node. We'll try to rewrite it as a positional pattern.

                var indexer = op.Property;
                Debug.Assert(indexer.IsIndexer);
                var containingType = indexer.ContainingType;
                if (containingType != null &&
                    containingType.Equals(GetITupleType(op)) &&
                    op.Arguments.Length == 1 &&
                    IsConstant(op.Arguments[0].Value, out var constant) &&
                    constant.Type.SpecialType == SpecialType.System_Int32)
                {
                    return new IndexEvaluation(VisitInput(op.Instance), indexer, (int)constant.ConstantValue.Value, op.Syntax);
                }

                return null;
            }

            static Evaluation? VisitNullableTypeMemberAccess(IPropertyReferenceOperation op)
            {
                if (op.Property.ContainingType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
                    return null;

                // Special cases for a `Nullable<T>` receiver:
                return op.Property.Name switch
                {
                    // (1) Checking `HasValue` is interpreted as a null check
                    nameof(Nullable<int>.HasValue) => new NotNull(VisitInput(op.Instance)),
                    // (2) Skipping `Value` access since in patterns this is implicit
                    nameof(Nullable<int>.Value) => VisitInput(op.Instance),
                    _ => null
                };
            }
        }

        private static INamedTypeSymbol? GetITupleType(IOperation op)
        {
            return op.SemanticModel.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ITuple");
        }

        private static AnalyzedNode? VisitNotOperator(IUnaryOperation op)
        {
            var operand = Visit(op.Operand);
            if (operand is null || operand is OperationEvaluation)
                return null;
            return Not.Create(operand);
        }
        private static AnalyzedNode? VisitBinary(IOperation left, IOperation right, bool disjunctive, Evaluation? input)
        {
            var leftNode = VisitCore(left, input);
            if (leftNode is null)
                return null;
            var rightNode = VisitCore(right, input);
            if (rightNode is null)
                return null;
            return Sequence.Create(disjunctive, leftNode, rightNode);
        }

        private static AnalyzedNode? VisitConditionalOrOperator(IBinaryOperation op)
            => VisitBinary(op.LeftOperand, op.RightOperand, disjunctive: true, input: null);

        private enum ConstantResult { None, Left, Right }

        private static (ConstantResult, IOperation) DetermineConstant(IBinaryOperation operation)
        {
            return (operation.LeftOperand, operation.RightOperand) switch
            {
                var (_, v) when IsConstant(v, out var op) => (ConstantResult.Right, op),
                var (v, _) when IsConstant(v, out var op) => (ConstantResult.Left, op),
                _ => default,
            };
        }

        private static bool IsConstant(IOperation operation, out IOperation constant)
        {
            constant = Unwrap(operation);
            return constant.ConstantValue.HasValue;

            static IOperation Unwrap(IOperation operation)
            {
                while (operation is IConversionOperation op)
                    operation = op.Operand;
                return operation;
            }
        }

        private static AnalyzedNode? VisitEqualsOperator(IBinaryOperation op)
        {
            return DetermineConstant(op) switch
            {
                (ConstantResult.Right, var value) => new Constant(VisitInput(op.LeftOperand), value),
                (ConstantResult.Left, var value) => new Constant(VisitInput(op.RightOperand), value),
                _ => null,
            };
        }

        private static AnalyzedNode? VisitNotEqualsOperator(IBinaryOperation op)
        {
            // To be able to elide unnecessary null-checks we need to capture it as a separate node.
            return VisitNotEqualsNull(op) ??
                   VisitNotEquals(op);
        }

        private static AnalyzedNode? VisitNotEquals(IBinaryOperation op)
            => Not.Create(VisitEqualsOperator(op));

        private static AnalyzedNode? VisitNotEqualsNull(IBinaryOperation op)
        {
            return DetermineNullConstant(op) switch
            {
                ConstantResult.Right => new NotNull(VisitInput(op.LeftOperand)),
                ConstantResult.Left => new NotNull(VisitInput(op.RightOperand)),
                _ => null,
            };

            static ConstantResult DetermineNullConstant(IBinaryOperation operation)
            {
                var (result, value) = DetermineConstant(operation);
                return result != ConstantResult.None && value.ConstantValue.Value is null ? result : default;
            }
        }

        private static AnalyzedNode? VisitBinaryOperator(IBinaryOperation op)
        {
            switch (op.OperatorKind)
            {
                case BinaryOperatorKind.Equals:
                    return VisitEqualsOperator(op);

                case BinaryOperatorKind.NotEquals:
                    return VisitNotEqualsOperator(op);

                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.LessThanOrEqual:
                case BinaryOperatorKind.GreaterThanOrEqual:
                case BinaryOperatorKind.GreaterThan:
                    return VisitRelationalOperator(op);

                case BinaryOperatorKind.ConditionalOr:
                    return VisitConditionalOrOperator(op);

                case BinaryOperatorKind.ConditionalAnd:
                    return VisitConditionalAndOperator(op);
            }

            return null;
        }

        private static AnalyzedNode? VisitConditionalAndOperator(IBinaryOperation op)
        {
            return VisitBinary(op.LeftOperand, op.RightOperand, disjunctive: false, input: null);
        }

        private static AnalyzedNode? VisitRelationalOperator(IBinaryOperation op)
        {
            return DetermineConstant(op) switch
            {
                (ConstantResult.Right, var value) => new Relational(VisitInput(op.LeftOperand), op.OperatorKind, value),
                (ConstantResult.Left, var value) => new Relational(VisitInput(op.RightOperand), Flip(op.OperatorKind), value),
                _ => null,
            };

            static BinaryOperatorKind Flip(BinaryOperatorKind operatorKind)
            {
                return operatorKind switch
                {
                    BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThan,
                    BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThanOrEqual,
                    BinaryOperatorKind.GreaterThanOrEqual => BinaryOperatorKind.LessThanOrEqual,
                    BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThan,
                    BinaryOperatorKind.NotEquals => BinaryOperatorKind.NotEquals,
                    var v => throw ExceptionUtilities.UnexpectedValue(v)
                };
            }
        }

        private static AnalyzedNode? VisitPatternOperation(Evaluation? input, IPatternOperation pattern)
            => VisitPatternOperation(ref input, pattern);

        private static AnalyzedNode? VisitPatternOperation(ref Evaluation? input, IPatternOperation pattern)
        {
            var conversion = pattern.SemanticModel.Compilation.ClassifyConversion(pattern.InputType, pattern.NarrowedType);
            if (!conversion.IsImplicit)
            {
                // If the type is significant, we record it as an evaluation on the original input.
                // This helps to keep the correct order with regard to the narrowed type when rewriting.
                input = new Type(input, pattern.NarrowedType);
            }

            return pattern switch
            {
                IBinaryPatternOperation op => VisitBinaryPatternOperation(input, op),
                IConstantPatternOperation op => new Constant(input, op.Value),
                IRelationalPatternOperation op => new Relational(input, op.OperatorKind, op.Value),
                ITypePatternOperation _ => input, // The matched type is already captured in the input
                IDiscardPatternOperation _ => True.Instance, // A discard pattern always matches
                INegatedPatternOperation op => Not.Create(VisitPatternOperation(input, op.Pattern)),
                IDeclarationPatternOperation op => VisitDeclarationPatternOperation(input, op),
                IRecursivePatternOperation op => VisitRecursivePatternOperation(input, op),
                var op => throw ExceptionUtilities.UnexpectedValue(op.Kind)
            };
        }

        private static AnalyzedNode? VisitBinaryPatternOperation(Evaluation? input, IBinaryPatternOperation op)
        {
            var disjunctive = op.OperatorKind == BinaryOperatorKind.Or;

            // Input to an and-pattern is passed through to the RHS to capture the narrowed-type of LHS
            var leftNode = disjunctive
                ? VisitPatternOperation(input, op.LeftPattern)
                : VisitPatternOperation(ref input, op.LeftPattern);

            if (leftNode is null)
                return null;

            var rightNode = VisitPatternOperation(input, op.RightPattern);
            if (rightNode is null)
                return null;

            return Sequence.Create(disjunctive, leftNode, rightNode);
        }

        private static AnalyzedNode? VisitRecursivePatternOperation(Evaluation? input, IRecursivePatternOperation operation)
        {
            var tests = ArrayBuilder<AnalyzedNode>.GetInstance();

            if (operation.DeclaredSymbol != null)
                tests.Add(new Variable(input, operation.DeclaredSymbol));

            if (TryAddPropertySubpatterns(input, operation, tests) &&
                TryAddPositionalSubpatterns(input, operation, tests))
            {
                return AndSequence.Create(tests);
            }

            tests.Free();

            return null;

            static bool TryAddPropertySubpatterns(Evaluation? input, IRecursivePatternOperation operation, ArrayBuilder<AnalyzedNode> tests)
            {
                foreach (var property in operation.PropertySubpatterns)
                {
                    Evaluation? evaluation = property.Member switch
                    {
                        IFieldReferenceOperation op => new MemberEvaluation(input, op),
                        IPropertyReferenceOperation op => new MemberEvaluation(input, op),
                        _ => null
                    };
                    if (evaluation is null)
                        return false;
                    var pattern = VisitCore(property.Pattern, evaluation);
                    if (pattern is null)
                        return false;
                    tests.Add(pattern);
                }
                return true;
            }

            static bool TryAddPositionalSubpatterns(Evaluation? input, IRecursivePatternOperation operation, ArrayBuilder<AnalyzedNode> tests)
            {
                var subpatternCount = operation.DeconstructionSubpatterns.Length;
                switch (operation.DeconstructSymbol)
                {
                    case IMethodSymbol method:
                        {
                            Debug.Assert(method.Name == WellKnownMemberNames.DeconstructMethodName);
                            Debug.Assert(method.Parameters.Length - (method.IsExtensionMethod ? 1 : 0) == subpatternCount);
                            var deconstructEvaluation = new DeconstructEvaluation(input, method);
                            for (var index = 0; index < subpatternCount; index++)
                            {
                                var outVariableEvaluation = new OutVariableEvaluation(deconstructEvaluation, index);
                                var subpattern = VisitCore(operation.DeconstructionSubpatterns[index], outVariableEvaluation);
                                if (subpattern is null)
                                    return false;
                                tests.Add(subpattern);
                            }
                            break;
                        }

                    case ITypeSymbol iTupleType:
                        {
                            Debug.Assert(iTupleType.Equals(GetITupleType(operation)));
                            var indexer = iTupleType.GetIndexers().Single();
                            for (var index = 0; index < subpatternCount; index++)
                            {
                                var indexEvaluation = new IndexEvaluation(input, indexer, index);
                                var subpattern = VisitCore(operation.DeconstructionSubpatterns[index], indexEvaluation);
                                if (subpattern is null)
                                    return false;
                                tests.Add(subpattern);
                            }
                            break;
                        }

                    case null when operation.MatchedType is INamedTypeSymbol { IsTupleType: true } tupleType:
                        {
                            Debug.Assert(tupleType.Equals(operation.InputType));
                            Debug.Assert(tupleType.Arity == subpatternCount);
                            for (var index = 0; index < subpatternCount; index++)
                            {
                                var field = (IFieldSymbol)tupleType.GetMembers($"Item{index + 1}").Single();
                                var fieldEvaluation = new MemberEvaluation(input, field);
                                var subpattern = VisitCore(operation.DeconstructionSubpatterns[index], fieldEvaluation);
                                if (subpattern is null)
                                    return false;
                                tests.Add(subpattern);
                            }
                            break;
                        }
                }

                return true;
            }
        }

        private static AnalyzedNode VisitDeclarationPatternOperation(Evaluation? input, IDeclarationPatternOperation op)
        {
            return op.DeclaredSymbol != null
                ? new Variable(input, op.DeclaredSymbol)
                : True.Instance;
        }
    }
}
