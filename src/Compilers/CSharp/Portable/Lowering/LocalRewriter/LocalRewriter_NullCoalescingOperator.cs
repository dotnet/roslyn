// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitNullCoalescingOperator(BoundNullCoalescingOperator node)
        {
            BoundExpression rewrittenLeft = (BoundExpression)Visit(node.LeftOperand);
            BoundExpression rewrittenRight = (BoundExpression)Visit(node.RightOperand);
            TypeSymbol rewrittenResultType = VisitType(node.Type);

            return MakeNullCoalescingOperator(node.Syntax, rewrittenLeft, rewrittenRight, node.LeftConversion, node.OperatorResultKind, rewrittenResultType);
        }

        private BoundExpression MakeNullCoalescingOperator(
            SyntaxNode syntax,
            BoundExpression rewrittenLeft,
            BoundExpression rewrittenRight,
            Conversion leftConversion,
            BoundNullCoalescingOperatorResultKind resultKind,
            TypeSymbol rewrittenResultType)
        {
            Debug.Assert(rewrittenLeft != null);
            Debug.Assert(rewrittenRight != null);
            Debug.Assert(leftConversion.IsValid);
            Debug.Assert((object)rewrittenResultType != null);
            Debug.Assert(rewrittenRight.Type.Equals(rewrittenResultType, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

            if (_inExpressionLambda)
            {
                TypeSymbol strippedLeftType = rewrittenLeft.Type.StrippedType();
                Conversion rewrittenConversion = TryMakeConversion(syntax, leftConversion, strippedLeftType, rewrittenResultType);
                if (!rewrittenConversion.Exists)
                {
                    return BadExpression(syntax, rewrittenResultType, rewrittenLeft, rewrittenRight);
                }

                return new BoundNullCoalescingOperator(syntax, rewrittenLeft, rewrittenRight, rewrittenConversion, resultKind, rewrittenResultType);
            }

            var isUnconstrainedTypeParameter = rewrittenLeft is
            {
                Type: object { IsReferenceType: false, IsValueType: false }
            };

            // first we can make a small optimization:
            // If left is a constant then we already know whether it is null or not. If it is null then we
            // can simply generate "right". If it is not null then we can simply generate
            // MakeConversion(left). This does not hold when the left is an unconstrained type parameter: at runtime,
            // it can be either left or right depending on the runtime type of T
            if (!isUnconstrainedTypeParameter)
            {
                if (rewrittenLeft.IsDefaultValue())
                {
                    return rewrittenRight;
                }

                if (rewrittenLeft.ConstantValue != null)
                {
                    Debug.Assert(!rewrittenLeft.ConstantValue.IsNull);

                    return GetConvertedLeftForNullCoalescingOperator(rewrittenLeft, leftConversion, rewrittenResultType);
                }
            }

            // string concatenation is never null.
            // interpolated string lowering may introduce redundant null coalescing, which we have to remove.
            if (IsStringConcat(rewrittenLeft))
            {
                return GetConvertedLeftForNullCoalescingOperator(rewrittenLeft, leftConversion, rewrittenResultType);
            }

            // if left conversion is intrinsic implicit (always succeeds) and results in a reference type
            // we can apply conversion before doing the null check that allows for a more efficient IL emit.
            if (rewrittenLeft.Type.IsReferenceType &&
                leftConversion.IsImplicit &&
                !leftConversion.IsUserDefined)
            {
                if (!leftConversion.IsIdentity)
                {
                    rewrittenLeft = MakeConversionNode(rewrittenLeft.Syntax, rewrittenLeft, leftConversion, rewrittenResultType, @checked: false);
                }
                return new BoundNullCoalescingOperator(syntax, rewrittenLeft, rewrittenRight, Conversion.Identity, resultKind, rewrittenResultType);
            }

            if (leftConversion.IsIdentity || leftConversion.Kind == ConversionKind.ExplicitNullable)
            {
                var conditionalAccess = rewrittenLeft as BoundLoweredConditionalAccess;
                if (conditionalAccess != null &&
                    (conditionalAccess.WhenNullOpt == null || NullableNeverHasValue(conditionalAccess.WhenNullOpt)))
                {
                    var notNullAccess = NullableAlwaysHasValue(conditionalAccess.WhenNotNull);
                    if (notNullAccess != null)
                    {
                        var whenNullOpt = rewrittenRight;

                        if (whenNullOpt.Type.IsNullableType())
                        {
                            notNullAccess = conditionalAccess.WhenNotNull;
                        }

                        if (whenNullOpt.IsDefaultValue() && whenNullOpt.Type.SpecialType != SpecialType.System_Decimal)
                        {
                            whenNullOpt = null;
                        }

                        return conditionalAccess.Update(
                            conditionalAccess.Receiver,
                            conditionalAccess.HasValueMethodOpt,
                            whenNotNull: notNullAccess,
                            whenNullOpt: whenNullOpt,
                            id: conditionalAccess.Id,
                            type: rewrittenResultType
                        );
                    }
                }
            }

            // Optimize left ?? right to left.GetValueOrDefault() when left is T? and right is the default value of T
            if (rewrittenLeft.Type.IsNullableType()
                && RemoveIdentityConversions(rewrittenRight).IsDefaultValue()
                && rewrittenRight.Type.Equals(rewrittenLeft.Type.GetNullableUnderlyingType(), TypeCompareKind.AllIgnoreOptions)
                && TryGetNullableMethod(rewrittenLeft.Syntax, rewrittenLeft.Type, SpecialMember.System_Nullable_T_GetValueOrDefault, out MethodSymbol getValueOrDefault))
            {
                return BoundCall.Synthesized(rewrittenLeft.Syntax, rewrittenLeft, getValueOrDefault);
            }

            // We lower left ?? right to
            //
            // var temp = left;
            // (temp != null) ? MakeConversion(temp) : right
            //

            BoundAssignmentOperator tempAssignment;
            BoundLocal boundTemp = _factory.StoreToTemp(rewrittenLeft, out tempAssignment);

            // temp != null
            BoundExpression nullCheck = MakeNullCheck(syntax, boundTemp, BinaryOperatorKind.NotEqual);

            // MakeConversion(temp, rewrittenResultType)
            BoundExpression convertedLeft = GetConvertedLeftForNullCoalescingOperator(boundTemp, leftConversion, rewrittenResultType);
            Debug.Assert(convertedLeft.HasErrors || convertedLeft.Type.Equals(rewrittenResultType, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

            // (temp != null) ? MakeConversion(temp, LeftConversion) : RightOperand
            BoundExpression conditionalExpression = RewriteConditionalOperator(
                syntax: syntax,
                rewrittenCondition: nullCheck,
                rewrittenConsequence: convertedLeft,
                rewrittenAlternative: rewrittenRight,
                constantValueOpt: null,
                rewrittenType: rewrittenResultType,
                isRef: false);

            Debug.Assert(conditionalExpression.ConstantValue == null); // we shouldn't have hit this else case otherwise
            Debug.Assert(conditionalExpression.Type.Equals(rewrittenResultType, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

            return new BoundSequence(
                syntax: syntax,
                locals: ImmutableArray.Create(boundTemp.LocalSymbol),
                sideEffects: ImmutableArray.Create<BoundExpression>(tempAssignment),
                value: conditionalExpression,
                type: rewrittenResultType);
        }

        private bool IsStringConcat(BoundExpression expression)
        {
            if (expression.Kind != BoundKind.Call)
            {
                return false;
            }

            var boundCall = (BoundCall)expression;

            var method = boundCall.Method;

            if (method.IsStatic && method.ContainingType.SpecialType == SpecialType.System_String)
            {
                if ((object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringString) ||
                    (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringStringString) ||
                    (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringStringStringString) ||
                    (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatObject) ||
                    (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatObjectObject) ||
                    (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatObjectObjectObject) ||
                    (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatStringArray) ||
                    (object)method == (object)_compilation.GetSpecialTypeMember(SpecialMember.System_String__ConcatObjectArray))
                {
                    return true;
                }
            }

            return false;
        }

        private static BoundExpression RemoveIdentityConversions(BoundExpression expression)
        {
            while (expression.Kind == BoundKind.Conversion)
            {
                var boundConversion = (BoundConversion)expression;
                if (boundConversion.ConversionKind != ConversionKind.Identity)
                {
                    return expression;
                }

                expression = boundConversion.Operand;
            }

            return expression;
        }

        private BoundExpression GetConvertedLeftForNullCoalescingOperator(BoundExpression rewrittenLeft, Conversion leftConversion, TypeSymbol rewrittenResultType)
        {
            Debug.Assert(rewrittenLeft != null);
            Debug.Assert((object)rewrittenLeft.Type != null);
            Debug.Assert((object)rewrittenResultType != null);
            Debug.Assert(leftConversion.IsValid);

            TypeSymbol rewrittenLeftType = rewrittenLeft.Type;
            Debug.Assert(rewrittenLeftType.IsNullableType() || !rewrittenLeftType.IsValueType);

            // Native compiler violates the specification for the case where result type is right operand type and left operand is nullable.
            // For this case, we need to insert an extra explicit nullable conversion from the left operand to its underlying nullable type
            // before performing the leftConversion.
            // See comments in Binder.BindNullCoalescingOperator referring to GetConvertedLeftForNullCoalescingOperator for more details.

            if (!TypeSymbol.Equals(rewrittenLeftType, rewrittenResultType, TypeCompareKind.ConsiderEverything2) && rewrittenLeftType.IsNullableType())
            {
                TypeSymbol strippedLeftType = rewrittenLeftType.GetNullableUnderlyingType();
                MethodSymbol getValueOrDefault = UnsafeGetNullableMethod(rewrittenLeft.Syntax, rewrittenLeftType, SpecialMember.System_Nullable_T_GetValueOrDefault);
                rewrittenLeft = BoundCall.Synthesized(rewrittenLeft.Syntax, rewrittenLeft, getValueOrDefault);
                if (TypeSymbol.Equals(strippedLeftType, rewrittenResultType, TypeCompareKind.ConsiderEverything2))
                {
                    return rewrittenLeft;
                }
            }

            return MakeConversionNode(rewrittenLeft.Syntax, rewrittenLeft, leftConversion, rewrittenResultType, @checked: false);
        }
    }
}
