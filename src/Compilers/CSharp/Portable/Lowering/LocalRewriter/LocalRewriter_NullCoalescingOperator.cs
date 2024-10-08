// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitNullCoalescingOperator(BoundNullCoalescingOperator node)
        {
            BoundExpression rewrittenLeft = VisitExpression(node.LeftOperand);
            BoundExpression rewrittenRight = VisitExpression(node.RightOperand);
            TypeSymbol? rewrittenResultType = VisitType(node.Type);

            return MakeNullCoalescingOperator(node.Syntax, rewrittenLeft, rewrittenRight, node.LeftPlaceholder, node.LeftConversion, node.OperatorResultKind, rewrittenResultType);
        }

        private BoundExpression MakeNullCoalescingOperator(
            SyntaxNode syntax,
            BoundExpression rewrittenLeft,
            BoundExpression rewrittenRight,
            BoundValuePlaceholder? leftPlaceholder,
            BoundExpression? leftConversion,
            BoundNullCoalescingOperatorResultKind resultKind,
            TypeSymbol? rewrittenResultType)
        {
            Debug.Assert(rewrittenLeft != null);
            Debug.Assert(rewrittenRight != null);
            Debug.Assert(BoundNode.GetConversion(leftConversion, leftPlaceholder).IsValid);
            Debug.Assert(rewrittenResultType is { });
            Debug.Assert(rewrittenRight.Type is { });
            Debug.Assert(rewrittenRight.Type.Equals(rewrittenResultType, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

            if (_inExpressionLambda)
            {
                // Because of Error CS0845 (An expression tree lambda may not contain a coalescing operator with a null or default literal left-hand side)
                // we know that the left-hand-side has a type.
                Debug.Assert(rewrittenLeft.Type is { });

                if (leftConversion is BoundConversion { Conversion: { IsIdentity: false } })
                {
                    Debug.Assert(leftPlaceholder is not null);

                    leftConversion = ApplyConversion(leftConversion, leftPlaceholder, leftPlaceholder);

                    if (leftConversion is not BoundConversion { Conversion: { Exists: true } })
                    {
                        return BadExpression(syntax, rewrittenResultType, rewrittenLeft, rewrittenRight);
                    }
                }

                return new BoundNullCoalescingOperator(syntax, rewrittenLeft, rewrittenRight, leftPlaceholder, leftConversion, resultKind, @checked: false, rewrittenResultType);
            }

            var isUnconstrainedTypeParameter = rewrittenLeft.Type is { IsReferenceType: false, IsValueType: false };

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

                if (rewrittenLeft.ConstantValueOpt != null)
                {
                    Debug.Assert(!rewrittenLeft.ConstantValueOpt.IsNull);

                    return GetConvertedLeftForNullCoalescingOperator(rewrittenLeft, leftPlaceholder, leftConversion, rewrittenResultType);
                }
            }

            // string concatenation is never null.
            // interpolated string lowering may introduce redundant null coalescing, which we have to remove.
            if (IsStringConcat(rewrittenLeft))
            {
                return GetConvertedLeftForNullCoalescingOperator(rewrittenLeft, leftPlaceholder, leftConversion, rewrittenResultType);
            }

            // if left conversion is intrinsic implicit (always succeeds) and results in a reference type
            // we can apply conversion before doing the null check that allows for a more efficient IL emit.
            Debug.Assert(rewrittenLeft.Type is { });
            if (rewrittenLeft.Type.IsReferenceType &&
                BoundNode.GetConversion(leftConversion, leftPlaceholder) is { IsImplicit: true, IsUserDefined: false })
            {
                rewrittenLeft = ApplyConversionIfNotIdentity(leftConversion, leftPlaceholder, rewrittenLeft);

                return new BoundNullCoalescingOperator(syntax, rewrittenLeft, rewrittenRight, leftPlaceholder: null, leftConversion: null, resultKind, @checked: false, rewrittenResultType);
            }

            if (BoundNode.GetConversion(leftConversion, leftPlaceholder) is { IsIdentity: true } or { Kind: ConversionKind.ExplicitNullable })
            {
                var conditionalAccess = rewrittenLeft as BoundLoweredConditionalAccess;
                if (conditionalAccess != null &&
                    (conditionalAccess.WhenNullOpt == null || NullableNeverHasValue(conditionalAccess.WhenNullOpt)))
                {
                    var notNullAccess = NullableAlwaysHasValue(conditionalAccess.WhenNotNull);
                    if (notNullAccess != null)
                    {
                        BoundExpression? whenNullOpt = rewrittenRight;

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
                            forceCopyOfNullableValueType: conditionalAccess.ForceCopyOfNullableValueType,
                            type: rewrittenResultType
                        );
                    }
                }
            }

            if (rewrittenLeft.Type.IsNullableType() &&
                rewrittenRight.Type.Equals(rewrittenLeft.Type.GetNullableUnderlyingType(), TypeCompareKind.AllIgnoreOptions))
            {
                var unwrappedRight = RemoveIdentityConversions(rewrittenRight);

                // Optimize left ?? right to left.GetValueOrDefault() when left is T? and right is the default value of T
                if (unwrappedRight.IsDefaultValue() &&
                    TryGetNullableMethod(rewrittenLeft.Syntax, rewrittenLeft.Type, SpecialMember.System_Nullable_T_GetValueOrDefault, out MethodSymbol? getValueOrDefault, isOptional: true))
                {
                    return BoundCall.Synthesized(rewrittenLeft.Syntax, rewrittenLeft, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, getValueOrDefault);
                }

                // Optimize left ?? right to left.GetValueOrDefault(right) when left is T? and right is a side-effectless expression of type T
                if (unwrappedRight is { ConstantValueOpt: not null } or BoundLocal { LocalSymbol.IsRef: false } or BoundParameter { ParameterSymbol.RefKind: RefKind.None } &&
                    TryGetNullableMethod(rewrittenLeft.Syntax, rewrittenLeft.Type, SpecialMember.System_Nullable_T_GetValueOrDefaultDefaultValue, out MethodSymbol? getValueOrDefaultDefaultValue, isOptional: true))
                {
                    return BoundCall.Synthesized(rewrittenLeft.Syntax, rewrittenLeft, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, getValueOrDefaultDefaultValue, rewrittenRight);
                }
            }

            // We lower left ?? right to
            //
            // var temp = left;
            // (temp != null) ? MakeConversion(temp) : right
            //

            BoundAssignmentOperator tempAssignment;
            BoundLocal boundTemp = _factory.StoreToTemp(rewrittenLeft, out tempAssignment);

            // temp != null
            BoundExpression nullCheck = _factory.MakeNullCheck(syntax, boundTemp, BinaryOperatorKind.NotEqual);

            // MakeConversion(temp, rewrittenResultType)
            BoundExpression convertedLeft = GetConvertedLeftForNullCoalescingOperator(boundTemp, leftPlaceholder, leftConversion, rewrittenResultType);
            Debug.Assert(convertedLeft.HasErrors || convertedLeft.Type!.Equals(rewrittenResultType, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

            // (temp != null) ? MakeConversion(temp, LeftConversion) : RightOperand
            BoundExpression conditionalExpression = RewriteConditionalOperator(
                syntax: syntax,
                rewrittenCondition: nullCheck,
                rewrittenConsequence: convertedLeft,
                rewrittenAlternative: rewrittenRight,
                constantValueOpt: null,
                rewrittenType: rewrittenResultType,
                isRef: false);

            Debug.Assert(conditionalExpression.ConstantValueOpt == null); // we shouldn't have hit this else case otherwise
            Debug.Assert(conditionalExpression.Type!.Equals(rewrittenResultType, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

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

        private BoundExpression GetConvertedLeftForNullCoalescingOperator(BoundExpression rewrittenLeft, BoundValuePlaceholder? leftPlaceholder, BoundExpression? leftConversion, TypeSymbol rewrittenResultType)
        {
            Debug.Assert(rewrittenLeft != null);
            Debug.Assert(rewrittenLeft.Type is { });
            Debug.Assert(rewrittenResultType is { });
            Debug.Assert(BoundNode.GetConversion(leftConversion, leftPlaceholder).IsValid);

            TypeSymbol rewrittenLeftType = rewrittenLeft.Type;
            Debug.Assert(rewrittenLeftType.IsNullableType() || !rewrittenLeftType.IsValueType);

            // Native compiler violates the specification for the case where result type is right operand type and left operand is nullable.
            // For this case, we need to insert an extra explicit nullable conversion from the left operand to its underlying nullable type
            // before performing the leftConversion.
            // See comments in Binder.BindNullCoalescingOperator referring to GetConvertedLeftForNullCoalescingOperator for more details.

            var conversionTakesNullableType = leftPlaceholder?.Type?.IsNullableType() == true;

            if (!TypeSymbol.Equals(rewrittenLeftType, rewrittenResultType, TypeCompareKind.ConsiderEverything2)
                && rewrittenLeftType.IsNullableType()
                && !conversionTakesNullableType)
            {
                TypeSymbol strippedLeftType = rewrittenLeftType.GetNullableUnderlyingType();
                MethodSymbol getValueOrDefault = UnsafeGetNullableMethod(rewrittenLeft.Syntax, rewrittenLeftType, SpecialMember.System_Nullable_T_GetValueOrDefault);
                rewrittenLeft = BoundCall.Synthesized(rewrittenLeft.Syntax, rewrittenLeft, initialBindingReceiverIsSubjectToCloning: ThreeState.Unknown, getValueOrDefault);
                if (TypeSymbol.Equals(strippedLeftType, rewrittenResultType, TypeCompareKind.ConsiderEverything2))
                {
                    return rewrittenLeft;
                }
            }

            rewrittenLeft = ApplyConversionIfNotIdentity(leftConversion, leftPlaceholder, rewrittenLeft);

            return rewrittenLeft;
        }
    }
}
