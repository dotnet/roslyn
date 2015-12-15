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

            return MakeNullCoalescingOperator(node.Syntax, rewrittenLeft, rewrittenRight, node.LeftConversion, rewrittenResultType);
        }

        private BoundExpression MakeNullCoalescingOperator(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenLeft,
            BoundExpression rewrittenRight,
            Conversion leftConversion,
            TypeSymbol rewrittenResultType)
        {
            Debug.Assert(rewrittenLeft != null);
            Debug.Assert(rewrittenRight != null);
            Debug.Assert(leftConversion.IsValid);
            Debug.Assert((object)rewrittenResultType != null);
            Debug.Assert(rewrittenRight.Type.Equals(rewrittenResultType, TypeSymbolEqualityOptions.IgnoreDynamic));

            if (_inExpressionLambda)
            {
                TypeSymbol strippedLeftType = rewrittenLeft.Type.StrippedType();
                Conversion rewrittenConversion = MakeConversion(syntax, leftConversion, strippedLeftType, rewrittenResultType);
                return new BoundNullCoalescingOperator(syntax, rewrittenLeft, rewrittenRight, rewrittenConversion, rewrittenResultType);
            }

            // first we can make a small optimization:
            // If left is a constant then we already know whether it is null or not. If it is null then we 
            // can simply generate "right". If it is not null then we can simply generate
            // MakeConversion(left).

            if (rewrittenLeft.IsDefaultValue())
            {
                return rewrittenRight;
            }

            if (rewrittenLeft.ConstantValue != null)
            {
                Debug.Assert(!rewrittenLeft.ConstantValue.IsNull);

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
                    rewrittenLeft = MakeConversion(rewrittenLeft.Syntax, rewrittenLeft, leftConversion, rewrittenResultType, @checked: false);
                }
                return new BoundNullCoalescingOperator(syntax, rewrittenLeft, rewrittenRight, Conversion.Identity, rewrittenResultType);
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
            Debug.Assert(convertedLeft.Type.Equals(rewrittenResultType, TypeSymbolEqualityOptions.IgnoreDynamic));

            // (temp != null) ? MakeConversion(temp, LeftConversion) : RightOperand
            BoundExpression conditionalExpression = RewriteConditionalOperator(
                syntax: syntax,
                rewrittenCondition: nullCheck,
                rewrittenConsequence: convertedLeft,
                rewrittenAlternative: rewrittenRight,
                constantValueOpt: null,
                rewrittenType: rewrittenResultType);

            Debug.Assert(conditionalExpression.ConstantValue == null); // we shouldn't have hit this else case otherwise
            Debug.Assert(conditionalExpression.Type.Equals(rewrittenResultType, TypeSymbolEqualityOptions.IgnoreDynamic));

            return new BoundSequence(
                syntax: syntax,
                locals: ImmutableArray.Create(boundTemp.LocalSymbol),
                sideEffects: ImmutableArray.Create<BoundExpression>(tempAssignment),
                value: conditionalExpression,
                type: rewrittenResultType);
        }

        private BoundExpression GetConvertedLeftForNullCoalescingOperator(BoundExpression rewrittenLeft, Conversion leftConversion, TypeSymbol rewrittenResultType)
        {
            Debug.Assert(rewrittenLeft != null);
            Debug.Assert((object)rewrittenLeft.Type != null);
            Debug.Assert((object)rewrittenResultType != null);
            Debug.Assert(leftConversion.IsValid);

            TypeSymbol rewrittenLeftType = rewrittenLeft.Type;
            Debug.Assert(rewrittenLeftType.IsNullableType() || rewrittenLeftType.IsReferenceType);

            // Native compiler violates the specification for the case where result type is right operand type and left operand is nullable.
            // For this case, we need to insert an extra explicit nullable conversion from the left operand to its underlying nullable type
            // before performing the leftConversion.
            // See comments in Binder.BindNullCoalescingOperator referring to GetConvertedLeftForNullCoalescingOperator for more details.

            if (rewrittenLeftType != rewrittenResultType && rewrittenLeftType.IsNullableType())
            {
                TypeSymbol strippedLeftType = rewrittenLeftType.GetNullableUnderlyingType();
                MethodSymbol getValueOrDefault = GetNullableMethod(rewrittenLeft.Syntax, rewrittenLeftType, SpecialMember.System_Nullable_T_GetValueOrDefault);
                rewrittenLeft = BoundCall.Synthesized(rewrittenLeft.Syntax, rewrittenLeft, getValueOrDefault);
                if (strippedLeftType == rewrittenResultType)
                {
                    return rewrittenLeft;
                }
            }

            return MakeConversion(rewrittenLeft.Syntax, rewrittenLeft, leftConversion, rewrittenResultType, @checked: false);
        }
    }
}
