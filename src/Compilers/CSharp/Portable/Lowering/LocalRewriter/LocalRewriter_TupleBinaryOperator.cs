// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        /// <summary>
        /// Rewrite `GetTuple() == (1, 2)` to `tuple.Item1 == 1 &amp;&amp; tuple.Item2 == 2`.
        /// Also supports the != operator, nullable and nested tuples.
        ///
        /// Note that all the side-effects for visible expressions are evaluated first and from left to right. The initialization phase
        /// contains side-effects for:
        /// - single elements in tuple literals, like `a` in `(a, ...) == (...)` for example
        /// - nested expressions that aren't tuple literals, like `GetTuple()` in `(..., GetTuple()) == (..., (..., ...))`
        /// On the other hand, `Item1` and `Item2` of `GetTuple()` are not saved as part of the initialization phase of `GetTuple() == (..., ...)`
        ///
        /// Element-wise conversions occur late, together with the element-wise comparisons. They may not be evaluated.
        /// </summary>
        /// There are 2 cases:
        /// - tuple literal cannot be null and it produces as many temps as elements
        /// - tuple types or nullable tuple types produce a single temp (and no further temps are initialized)
        public override BoundNode VisitTupleBinaryOperator(BoundTupleBinaryOperator node)
        {
            var boolType = node.Type; // we can re-use the bool type
            var initialEffectsAndTemps = TupleOperatorSideEffectsAndTemps.GetInstance();

            var returnValue = RewriteTupleOperator(node.Operators, node.Left, node.Right, boolType, initialEffectsAndTemps, node.OperatorKind, inLeftLiteral: true, inRightLiteral: true);

            var (effects, temps) = initialEffectsAndTemps.ToImmutableAndFree();
            var result = _factory.Sequence(temps, effects, returnValue);
            return result;
        }

        private BoundExpression RewriteTupleOperator(TupleBinaryOperatorInfo @operator,
            BoundExpression left, BoundExpression right, TypeSymbol boolType,
            TupleOperatorSideEffectsAndTemps initialEffectsAndTemps, BinaryOperatorKind operatorKind, bool inLeftLiteral, bool inRightLiteral)
        {
            if (@operator.IsSingle())
            {
                return RewriteTupleSingleOperator((TupleBinaryOperatorInfo.Single)@operator, left, right, boolType, operatorKind);
            }
            else
            {
                return RewriteTupleNestedOperators((TupleBinaryOperatorInfo.Multiple)@operator, left, right, boolType, initialEffectsAndTemps, operatorKind, inLeftLiteral, inRightLiteral);
            }
        }

        private BoundExpression RewriteTupleNestedOperators(TupleBinaryOperatorInfo.Multiple operators, BoundExpression left, BoundExpression right,
            TypeSymbol boolType, TupleOperatorSideEffectsAndTemps initialEffectsAndTemps, BinaryOperatorKind operatorKind, bool inLeftLiteral, bool inRightLiteral)
        {
            // If either left or right is nullable, produce:
            //
            //      // outer sequence
            //      var leftHasValue = left.HasValue; (or true if !leftNullable)
            //      var rightHasValue = right.HasValue; (or true if !rightNullable)
            //      leftHasValue == rightHasValue
            //          ? leftHasValue ? ... inner sequence ... : true/false
            //          : false/true
            //
            // where inner sequence is:
            //      var leftValue = left.GetValueOrDefault(); (or left if !leftNullable)
            //      var rightValue = right.GetValueOrDefault(); (or right if !rightNullable)
            //      ... logical expression using leftValue and rightValue ...
            //
            // and true/false and false/true depend on operatorKind (== vs. !=)
            //
            // and temps are created an initialized with early side-effects for:
            //  - elements from left and right, in case of a tuple literal
            //  - left and right, in case of a tuple or nullable tuple type
            //
            // But if neither is nullable, then just produce the inner sequence.
            //
            // Note: all the temps are created in a single bucket (rather than different scopes of applicability) for simplicity

            var outerEffects = ArrayBuilder<BoundExpression>.GetInstance();
            var innerEffects = ArrayBuilder<BoundExpression>.GetInstance();

            BoundExpression leftHasValue;
            BoundExpression leftValue;

            var coreLeft = WithoutImplicitNullableConversions(left);
            var isLeftNullable = coreLeft.Type.IsNullableType();
            if (isLeftNullable)
            {
                BoundExpression savedLeft = EvaluateSideEffectingArgumentToTemp(VisitExpression(left), outerEffects, ref initialEffectsAndTemps.temps);
                leftHasValue = MakeHasValueTemp(savedLeft, initialEffectsAndTemps.temps, outerEffects);
                leftValue = MakeValueOrDefaultTemp(savedLeft, initialEffectsAndTemps.temps, innerEffects);
                inLeftLiteral = false;
            }
            else
            {
                leftHasValue = MakeBooleanConstant(left.Syntax, true);
                leftValue = SaveTupleIfNotLiteral(coreLeft, ref inLeftLiteral, initialEffectsAndTemps, isRight: false);
            }

            BoundExpression rightHasValue;
            BoundExpression rightValue;

            var coreRight = WithoutImplicitNullableConversions(right);
            var isRightNullable = coreRight.Type.IsNullableType();
            if (isRightNullable)
            {
                BoundExpression savedRight = EvaluateSideEffectingArgumentToTemp(VisitExpression(right), outerEffects, ref initialEffectsAndTemps.temps);
                rightHasValue = MakeNullableHasValue(savedRight.Syntax, savedRight); // no need for local for right.HasValue since used once
                rightValue = MakeValueOrDefaultTemp(savedRight, initialEffectsAndTemps.temps, innerEffects);
                inRightLiteral = false;
            }
            else
            {
                rightHasValue = MakeBooleanConstant(right.Syntax, true);
                rightValue = SaveTupleIfNotLiteral(coreRight, ref inRightLiteral, initialEffectsAndTemps, isRight: true);
            }

            BoundExpression logicalExpression = RewriteNonNullableNestedTupleOperators(operators, leftValue, rightValue, boolType, initialEffectsAndTemps.temps, innerEffects, initialEffectsAndTemps, operatorKind, inLeftLiteral, inRightLiteral);

            BoundExpression innerSequence = _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, innerEffects.ToImmutableAndFree(), logicalExpression);

            if (!isLeftNullable && !isRightNullable)
            {
                return innerSequence;
            }

            // outer sequence:
            //      leftHasValue == rightHasValue
            //          ? leftHasValue ? ... inner sequence ... : true/false
            //          : false/true
            bool boolValue = operatorKind == BinaryOperatorKind.Equal; // true/false
            BoundExpression outerSequence =
                _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, outerEffects.ToImmutableAndFree(),
                    _factory.Conditional(
                        _factory.Binary(BinaryOperatorKind.Equal, boolType, leftHasValue, rightHasValue),
                        _factory.Conditional(leftHasValue, innerSequence, MakeBooleanConstant(right.Syntax, boolValue), boolType),
                        MakeBooleanConstant(right.Syntax, !boolValue),
                        boolType));

            return outerSequence;
        }

        private BoundExpression WithoutImplicitNullableConversions(BoundExpression expr)
        {
            while (true)
            {
                if (expr.Kind != BoundKind.Conversion)
                {
                    return expr;
                }

                var conversion = (BoundConversion)expr;
                if (!conversion.Conversion.IsImplicit || !conversion.Conversion.IsNullable)
                {
                    return expr;
                }

                expr = conversion.Operand;
            }
        }

        private BoundExpression MakeTemp(BoundExpression expression, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> effects)
        {
            BoundLocal temp = _factory.StoreToTemp(VisitExpression(expression), out BoundAssignmentOperator assignmentToTemp);
            effects.Add(assignmentToTemp);
            temps.Add(temp.LocalSymbol);
            return temp;
        }

        /// <summary>
        /// Returns a temp which is initialized with lowered-expression.HasValue
        /// </summary>
        private BoundExpression MakeHasValueTemp(BoundExpression expression, ArrayBuilder<LocalSymbol> temps,
            ArrayBuilder<BoundExpression> effects)
        {
            BoundExpression hasValueCall = MakeNullableHasValue(expression.Syntax, expression);
            return MakeTemp(hasValueCall, temps, effects);
        }

        /// <summary>
        /// Returns a temp which is initialized with lowered-expression.GetValueOrDefault()
        /// </summary>
        private BoundExpression MakeValueOrDefaultTemp(BoundExpression expression,
            ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> effects)
        {
            BoundExpression valueOrDefaultCall = MakeOptimizedGetValueOrDefault(expression.Syntax, expression);
            return MakeTemp(valueOrDefaultCall, temps, effects);
        }

        /// <summary>
        /// Produces a chain of equality (or inequality) checks combined logically with AND (or OR)
        /// </summary>
        private BoundExpression RewriteNonNullableNestedTupleOperators(TupleBinaryOperatorInfo.Multiple operators,
            BoundExpression left, BoundExpression right, TypeSymbol type,
            ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> effects, TupleOperatorSideEffectsAndTemps effectAndTemps, BinaryOperatorKind operatorKind, bool inLeftLiteral, bool inRightLiteral)
        {
            Debug.Assert(left.Type?.IsNullableType() == false);
            Debug.Assert(right.Type?.IsNullableType() == false);

            ImmutableArray<TupleBinaryOperatorInfo> nestedOperators = operators.Operators;

            BoundExpression currentResult = null;
            for (int i = 0; i < nestedOperators.Length; i++)
            {
                BoundExpression leftElement = GetTuplePart(left, i, nestedOperators, temps, effects, effectAndTemps, isRight: false);
                BoundExpression rightElement = GetTuplePart(right, i, nestedOperators, temps, effects, effectAndTemps, isRight: true);
                var nextLogicalOperand = RewriteTupleOperator(nestedOperators[i], leftElement, rightElement, type, effectAndTemps, operatorKind, inLeftLiteral, inRightLiteral);
                if (currentResult is null)
                {
                    currentResult = nextLogicalOperand;
                }
                else
                {
                    var logicalOperator = operatorKind == BinaryOperatorKind.Equal ? BinaryOperatorKind.LogicalBoolAnd : BinaryOperatorKind.LogicalBoolOr;
                    currentResult = _factory.Binary(logicalOperator, type, currentResult, nextLogicalOperand);
                }
            }

            return currentResult;
        }

        private BoundExpression SaveTupleIfNotLiteral(BoundExpression tuple, ref bool inLiteral, TupleOperatorSideEffectsAndTemps effectsAndTemps, bool isRight)
        {
            if (TupleNeedsSaving(tuple) && inLiteral)
            {
                var tupleTemp = EvaluateSideEffectingArgumentToTemp(VisitExpression(tuple), isRight ? effectsAndTemps.rightInit : effectsAndTemps.leftInit, ref effectsAndTemps.temps);
                inLiteral = false;
                return tupleTemp;
            }

            return tuple;
        }

        /// <summary>
        /// The elements of tuple literals are evaluated and saved individually.
        /// But for other tuple expressions, like `GetTuple()` or `GetNullableTuple()`, we need to save the tuple itself. The elements will lazily be accessed later.
        /// </summary>
        private static bool TupleNeedsSaving(BoundExpression tuple)
        {
            if (IsTupleExpression(tuple.Kind))
            {
                return false;
            }

            if (tuple.Kind == BoundKind.Conversion)
            {
                var tupleConversion = (BoundConversion)tuple;
                if (tupleConversion.Conversion.Kind == ConversionKind.ImplicitNullable)
                {
                    return TupleNeedsSaving(tupleConversion.Operand);
                }

                if ((tupleConversion.Conversion.Kind == ConversionKind.ImplicitTupleLiteral || tupleConversion.Conversion.Kind == ConversionKind.Identity)
                    && IsTupleExpression(tupleConversion.Operand.Kind))
                {
                    return false;
                }
            }

            Debug.Assert(tuple.Type?.IsTupleType == true);
            return true;
        }

        // The tuple itself was already saved to temp if necessary
        // If getting the element of a tuple literal corresponding to a single operator, we save it into initialization side-effects
        // If getting the element of a tuple literal corresponding to a nested operator, we just return it
        // If getting the element of a tuple type, we save it into the local side-effects
        private BoundExpression GetTuplePart(BoundExpression tuple, int i, ImmutableArray<TupleBinaryOperatorInfo> operators,
            ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> effects, TupleOperatorSideEffectsAndTemps initialEffectsAndTemps, bool isRight)
        {
            // Example:
            // (1, 2) == (1, 2);
            if (IsTupleExpression(tuple.Kind))
            {
                return MakeTemp(((BoundTupleExpression)tuple).Arguments[i], operators[i], initialEffectsAndTemps, isRight);
            }

            // Example:
            // (1L, 2L == (1, 2);
            // (1, "hello") == (1, null);
            if (tuple.Kind == BoundKind.Conversion)
            {
                var tupleConversion = (BoundConversion)tuple;
                if (tupleConversion.Conversion.Kind == ConversionKind.ImplicitNullable)
                {
                    return GetTuplePart(tupleConversion.Operand, i, operators, temps, effects, initialEffectsAndTemps, isRight);
                }

                if ((tupleConversion.Conversion.Kind == ConversionKind.ImplicitTupleLiteral || tupleConversion.Conversion.Kind == ConversionKind.Identity)
                    && IsTupleExpression(tupleConversion.Operand.Kind))
                {
                    var coreArgument = WithoutImplicitNullableConversions(((BoundTupleExpression)tupleConversion.Operand).Arguments[i]);
                    return MakeTemp(coreArgument, operators[i], initialEffectsAndTemps, isRight);
                }
            }

            Debug.Assert(tuple.Type.IsTupleType);

            // Example:
            // t == GetTuple();
            // t == ((byte, byte)) (1, 2);
            // t == ((short, short))((int, int))(1L, 2L);
            return MakeTupleFieldAccessAndReportUseSiteDiagnostics(tuple, tuple.Syntax, tuple.Type.TupleElements[i]);
        }

        private BoundExpression MakeTemp(BoundExpression expr, TupleBinaryOperatorInfo op, TupleOperatorSideEffectsAndTemps initialEffectsAndTemps, bool isRight)
        {
            if (op.IsSingle())
            {
                return EvaluateSideEffectingArgumentToTemp(VisitExpression(expr), isRight ? initialEffectsAndTemps.rightInit : initialEffectsAndTemps.leftInit, ref initialEffectsAndTemps.temps);
            }
            else
            {
                return expr;
            }
        }

        /// <summary>
        /// Produce an element-wise comparison and logic to ensure the result is a bool type.
        ///
        /// If an element-wise comparison doesn't return bool, then:
        /// - if it is dynamic, we'll do `!(comparisonResult.false)` or `comparisonResult.true`
        /// - if it implicitly converts to bool, we'll just do the conversion
        /// - otherwise, we'll do `!(comparisonResult.false)` or `comparisonResult.true` (as we'd do for `if` or `while`)
        /// </summary>
        private BoundExpression RewriteTupleSingleOperator(TupleBinaryOperatorInfo.Single single,
            BoundExpression left, BoundExpression right, TypeSymbol boolType, BinaryOperatorKind operatorKind)
        {
            if (single.Kind.IsDynamic())
            {
                // Produce
                // !((left == right).op_false)
                // (left != right).op_true

                var dynamicResult = _dynamicFactory.MakeDynamicBinaryOperator(single.Kind, left, right, isCompoundAssignment: false, _compilation.DynamicType).ToExpression();
                if (operatorKind == BinaryOperatorKind.Equal)
                {
                    return _factory.Not(MakeUnaryOperator(UnaryOperatorKind.DynamicFalse, left.Syntax, method: null, dynamicResult, boolType));
                }
                else
                {
                    return MakeUnaryOperator(UnaryOperatorKind.DynamicTrue, left.Syntax, method: null, dynamicResult, boolType);
                }
            }
            else
            {
                BoundExpression binary = MakeBinaryOperator(_factory.Syntax, single.Kind, left, right, single.MethodSymbolOpt?.ReturnType ?? boolType, single.MethodSymbolOpt);
                UnaryOperatorSignature boolOperator = single.BoolOperator;
                Conversion boolConversion = single.BoolConversion;

                BoundExpression result;
                if (boolOperator.Kind != UnaryOperatorKind.Error)
                {
                    // Produce
                    // !((left == right).op_false)
                    // (left != right).op_true
                    BoundExpression convertedBinary = MakeConversionNode(_factory.Syntax, binary, boolConversion, boolOperator.OperandType, @checked: false);

                    Debug.Assert(boolOperator.ReturnType.SpecialType == SpecialType.System_Boolean);
                    result = MakeUnaryOperator(boolOperator.Kind, binary.Syntax, boolOperator.Method, convertedBinary, boolType);

                    if (operatorKind == BinaryOperatorKind.Equal)
                    {
                        result = _factory.Not(result);
                    }
                }
                else if (boolConversion != Conversion.Identity)
                {
                    // Produce
                    // (bool)(left == right)
                    // (bool)(left != right)
                    result = MakeConversionNode(_factory.Syntax, binary, boolConversion, boolType, @checked: false);
                }
                else
                {
                    result = binary;
                }

                return result;
            }
        }

        private class TupleOperatorSideEffectsAndTemps
        {
            internal ArrayBuilder<BoundExpression> leftInit;
            internal ArrayBuilder<BoundExpression> rightInit;
            internal ArrayBuilder<LocalSymbol> temps;

            internal static TupleOperatorSideEffectsAndTemps GetInstance()
            {
                var result = new TupleOperatorSideEffectsAndTemps();
                result.leftInit = ArrayBuilder<BoundExpression>.GetInstance();
                result.rightInit = ArrayBuilder<BoundExpression>.GetInstance();
                result.temps = ArrayBuilder<LocalSymbol>.GetInstance();

                return result;
            }

            internal (ImmutableArray<BoundExpression>, ImmutableArray<LocalSymbol>) ToImmutableAndFree()
            {
                leftInit.AddRange(rightInit);
                rightInit.Free();

                return (leftInit.ToImmutableAndFree(), temps.ToImmutableAndFree());
            }
        }
    }
}
