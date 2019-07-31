// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            BoundExpression result = _factory.Sequence(temps.ToImmutableAndFree(), initEffects.ToImmutableAndFree(), returnValue);
            return result;
        }

        private bool IsLikeTupleExpression(BoundExpression expr, out BoundTupleExpression tuple)
        {
            switch (expr)
            {
                case BoundTupleExpression t:
                    tuple = t;
                    return true;
                case BoundConversion { Conversion: { Kind: ConversionKind.Identity }, Operand: var o }:
                    return IsLikeTupleExpression(o, out tuple);
                case BoundConversion { Conversion: { Kind: ConversionKind.ImplicitTupleLiteral }, Operand: var o }:
                    // The compiler produces the implicit tuple literal conversion as an identity conversion for
                    // the benefit of the semantic model only.
                    Debug.Assert(expr.Type == (object)o.Type || expr.Type.Equals(o.Type, TypeCompareKind.AllIgnoreOptions));
                    return IsLikeTupleExpression(o, out tuple);
                case BoundConversion { Conversion: { Kind: var kind } c, Operand: var o } conversion when
                        c.IsTupleConversion || c.IsTupleLiteralConversion:
                    {
                        // Push tuple conversions down to the elements.
                        if (!IsLikeTupleExpression(o, out tuple)) return false;
                        var underlyingConversions = c.UnderlyingConversions;
                        var resultTypes = conversion.Type.TupleElementTypesWithAnnotations;
                        var builder = ArrayBuilder<BoundExpression>.GetInstance(tuple.Arguments.Length);
                        for (int i = 0; i < tuple.Arguments.Length; i++)
                        {
                            var element = tuple.Arguments[i];
                            var elementConversion = underlyingConversions[i];
                            var elementType = resultTypes[i].Type;
                            var newArgument = new BoundConversion(
                                syntax: expr.Syntax,
                                operand: element,
                                conversion: elementConversion,
                                @checked: conversion.Checked,
                                explicitCastInCode: conversion.ExplicitCastInCode,
                                conversionGroupOpt: null,
                                constantValueOpt: null,
                                type: elementType,
                                hasErrors: conversion.HasErrors);
                            builder.Add(newArgument);
                        }
                        var newArguments = builder.ToImmutableAndFree();
                        tuple = new BoundConvertedTupleLiteral(
                            tuple.Syntax, sourceTuple: null, newArguments, ImmutableArray<string>.Empty,
                            ImmutableArray<bool>.Empty, conversion.Type, conversion.HasErrors);
                        return true;
                    }
                case BoundConversion { Conversion: { Kind: var kind }, Operand: var o } when
                        (kind == ConversionKind.ImplicitNullable || kind == ConversionKind.ExplicitNullable) &&
                        expr.Type.IsNullableType() && expr.Type.StrippedType().Equals(o.Type, TypeCompareKind.AllIgnoreOptions):
                    return IsLikeTupleExpression(o, out tuple);
                default:
                    tuple = null;
                    return false;
            }
        }

        private BoundExpression PushDownImplicitTupleConversion(
            BoundExpression expr,
            ArrayBuilder<BoundExpression> initEffects,
            ArrayBuilder<LocalSymbol> temps)
        {
            if (expr is BoundConversion { ConversionKind: ConversionKind.ImplicitTuple, Conversion: var conversion } boundConversion)
            {
                // We push an implicit tuple converion down to its elements
                var syntax = boundConversion.Syntax;
                var destElementTypes = expr.Type.GetElementTypesOfTupleOrCompatible();
                var numElements = destElementTypes.Length;
                TypeSymbol srcType = (TupleTypeSymbol)boundConversion.Operand.Type;
                var srcElementFields = srcType.TupleElements;
                var fieldAccessorsBuilder = ArrayBuilder<BoundExpression>.GetInstance(numElements);
                var savedTuple = DeferSideEffectingArgumentToTempForTupleEquality(LowerConversions(boundConversion.Operand), initEffects, temps);
                var elementConversions = conversion.UnderlyingConversions;

                for (int i = 0; i < numElements; i++)
                {
                    var fieldAccess = MakeTupleFieldAccessAndReportUseSiteDiagnostics(savedTuple, syntax, srcElementFields[i]);
                    var convertedFieldAccess = new BoundConversion(
                        syntax, fieldAccess, elementConversions[i], boundConversion.Checked, boundConversion.ExplicitCastInCode, null, null, destElementTypes[i].Type, boundConversion.HasErrors);
                    fieldAccessorsBuilder.Add(convertedFieldAccess);
                }

                return new BoundConvertedTupleLiteral(
                    syntax, sourceTuple: null, fieldAccessorsBuilder.ToImmutableAndFree(), ImmutableArray<string>.Empty,
                    ImmutableArray<bool>.Empty, expr.Type, expr.HasErrors);
            }

            return expr;
        }

        /// <summary>
        /// Walk down tuple literals and replace all the side-effecting elements that need saving with temps.
        /// Expressions that are not tuple literals need saving, as are tuple literals that are involved in
        /// a simple comparison rather than a tuple comparison.
        /// </summary>
        private BoundExpression ReplaceTerminalElementsWithTemps(
            BoundExpression expr,
            TupleBinaryOperatorInfo operators,
            ArrayBuilder<BoundExpression> initEffects,
            ArrayBuilder<LocalSymbol> temps)
        {
            if (operators.InfoKind == TupleBinaryOperatorInfoKind.Multiple)
            {
                expr = PushDownImplicitTupleConversion(expr, initEffects, temps);
                if (IsLikeTupleExpression(expr, out BoundTupleExpression tuple))
                {
                    // Example:
                    // in `(expr1, expr2) == (..., ...)` we need to save `expr1` and `expr2`
                    var multiple = (TupleBinaryOperatorInfo.Multiple)operators;
                    var builder = ArrayBuilder<BoundExpression>.GetInstance(tuple.Arguments.Length);
                    for (int i = 0; i < tuple.Arguments.Length; i++)
                    {
                        var argument = tuple.Arguments[i];
                        var newArgument = ReplaceTerminalElementsWithTemps(argument, multiple.Operators[i], initEffects, temps);
                        builder.Add(newArgument);
                    }

                    var newArguments = builder.ToImmutableAndFree();
                    return new BoundConvertedTupleLiteral(
                        tuple.Syntax, sourceTuple: null, newArguments, ImmutableArray<string>.Empty,
                        ImmutableArray<bool>.Empty, tuple.Type, tuple.HasErrors);
                }
            }

            // Examples:
            // in `expr == (..., ...)` we need to save `expr` because it's not a tuple literal
            // in `(..., expr) == (..., (..., ...))` we need to save `expr` because it is used in a simple comparison
            return DeferSideEffectingArgumentToTempForTupleEquality(expr, initEffects, temps);
        }

        /// <summary>
        /// Evaluate side effects into a temp, if necessary.  If there is an implicit user-defined
        /// conversion operation near the top of the arg, preserve that in the returned expression to be evaluated later.
        /// Conversions at the head of the result are unlowered, though the nested arguments within it are lowered.
        /// That resulting expression must be passed through <see cref="LowerConversions(BoundExpression)"/> to
        /// complete the lowering.
        /// </summary>
        private BoundExpression DeferSideEffectingArgumentToTempForTupleEquality(
            BoundExpression expr,
            ArrayBuilder<BoundExpression> effects,
            ArrayBuilder<LocalSymbol> temps,
            bool enclosingConversionWasExplicit = false)
        {
            switch (expr)
            {
                case { ConstantValue: { } }:
                    return VisitExpression(expr);
                case BoundConversion { Conversion: { Kind: ConversionKind.DefaultOrNullLiteral } conversion } bc:
                    // This conversion can be performed lazily, but need not be saved.  It is treated as non-side-effecting.
                    return EvaluateSideEffectingArgumentToTemp(expr, effects, temps);
                case BoundConversion { Conversion: { Kind: var conversionKind } conversion } bc when conversionMustBePerformedOnOriginalExpression(bc, conversionKind):
                    // Some conversions cannot be performed on a copy of the argument and must be done early.
                    return EvaluateSideEffectingArgumentToTemp(expr, effects, temps);
                case BoundConversion { Conversion: { IsUserDefined: true } } conv when conv.ExplicitCastInCode || enclosingConversionWasExplicit:
                    // A user-defined conversion triggered by a cast must be performed early.
                    return EvaluateSideEffectingArgumentToTemp(expr, effects, temps);
                case BoundConversion conv:
                    {
                        // other conversions are deferred
                        var deferredOperand = DeferSideEffectingArgumentToTempForTupleEquality(conv.Operand, effects, temps, conv.ExplicitCastInCode || enclosingConversionWasExplicit);
                        return conv.UpdateOperand(deferredOperand);
                    }
                case BoundObjectCreationExpression { Arguments: { Length: 0 } } _ when expr.Type.IsNullableType():
                    return new BoundLiteral(expr.Syntax, ConstantValue.Null, expr.Type);
                case BoundObjectCreationExpression { Arguments: { Length: 1 } } creation when expr.Type.IsNullableType():
                    {
                        var deferredOperand = DeferSideEffectingArgumentToTempForTupleEquality(
                            creation.Arguments[0], effects, temps, enclosingConversionWasExplicit: true);
                        return new BoundConversion(
                            syntax: expr.Syntax, operand: deferredOperand,
                            conversion: Conversion.MakeNullableConversion(ConversionKind.ImplicitNullable, Conversion.Identity),
                            @checked: false, explicitCastInCode: true, conversionGroupOpt: null, constantValueOpt: null,
                            type: expr.Type, hasErrors: expr.HasErrors);
                    }
                default:
                    // When in doubt, evaluate early to a temp.
                    return EvaluateSideEffectingArgumentToTemp(expr, effects, temps);
            }

            bool conversionMustBePerformedOnOriginalExpression(BoundConversion expr, ConversionKind kind)
            {
                // These are conversions from-expression that do not produce a constant,
                // and which must be performed on the original expression, not on a copy of it.
                switch (kind)
                {
                    case ConversionKind.AnonymousFunction:       // a lambda cannot be saved without a target type
                    case ConversionKind.MethodGroup:             // similarly for a method group
                    case ConversionKind.InterpolatedString:      // an interpolated string must be saved in interpolated form
                    case ConversionKind.SwitchExpression:        // a switch expression must have its arms converted
                    case ConversionKind.StackAllocToPointerType: // a stack alloc is not well-defined without an enclosing conversion
                    case ConversionKind.StackAllocToSpanType:
                        return true;
                    default:
                        return false;
                }
            }
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
            BoundExpression innerSequence = _factory.Sequence(locals: ImmutableArray<LocalSymbol>.Empty, innerEffects.ToImmutableAndFree(), logicalExpression);

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
                return _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, outerEffects.ToImmutableAndFree(),
                    result: boolValue ? _factory.Not(leftHasValue) : leftHasValue);
            }

            if (leftHasValue.ConstantValue == ConstantValue.False)
            {
                // The outer sequence degenerates when we known that `leftHasValue` is false
                // Produce: !rightHasValue (or rightHasValue for inequality comparison)
                return _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, outerEffects.ToImmutableAndFree(),
                    result: boolValue ? _factory.Not(rightHasValue) : rightHasValue);
            }

            // outer sequence:
            //      leftHasValue == rightHasValue
            //          ? leftHasValue ? ... inner sequence ... : true/false
            //          : false/true
            BoundExpression outerSequence =
                _factory.Sequence(ImmutableArray<LocalSymbol>.Empty, outerEffects.ToImmutableAndFree(),
                    _factory.Conditional(
                        _factory.Binary(BinaryOperatorKind.Equal, boolType, leftHasValue, rightHasValue),
                        _factory.Conditional(leftHasValue, innerSequence, MakeBooleanConstant(right.Syntax, boolValue), boolType),
                        MakeBooleanConstant(right.Syntax, !boolValue),
                        boolType));

            return outerSequence;
        }

        /// <summary>
        /// Produce a <c>.HasValue</c> and a <c>.GetValueOrDefault()</c> for nullable expressions that are neither always null or
        /// never null, and functionally equivalent parts for other cases.
        /// </summary>
        private void MakeNullableParts(BoundExpression expr, ArrayBuilder<LocalSymbol> temps, ArrayBuilder<BoundExpression> innerEffects,
            ArrayBuilder<BoundExpression> outerEffects, bool saveHasValue, out BoundExpression hasValue, out BoundExpression value, out bool isNullable)
        {
            isNullable = !(expr is BoundTupleExpression) && expr.Type.IsNullableType();
            if (!isNullable)
            {
                hasValue = MakeBooleanConstant(expr.Syntax, true);
                expr = PushDownImplicitTupleConversion(expr, innerEffects, temps);
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
            if (NullableAlwaysHasValue(expr) is BoundExpression knownValue)
            {
                hasValue = MakeBooleanConstant(expr.Syntax, true);
                // If a tuple conversion, keep its parts around with deferred conversions.
                value = PushDownImplicitTupleConversion(knownValue, innerEffects, temps);
                value = LowerConversions(value);
                isNullable = false;
                return;
            }

            // Regular nullable expressions
            hasValue = makeNullableHasValue(expr);
            if (saveHasValue)
            {
                hasValue = MakeTemp(hasValue, temps, outerEffects);
            }

            value = MakeValueOrDefaultTemp(expr, temps, innerEffects);

            BoundExpression makeNullableHasValue(BoundExpression expr)
            {
                // Optimize conversions where we can use the HasValue of the underlying
                switch (expr)
                {
                    case BoundConversion { Conversion: { IsIdentity: true }, Operand: var o }:
                        return makeNullableHasValue(o);
                    case BoundConversion { Conversion: { IsNullable: true, UnderlyingConversions: var underlying }, Operand: var o }
                            when expr.Type.IsNullableType() && o.Type.IsNullableType() && !underlying[0].IsUserDefined:
                        // Note that a user-defined conversion from K to Nullable<R> which may translate
                        // a non-null K to a null value gives rise to a lifted conversion from Nullable<K> to Nullable<R> with the same property.
                        // We therefore do not attempt to optimize nullable conversions with an underlying user-defined conversion.
                        return makeNullableHasValue(o);
                    default:
                        return _factory.MakeNullableHasValue(expr.Syntax, expr);
                }
            }
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
        private BoundExpression MakeValueOrDefaultTemp(
            BoundExpression expr,
            ArrayBuilder<LocalSymbol> temps,
            ArrayBuilder<BoundExpression> effects)
        {
            // Optimize conversions where we can use the underlying
            switch (expr)
            {
                case BoundConversion { Conversion: { IsIdentity: true }, Operand: var o }:
                    return MakeValueOrDefaultTemp(o, temps, effects);
                case BoundConversion { Conversion: { IsNullable: true, UnderlyingConversions: var nested }, Operand: var o } conv when
                        expr.Type.IsNullableType() && o.Type.IsNullableType() && nested[0] is { IsTupleConversion: true } tupleConversion:
                    {
                        var operand = MakeValueOrDefaultTemp(o, temps, effects);
                        var types = expr.Type.GetNullableUnderlyingType().GetElementTypesOfTupleOrCompatible();
                        int tupleCardinality = operand.Type.TupleElementTypesWithAnnotations.Length;
                        var underlyingConversions = tupleConversion.UnderlyingConversions;
                        Debug.Assert(underlyingConversions.Length == tupleCardinality);
                        var argumentBuilder = ArrayBuilder<BoundExpression>.GetInstance(tupleCardinality);
                        for (int i = 0; i < tupleCardinality; i++)
                        {
                            argumentBuilder.Add(MakeBoundConversion(GetTuplePart(operand, i), underlyingConversions[i], types[i], conv));
                        }
                        return new BoundConvertedTupleLiteral(
                            syntax: operand.Syntax,
                            sourceTuple: null,
                            arguments: argumentBuilder.ToImmutableAndFree(),
                            argumentNamesOpt: ImmutableArray<string>.Empty,
                            inferredNamesOpt: ImmutableArray<bool>.Empty,
                            type: expr.Type,
                            hasErrors: expr.HasErrors).WithSuppression(expr.IsSuppressed);
                        throw null;
                    }
                default:
                    {
                        BoundExpression valueOrDefaultCall = MakeOptimizedGetValueOrDefault(expr.Syntax, expr);
                        return MakeTemp(valueOrDefaultCall, temps, effects);
                    }
            }


            BoundExpression MakeBoundConversion(BoundExpression expr, Conversion conversion, TypeWithAnnotations type, BoundConversion enclosing)
            {
                return new BoundConversion(
                    expr.Syntax, expr, conversion, enclosing.Checked, enclosing.ExplicitCastInCode,
                    conversionGroupOpt: null, constantValueOpt: null, type: type.Type);
            }

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
        /// For expressions with tuple type, we access <c>Item{i+1}</c>.
        /// </summary>
        private BoundExpression GetTuplePart(BoundExpression tuple, int i)
        {
            // Example:
            // (1, 2) == (1, 2);
            if (tuple is BoundTupleExpression tupleExpression)
            {
                return tupleExpression.Arguments[i];
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
            // We deferred lowering some of the conversions on the operand, even though the
            // code below the conversions were lowered.  We lower the conversion part now.
            left = LowerConversions(left);
            right = LowerConversions(right);

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

            BoundExpression binary = MakeBinaryOperator(_factory.Syntax, single.Kind, left, right, single.MethodSymbolOpt?.ReturnType ?? boolType, single.MethodSymbolOpt);
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

        /// <summary>
        /// Lower any conversions appearing near the top of the bound expression, assuming non-conversions
        /// appearing below them have already been lowered.
        /// </summary>
        private BoundExpression LowerConversions(BoundExpression expr)
        {
            return (expr is BoundConversion conv)
                ? MakeConversionNode(
                    oldNodeOpt: conv, syntax: conv.Syntax, rewrittenOperand: LowerConversions(conv.Operand),
                    conversion: conv.Conversion, @checked: conv.Checked, explicitCastInCode: conv.ExplicitCastInCode,
                    constantValueOpt: conv.ConstantValue, rewrittenType: conv.Type)
                : expr;
        }
    }
}
