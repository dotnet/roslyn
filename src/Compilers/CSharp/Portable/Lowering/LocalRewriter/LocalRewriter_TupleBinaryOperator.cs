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
        /// Rewrite <c>GetTuple() == (1, 2)</c> to <c>tuple.Item1 == 1 &amp;&amp; tuple.Item2 == 2</c>.
        /// Also supports the != operator, nullable and nested tuples.
        ///
        /// Note that all the side-effects for visible expressions are evaluated first and from left to right. The initialization phase
        /// contains side-effects for:
        /// - single elements in tuple literals, like <c>a</c> in <c>(a, ...) == (...)</c> for example
        /// - nested expressions that aren't tuple literals, like <c>GetTuple()</c> in <c>(..., GetTuple()) == (..., (..., ...))</c>
        /// On the other hand, <c>Item1</c> and <c>Item2</c> of <c>GetTuple()</c> are not saved as part of the initialization phase of <c>GetTuple() == (..., ...)</c>
        ///
        /// Element-wise conversions occur late, together with the element-wise comparisons. They might not be evaluated.
        /// </summary>
        public override BoundNode VisitTupleBinaryOperator(BoundTupleBinaryOperator node)
        {
            var boolType = node.Type; // we can re-use the bool type
            var initEffects = ArrayBuilder<BoundExpression>.GetInstance();
            var temps = ArrayBuilder<LocalSymbol>.GetInstance();

            BoundExpression newLeft = ReplaceTerminalElementsWithTemps(node.Left, node.Operators, initEffects, temps);
            BoundExpression newRight = ReplaceTerminalElementsWithTemps(node.Right, node.Operators, initEffects, temps);

            var returnValue = RewriteTupleNestedOperators(node.Operators, newLeft, newRight, boolType, temps, node.OperatorKind);
            BoundExpression result = MakeSequenceOrResultValue(temps.ToImmutableAndFree(), initEffects.ToImmutableAndFree(), returnValue);
            return result;
        }

        private BoundExpression MakeSequenceOrResultValue(ImmutableArray<LocalSymbol> locals, ImmutableArray<BoundExpression> effects, BoundExpression returnValue)
        {
            if (locals.IsEmpty && effects.IsEmpty)
            {
                return returnValue;
            }

            return _factory.Sequence(locals, effects, returnValue);
        }

        /// <summary>
        /// Walk down tuple literals and replace all the side-effecting elements that need saving with temps.
        /// Expressions that are not tuple literals need saving, and tuple literals that are involved in a simple comparison rather than a tuple comparison.
        /// </summary>
        private BoundExpression ReplaceTerminalElementsWithTemps(BoundExpression expr, TupleBinaryOperatorInfo operators, ArrayBuilder<BoundExpression> initEffects, ArrayBuilder<LocalSymbol> temps)
        {
            if (operators.InfoKind == TupleBinaryOperatorInfoKind.Multiple)
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
            BoundExpression loweredExpr = VisitExpression(expr);
            if ((object)loweredExpr.Type != null)
            {
                BoundExpression value = NullableAlwaysHasValue(loweredExpr);
                if (value != null)
                {
                    // Optimization: if the nullable expression always has a value, we'll replace that value
                    // with a temp saving that value
                    BoundExpression savedValue = EvaluateSideEffectingArgumentToTemp(value, initEffects, temps);
                    var objectCreation = (BoundObjectCreationExpression)loweredExpr;
                    return objectCreation.UpdateArgumentsAndInitializer(ImmutableArray.Create(savedValue), objectCreation.ArgumentRefKindsOpt, objectCreation.InitializerExpressionOpt);
                }
            }

            // Note: lowered nullable expressions that never have a value also don't have side-effects
            return EvaluateSideEffectingArgumentToTemp(loweredExpr, initEffects, temps);
        }

        private BoundExpression RewriteTupleOperator(TupleBinaryOperatorInfo @operator,
            BoundExpression left, BoundExpression right, TypeSymbol boolType,
            ArrayBuilder<LocalSymbol> temps, BinaryOperatorKind operatorKind)
        {
            switch (@operator.InfoKind)
            {
                case TupleBinaryOperatorInfoKind.Multiple:
                    return RewriteTupleNestedOperators((TupleBinaryOperatorInfo.Multiple)@operator, left, right, boolType, temps, operatorKind);

                case TupleBinaryOperatorInfoKind.Single:
                    return RewriteTupleSingleOperator((TupleBinaryOperatorInfo.Single)@operator, left, right, boolType, operatorKind);

                case TupleBinaryOperatorInfoKind.NullNull:
                    var nullnull = (TupleBinaryOperatorInfo.NullNull)@operator;
                    return new BoundLiteral(left.Syntax, ConstantValue.Create(nullnull.Kind == BinaryOperatorKind.Equal), boolType);

                default:
                    throw ExceptionUtilities.UnexpectedValue(@operator.InfoKind);
            }
        }

        private BoundExpression RewriteTupleNestedOperators(TupleBinaryOperatorInfo.Multiple operators, BoundExpression left, BoundExpression right,
            TypeSymbol boolType, ArrayBuilder<LocalSymbol> temps, BinaryOperatorKind operatorKind)
        {
            left = Binder.GiveTupleTypeToDefaultLiteralIfNeeded(left, right.Type);
            right = Binder.GiveTupleTypeToDefaultLiteralIfNeeded(right, left.Type);

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
            // But if neither is nullable, then just produce the inner sequence.
            //
            // Note: all the temps are created in a single bucket (rather than different scopes of applicability) for simplicity

            var outerEffects = ArrayBuilder<BoundExpression>.GetInstance();
            var innerEffects = ArrayBuilder<BoundExpression>.GetInstance();

            BoundExpression leftHasValue, leftValue;
            bool isLeftNullable;
            MakeNullableParts(left, temps, innerEffects, outerEffects, saveHasValue: true, out leftHasValue, out leftValue, out isLeftNullable);

            BoundExpression rightHasValue, rightValue;
            bool isRightNullable;
            // no need for local for right.HasValue since used once
            MakeNullableParts(right, temps, innerEffects, outerEffects, saveHasValue: false, out rightHasValue, out rightValue, out isRightNullable);

            // Produces:
            //     ... logical expression using leftValue and rightValue ...
            BoundExpression logicalExpression = RewriteNonNullableNestedTupleOperators(operators, leftValue, rightValue, boolType, temps, innerEffects, operatorKind);

            // Produces:
            //     leftValue = left.GetValueOrDefault(); (or left if !leftNullable)
            //     rightValue = right.GetValueOrDefault(); (or right if !rightNullable)
            //     ... logical expression using leftValue and rightValue ...
            BoundExpression innerSequence = MakeSequenceOrResultValue(locals: ImmutableArray<LocalSymbol>.Empty, innerEffects.ToImmutableAndFree(), logicalExpression);

            if (!isLeftNullable && !isRightNullable)
            {
                // The outer sequence degenerates when we know that both `leftHasValue` and `rightHasValue` are true
                return innerSequence;
            }

            bool boolValue = operatorKind == BinaryOperatorKind.Equal; // true/false

            if (rightHasValue.ConstantValue == ConstantValue.False)
            {
                // The outer sequence degenerates when we known that `rightHasValue` is false
                // Produce: !leftHasValue (or leftHasValue for inequality comparison)
                return MakeSequenceOrResultValue(ImmutableArray<LocalSymbol>.Empty, outerEffects.ToImmutableAndFree(),
                    returnValue: boolValue ? _factory.Not(leftHasValue) : leftHasValue);
            }

            if (leftHasValue.ConstantValue == ConstantValue.False)
            {
                // The outer sequence degenerates when we known that `leftHasValue` is false
                // Produce: !rightHasValue (or rightHasValue for inequality comparison)
                return MakeSequenceOrResultValue(ImmutableArray<LocalSymbol>.Empty, outerEffects.ToImmutableAndFree(),
                    returnValue: boolValue ? _factory.Not(rightHasValue) : rightHasValue);
            }

            // outer sequence:
            //      leftHasValue == rightHasValue
            //          ? leftHasValue ? ... inner sequence ... : true/false
            //          : false/true
            BoundExpression outerSequence =
                MakeSequenceOrResultValue(ImmutableArray<LocalSymbol>.Empty, outerEffects.ToImmutableAndFree(),
                    _factory.Conditional(
                        _factory.Binary(BinaryOperatorKind.Equal, boolType, leftHasValue, rightHasValue),
                        _factory.Conditional(leftHasValue, innerSequence, MakeBooleanConstant(right.Syntax, boolValue), boolType),
                        MakeBooleanConstant(right.Syntax, !boolValue),
                        boolType));

            return outerSequence;
        }

        /// <summary>
        /// Produce a <c>.HasValue</c> and a <c>.GetValueOrDefault()</c> for nullable expressions that are neither always null or never null, and functionally equivalent parts for other cases.
        /// </summary>
        private void MakeNullableParts(BoundExpression expr, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> innerEffects,
            ArrayBuilder<BoundExpression> outerEffects, bool saveHasValue, out BoundExpression hasValue, out BoundExpression value, out bool isNullable)
        {
            isNullable = expr.Kind != BoundKind.TupleLiteral && expr.Type.IsNullableType();
            if (!isNullable)
            {
                hasValue = MakeBooleanConstant(expr.Syntax, true);
                value = expr;
                return;
            }

            // Optimization for nullable expressions that are always null
            if (NullableNeverHasValue(expr))
            {
                hasValue = MakeBooleanConstant(expr.Syntax, false);
                // Since there is no value in this nullable expression, we don't need to construct a `.GetValueOrDefault()`, `default(T)` will suffice
                value = new BoundDefaultExpression(expr.Syntax, expr.Type.StrippedType());
                return;
            }

            // Optimization for nullable expressions that are never null
            BoundExpression knownValue = NullableAlwaysHasValue(expr);
            if (knownValue != null)
            {
                hasValue = MakeBooleanConstant(expr.Syntax, true);
                value = knownValue;
                isNullable = false;
                return;
            }

            // Regular nullable expressions
            if (saveHasValue)
            {
                hasValue = MakeHasValueTemp(expr, temps, outerEffects);
            }
            else
            {
                hasValue = MakeNullableHasValue(expr.Syntax, expr);
            }

            value = MakeValueOrDefaultTemp(expr, temps, innerEffects);
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
            ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> effects, BinaryOperatorKind operatorKind)
        {
            ImmutableArray<TupleBinaryOperatorInfo> nestedOperators = operators.Operators;

            BoundExpression currentResult = null;
            for (int i = 0; i < nestedOperators.Length; i++)
            {
                BoundExpression leftElement = GetTuplePart(left, i);
                BoundExpression rightElement = GetTuplePart(right, i);
                BoundExpression nextLogicalOperand = RewriteTupleOperator(nestedOperators[i], leftElement, rightElement, type, temps, operatorKind);
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
        /// For tuple literals, we just return the element.
        /// For expressions with tuple type, we access <c>Item{i}</c>.
        /// </summary>
        private BoundExpression GetTuplePart(BoundExpression tuple, int i)
        {
            // Example:
            // (1, 2) == (1, 2);
            if (tuple.Kind == BoundKind.TupleLiteral)
            {
                return ((BoundTupleLiteral)tuple).Arguments[i];
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
        /// - if it is dynamic, we'll do <c>!(comparisonResult.false)</c> or <c>comparisonResult.true</c>
        /// - if it implicitly converts to bool, we'll just do the conversion
        /// - otherwise, we'll do <c>!(comparisonResult.false)</c> or <c>comparisonResult.true</c> (as we'd do for <c>if</c> or <c>while</c>)
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

            if (left.IsLiteralNull() && right.IsLiteralNull())
            {
                // For `null == null` this is special-cased during initial binding
                return new BoundLiteral(left.Syntax, ConstantValue.Create(operatorKind == BinaryOperatorKind.Equal), boolType);
            }

            // We leave both operands in nullable-null conversions unconverted, MakeBinaryOperator has special for null-literal
            bool isNullableNullConversion = single.Kind.OperandTypes() == BinaryOperatorKind.NullableNull;
            BoundExpression convertedLeft = isNullableNullConversion
                ? left
                : MakeConversionNode(left.Syntax, left, single.LeftConversion, single.LeftConvertedTypeOpt, @checked: false);

            BoundExpression convertedRight = isNullableNullConversion
                ? right
                : MakeConversionNode(right.Syntax, right, single.RightConversion, single.RightConvertedTypeOpt, @checked: false);

            BoundExpression binary = MakeBinaryOperator(_factory.Syntax, single.Kind, convertedLeft, convertedRight, single.MethodSymbolOpt?.ReturnType.TypeSymbol ?? boolType, single.MethodSymbolOpt);
            UnaryOperatorSignature boolOperator = single.BoolOperator;
            Conversion boolConversion = single.ConversionForBool;

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
            else if (!boolConversion.IsIdentity)
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
}
