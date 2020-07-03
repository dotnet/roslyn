// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    using static AnalyzedNode;

    internal class AnalyzedNodeFactory
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
                IPatternOperation op => VisitPatternOperation(op, input),
                IIsTypeOperation op => new Type(VisitInput(op.ValueOperand), op.TypeOperand),
                IIsPatternOperation op => VisitPatternOperation(op.Pattern, VisitInput(op.Value)),
                IPatternCaseClauseOperation op => VisitBinary(op.Pattern, op.Guard, disjunctive: false, input: null),
                ISwitchExpressionArmOperation op => VisitBinary(op.Pattern, op.Guard, disjunctive: false, input: null),
                IUnaryOperation { OperatorKind: UnaryOperatorKind.Not } op when ShouldVisit(op) => Not.Create(Visit(op.Operand)),
                var op => VisitInput(op)
            };

            static bool ShouldVisit(IUnaryOperation operation)
            {
                switch (operation.Operand)
                {
                    case IIsPatternOperation _:
                    case IIsTypeOperation _:
                    case IBinaryOperation _:
                    case IFieldReferenceOperation _:
                    case IPropertyReferenceOperation _:
                    case IUnaryOperation op when ShouldVisit(op):
                        return true;
                }

                return false;
            }
        }

        private static Evaluation? VisitInput(IOperation? operation)
        {
            Debug.Assert(!(operation is IConditionalAccessInstanceOperation), "!(operation is IConditionalAccessInstanceOperation)");

            if (operation is null)
                return null;

            return VisitEvaluation(operation) ?? new OperationEvaluation(operation);

            static Evaluation? VisitEvaluation(IOperation operation)
            {
                return operation switch
                {
                    ILocalReferenceOperation op => new Variable(input: null, op.Local, op.Syntax),
                    IFieldReferenceOperation op => new FieldEvaluation(VisitInput(op.Instance), op.Field, op.Syntax),
                    IPropertyReferenceOperation op when op.Property.IsIndexer => VisitIndexerReference(op),
                    IPropertyReferenceOperation op => VisitNullablePropertyReference(op) ??
                                                      new PropertyEvaluation(VisitInput(op.Instance), op.Property, op.Syntax),
                    IConditionalAccessOperation op => VisitConditionalAccess(VisitInput(op.Operation), op.WhenNotNull),
                    IConversionOperation op when op.Conversion.IsImplicit => VisitInput(op.Operand),
                    IConversionOperation op when op.OperatorMethod is null => new Type(VisitInput(op.Operand), op.Type),
                    // UNDONE: This is only valid if it is a 'this' reference, however,
                    // UNDONE: we cannot distinguish between 'this' and 'base' using IOperation
                    IInstanceReferenceOperation _ => null,
                    _ => null,
                };
            }

            static Evaluation? VisitConditionalAccess(Evaluation? input, IOperation operation)
            {
                if (input is null)
                    return null;

                return operation switch
                {
                    IFieldReferenceOperation op => new FieldEvaluation(input, op.Field),
                    IPropertyReferenceOperation op => new PropertyEvaluation(input, op.Property),
                    IConditionalAccessOperation op => VisitConditionalAccess(VisitConditionalAccess(input, op.Operation), op.WhenNotNull),
                    _ => null
                };
            }

            static Evaluation? VisitIndexerReference(IPropertyReferenceOperation op)
            {
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

            static Evaluation? VisitNullablePropertyReference(IPropertyReferenceOperation op)
            {
                if (op.Property.ContainingType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
                    return null;

                return op.Property.Name switch
                {
                    nameof(Nullable<int>.HasValue) => new NotNull(VisitInput(op.Instance)),
                    nameof(Nullable<int>.Value) => VisitInput(op.Instance),
                    _ => null
                };
            }
        }

        private static INamedTypeSymbol? GetITupleType(IOperation op)
        {
            return op.SemanticModel.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ITuple");
        }

        private static AnalyzedNode? VisitBinary(IOperation left, IOperation right, bool disjunctive, Evaluation? input)
        {
            var leftNode = VisitCore(left, input);
            if (leftNode is null)
                return null;
            var rightNode = VisitCore(right, input);
            if (rightNode is null)
                return null;
            var tests = ArrayBuilder<AnalyzedNode>.GetInstance(2);
            tests.Add(leftNode);
            tests.Add(rightNode);
            return Sequence.Create(disjunctive, tests);
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

        private static AnalyzedNode? VisitPatternOperation(IPatternOperation pattern, Evaluation? input)
        {
            return pattern switch
            {
                IBinaryPatternOperation op => VisitBinaryPatternOperation(input, op),
                IConstantPatternOperation op => new Constant(input, op.Value),
                IRelationalPatternOperation op => new Relational(input, op.OperatorKind, op.Value),
                ITypePatternOperation op => new Type(input, op.Type),
                IDiscardPatternOperation _ => True.Instance,
                INegatedPatternOperation op => Not.Create(VisitPatternOperation(op.Pattern, input)),
                IDeclarationPatternOperation op => VisitDeclarationPatternOperation(input, op),
                IRecursivePatternOperation op => VisitRecursivePatternOperation(input, op),
                IInvalidOperation _ => null,
                var op => throw ExceptionUtilities.UnexpectedValue(op.Kind)
            };
        }

        private static AnalyzedNode? VisitBinaryPatternOperation(Evaluation? input, IBinaryPatternOperation op)
        {
            return VisitBinary(op.LeftPattern, op.RightPattern, op.OperatorKind == BinaryOperatorKind.Or, input);
        }

        private static AnalyzedNode? VisitRecursivePatternOperation(Evaluation? input, IRecursivePatternOperation operation)
        {
            var tests = ArrayBuilder<AnalyzedNode>.GetInstance();
            if (operation.DeclaredSymbol != null)
                tests.Add(new Variable(input, operation.DeclaredSymbol));
            if (operation.MatchedType != null && !IsImplicitlyConvertibleFromInputType(operation, operation.MatchedType))
                tests.Add(new Type(input, operation.MatchedType));

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
#pragma warning disable IDE0007 // Use implicit type
                    Evaluation? evaluation = property.Member switch
#pragma warning restore IDE0007 // Use implicit type
                    {
                        IFieldReferenceOperation op => new FieldEvaluation(input, op.Field),
                        IPropertyReferenceOperation op => new PropertyEvaluation(input, op.Property),
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
                // We don't cover parameterless Deconstruct methods, but it should be rare.
                if (subpatternCount == 0)
                    return true;
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
                                var fieldEvaluation = new FieldEvaluation(input, field);
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
            var tests = ArrayBuilder<AnalyzedNode>.GetInstance(2);
            if (op.DeclaredSymbol != null)
                tests.Add(new Variable(input, op.DeclaredSymbol));
            if (op.MatchedType != null && !IsImplicitlyConvertibleFromInputType(op, op.MatchedType))
                tests.Add(new Type(input, op.MatchedType));
            return AndSequence.Create(tests);
        }

        private static bool IsImplicitlyConvertibleFromInputType(IPatternOperation op, ITypeSymbol matchedType)
        {
            return op.SemanticModel.Compilation.ClassifyConversion(op.InputType, matchedType).IsImplicit;
        }
    }
}
