﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

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

            BoundExpression newLeft = ReplaceTerminalElementsWithTemps(node.Left, node.Operators, initialEffectsAndTemps.leftInit, initialEffectsAndTemps.temps);
            BoundExpression newRight = ReplaceTerminalElementsWithTemps(node.Right, node.Operators, initialEffectsAndTemps.rightInit, initialEffectsAndTemps.temps);

            var returnValue = RewriteTupleOperator(node.Operators, newLeft, newRight, boolType, initialEffectsAndTemps, node.OperatorKind);

            var (effects, temps) = initialEffectsAndTemps.ToImmutableAndFree();
            var result = _factory.Sequence(temps, effects, returnValue);
            return result;
        }

        /// <summary>
        /// Walk down tuple literals and replace all the elements that need saving with temps.
        /// Expressions that are not tuple literals need saving, and tuple literals that are involved in a simple comparison rather than a tuple comparison.
        /// </summary>
        private BoundExpression ReplaceTerminalElementsWithTemps(BoundExpression expr, TupleBinaryOperatorInfo operators, ArrayBuilder<BoundExpression> initEffects, ArrayBuilder<LocalSymbol> temps)
        {
            if (!operators.IsSingle())
            {
                // Example:
                // in `(expr1, expr2) == (..., ...)` we need to save `expr1` and `expr2`
                if (expr.Kind == BoundKind.TupleLiteral)
                {
                    var tuple = (BoundTupleLiteral)expr;
                    var multiple = (TupleBinaryOperatorInfo.Multiple)operators;
                    var builder = ArrayBuilder<BoundExpression>.GetInstance(tuple.Arguments.Length);
                    for (int i = 0; i < tuple.Arguments.Length; i++)
                    {
                        var argument = tuple.Arguments[i];
                        var newArgument = ReplaceTerminalElementsWithTemps(argument, multiple.Operators[i], initEffects, temps);
                        builder.Add(newArgument);
                    }
                    return new BoundTupleLiteral(tuple.Syntax, tuple.ArgumentNamesOpt, tuple.InferredNamesOpt, builder.ToImmutableAndFree(), tuple.Type, tuple.HasErrors);
                }
            }

            // Examples:
            // in `expr == (..., ...)` we need to save `expr` because it's not a tuple literal
            // in `(..., expr) == (..., (..., ...))` we need to save `expr` because it is used in a simple comparison
            return MakeTemp(VisitExpression(expr), temps, initEffects);
        }

        private BoundExpression RewriteTupleOperator(TupleBinaryOperatorInfo @operator,
            BoundExpression left, BoundExpression right, TypeSymbol boolType,
            TupleOperatorSideEffectsAndTemps initialEffectsAndTemps, BinaryOperatorKind operatorKind)
        {
            if (@operator.IsSingle())
            {
                return RewriteTupleSingleOperator((TupleBinaryOperatorInfo.Single)@operator, left, right, boolType, operatorKind);
            }
            else
            {
                return RewriteTupleNestedOperators((TupleBinaryOperatorInfo.Multiple)@operator, left, right, boolType, initialEffectsAndTemps, operatorKind);
            }
        }

        private BoundExpression RewriteTupleNestedOperators(TupleBinaryOperatorInfo.Multiple operators, BoundExpression left, BoundExpression right,
            TypeSymbol boolType, TupleOperatorSideEffectsAndTemps initialEffectsAndTemps, BinaryOperatorKind operatorKind)
        {
            // If either left or right is nullable, produce:
            //
            //      // outer sequence
            //      leftHasValue = left.HasValue; (or true if !leftNullable)
            //      leftHasValue == right.HasValue (or true if !rightNullable)
            //          ? leftHasValue ? ... inner sequence ... : true/false
            //          : false/true
            //
            // where inner sequence is:
            //      leftValue = left.GetValueOrDefault(); (or left if !leftNullable)
            //      rightValue = right.GetValueOrDefault(); (or right if !rightNullable)
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

            // PROTOTYPE(tuple-equality) Consider if optimizations from TrivialLiftedComparisonOperatorOptimizations can be applied

            var outerEffects = ArrayBuilder<BoundExpression>.GetInstance();
            var innerEffects = ArrayBuilder<BoundExpression>.GetInstance();

            BoundExpression leftHasValue;
            BoundExpression leftValue;

            // PROTOTYPE(tuple-equality) Re-use NullableNeverHasValue/NullableAlwaysHasValue

            var isLeftNullable = left.Type.IsNullableType();
            if (isLeftNullable)
            {
                leftHasValue = MakeHasValueTemp(left, initialEffectsAndTemps.temps, outerEffects);
                leftValue = MakeValueOrDefaultTemp(left, initialEffectsAndTemps.temps, innerEffects);
            }
            else
            {
                leftHasValue = MakeBooleanConstant(left.Syntax, true);
                leftValue = left;
            }

            BoundExpression rightHasValue;
            BoundExpression rightValue;

            var isRightNullable = right.Type.IsNullableType();
            if (isRightNullable)
            {
                rightHasValue = MakeNullableHasValue(right.Syntax, right); // no need for local for right.HasValue since used once
                rightValue = MakeValueOrDefaultTemp(right, initialEffectsAndTemps.temps, innerEffects);
            }
            else
            {
                rightHasValue = MakeBooleanConstant(right.Syntax, true);
                rightValue = right;
            }

            // Produces:
            //     ... logical expression using leftValue and rightValue ...
            BoundExpression logicalExpression = RewriteNonNullableNestedTupleOperators(operators, leftValue, rightValue, boolType, initialEffectsAndTemps.temps, innerEffects, initialEffectsAndTemps, operatorKind);

            // Produces:
            //     leftValue = left.GetValueOrDefault(); (or left if !leftNullable)
            //     rightValue = right.GetValueOrDefault(); (or right if !rightNullable)
            //     ... logical expression using leftValue and rightValue ...
            BoundExpression innerSequence = _factory.Sequence(locals: ImmutableArray<LocalSymbol>.Empty, innerEffects.ToImmutableAndFree(), logicalExpression);

            if (!isLeftNullable && !isRightNullable)
            {
                // The outer sequence degenerates when we know that both `leftHasValue` and `rightHasValue` are true
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

        private BoundLocal MakeTemp(BoundExpression loweredExpression, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> effects)
        {
            BoundLocal temp = _factory.StoreToTemp(loweredExpression, out BoundAssignmentOperator assignmentToTemp);
            effects.Add(assignmentToTemp);
            temps.Add(temp.LocalSymbol);
            return temp;
        }

        /// <summary>
        /// Returns a temp which is initialized with lowered-expression.HasValue
        /// </summary>
        private BoundLocal MakeHasValueTemp(BoundExpression expression, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> effects)
        {
            BoundExpression hasValueCall = MakeNullableHasValue(expression.Syntax, expression);
            return MakeTemp(hasValueCall, temps, effects);
        }

        /// <summary>
        /// Returns a temp which is initialized with lowered-expression.GetValueOrDefault()
        /// </summary>
        private BoundLocal MakeValueOrDefaultTemp(BoundExpression expression,
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
            ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> effects, TupleOperatorSideEffectsAndTemps effectAndTemps, BinaryOperatorKind operatorKind)
        {
            Debug.Assert(left.Type?.IsNullableType() == false);
            Debug.Assert(right.Type?.IsNullableType() == false);

            ImmutableArray<TupleBinaryOperatorInfo> nestedOperators = operators.Operators;

            BoundExpression currentResult = null;
            for (int i = 0; i < nestedOperators.Length; i++)
            {
                BoundExpression leftElement = GetTuplePart(left, i, effectAndTemps.temps, effectAndTemps.leftInit);
                BoundExpression rightElement = GetTuplePart(right, i, effectAndTemps.temps, effectAndTemps.rightInit);
                BoundExpression nextLogicalOperand = RewriteTupleOperator(nestedOperators[i], leftElement, rightElement, type, effectAndTemps, operatorKind);
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

        /// <summary>
        /// For unconverted tuple literals, we just return the element (which is a temp that we prepared).
        /// For expressions with tuple type, we access `tuple.Item{i}`.
        /// </summary>
        private BoundExpression GetTuplePart(BoundExpression tuple, int i, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> initEffects)
        {
            // Example:
            // (1, 2) == (1, 2);
            if (IsTupleExpression(tuple.Kind))
            {
                return ((BoundTupleExpression)tuple).Arguments[i];
            }

            Debug.Assert(tuple.Type.IsTupleType);

            // Example:
            // t == GetTuple();
            // t == ((byte, byte)) (1, 2);
            // t == ((short, short))((int, int))(1L, 2L);
            return MakeTupleFieldAccessAndReportUseSiteDiagnostics(tuple, tuple.Syntax, tuple.Type.TupleElements[i]);
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

                BoundExpression dynamicResult = _dynamicFactory.MakeDynamicBinaryOperator(single.Kind, left, right, isCompoundAssignment: false, _compilation.DynamicType).ToExpression();
                if (operatorKind == BinaryOperatorKind.Equal)
                {
                    return _factory.Not(MakeUnaryOperator(UnaryOperatorKind.DynamicFalse, left.Syntax, method: null, dynamicResult, boolType));
                }
                else
                {
                    return MakeUnaryOperator(UnaryOperatorKind.DynamicTrue, left.Syntax, method: null, dynamicResult, boolType);
                }
            }

            // PROTOTYPE(tuple-equality) checked
            BoundExpression convertedLeft = MakeConversionNode(left.Syntax, left, single.LeftConversion, single.LeftConvertedType, @checked: false);
            BoundExpression convertedRight = MakeConversionNode(right.Syntax, right, single.RightConversion, single.RightConvertedType, @checked: false);

            BoundExpression binary = MakeBinaryOperator(_factory.Syntax, single.Kind, convertedLeft, convertedRight, single.MethodSymbolOpt?.ReturnType ?? boolType, single.MethodSymbolOpt);
            UnaryOperatorSignature boolOperator = single.BoolOperator;
            Conversion boolConversion = single.ConversionForBoolOperator;

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
