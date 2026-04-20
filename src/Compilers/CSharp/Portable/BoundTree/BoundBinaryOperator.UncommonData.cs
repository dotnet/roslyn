// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundBinaryOperator
    {
        private partial void Validate()
        {
            if (Data is { Method: { } method })
            {
                if (OperatorKind.IsDynamic())
                {
                    Debug.Assert(OperatorKind.IsLogical());
                    Debug.Assert(method.Name is WellKnownMemberNames.TrueOperatorName or WellKnownMemberNames.FalseOperatorName);
                    Debug.Assert(method.ParameterCount == 1);
                }
                else
                {
                    Debug.Assert(method is ErrorMethodSymbol or { ParameterCount: 2 });
                }
            }
        }

        internal class UncommonData
        {
            public static UncommonData UnconvertedInterpolatedStringAddition(ConstantValue? constantValue) =>
                new UncommonData(
                    constantValue,
                    method: null,
                    constrainedToType: null,
                    originalUserDefinedOperatorsOpt: default,
                    isUnconvertedInterpolatedStringAddition: true,
                    interpolatedStringHandlerData: null,
                    chainedRelationalLeftOperand: null,
                    chainedRelationalLeftConversion: Conversion.NoConversion,
                    chainedRelationalLeftConvertedType: null);

            public static UncommonData InterpolatedStringHandlerAddition(InterpolatedStringHandlerData data)
                => new UncommonData(
                    constantValue: null,
                    method: null,
                    constrainedToType: null,
                    originalUserDefinedOperatorsOpt: default,
                    isUnconvertedInterpolatedStringAddition: false,
                    data,
                    chainedRelationalLeftOperand: null,
                    chainedRelationalLeftConversion: Conversion.NoConversion,
                    chainedRelationalLeftConvertedType: null);

            public static UncommonData ChainedRelational(
                ConstantValue? constantValue,
                MethodSymbol? method,
                TypeSymbol? constrainedToType,
                ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt,
                BoundExpression chainedRelationalLeftOperand,
                Conversion chainedRelationalLeftConversion,
                TypeSymbol chainedRelationalLeftConvertedType)
                => new UncommonData(
                    constantValue,
                    method,
                    constrainedToType,
                    originalUserDefinedOperatorsOpt,
                    isUnconvertedInterpolatedStringAddition: false,
                    interpolatedStringHandlerData: null,
                    chainedRelationalLeftOperand,
                    chainedRelationalLeftConversion,
                    chainedRelationalLeftConvertedType);

            public static UncommonData? CreateIfNeeded(ConstantValue? constantValue, MethodSymbol? method, TypeSymbol? constrainedToType, ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt)
            {
                if (constantValue != null || method is not null || constrainedToType is not null || !originalUserDefinedOperatorsOpt.IsDefault)
                {
                    return new UncommonData(constantValue, method, constrainedToType, originalUserDefinedOperatorsOpt, isUnconvertedInterpolatedStringAddition: false, interpolatedStringHandlerData: null, chainedRelationalLeftOperand: null, chainedRelationalLeftConversion: Conversion.NoConversion, chainedRelationalLeftConvertedType: null);
                }

                return null;
            }

            public readonly ConstantValue? ConstantValue;
            public readonly MethodSymbol? Method;
            public readonly TypeSymbol? ConstrainedToType;
            public readonly bool IsUnconvertedInterpolatedStringAddition;
            public readonly InterpolatedStringHandlerData? InterpolatedStringHandlerData;

            // The shared middle operand `Y` for a chained relational comparison
            // (spec §11.11.13), with *only* the inner link's conversion already applied -
            // i.e. exactly what <c>BoundBinaryOperator.Left.Right</c> is on the outer
            // chained node. This is the value the lowerer hoists into a temp and feeds
            // to the inner link's comparison. The outer link's own LeftConversion (if any)
            // is applied at the outer comparison's point of use via
            // <see cref="ChainedRelationalLeftConversion"/> and
            // <see cref="ChainedRelationalLeftConvertedType"/>; it is NOT baked into the
            // value stored here. Keeping the two separate is what makes
            // <c>short &lt; int &lt; long</c> and similar asymmetric chains emit
            // verifiable IL (the temp's type is Y's inner-link type, and the outer
            // conversion lives on the load side).
            //
            // Non-null exactly when this node is a chained relational comparison
            // (see <see cref="BoundBinaryOperator.IsChainedRelational"/>).
            public readonly BoundExpression? ChainedRelationalLeftOperand;

            // The conversion the outer link's overload resolution selected for its left
            // operand (i.e. the chain's shared middle operand Y, post-inner-link). Applied
            // at lowering time to the temp holding Y's inner-link value to produce the
            // outer link's left operand. May be <see cref="Conversion.Identity"/> (common
            // same-type chains), in which case the lowerer uses the temp directly.
            // <see cref="Conversion.NoConversion"/> means "not chained" (no conversion
            // info present); consumers should guard on
            // <see cref="BoundBinaryOperator.IsChainedRelational"/> instead of inspecting
            // this field directly.
            public readonly Conversion ChainedRelationalLeftConversion;

            // The target type of <see cref="ChainedRelationalLeftConversion"/>: i.e., the
            // outer link's left-operand type (<c>signature.LeftType</c> at bind time).
            // Non-null exactly when this node is a chained relational comparison.
            public readonly TypeSymbol? ChainedRelationalLeftConvertedType;

            // The set of method symbols from which this operator's method was chosen.
            // Only kept in the tree if the operator was an error and overload resolution
            // was unable to choose a best method.
            public readonly ImmutableArray<MethodSymbol> OriginalUserDefinedOperatorsOpt;

            private UncommonData(
                ConstantValue? constantValue,
                MethodSymbol? method,
                TypeSymbol? constrainedToType,
                ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt,
                bool isUnconvertedInterpolatedStringAddition,
                InterpolatedStringHandlerData? interpolatedStringHandlerData,
                BoundExpression? chainedRelationalLeftOperand,
                Conversion chainedRelationalLeftConversion,
                TypeSymbol? chainedRelationalLeftConvertedType)
            {
                Debug.Assert(interpolatedStringHandlerData is null || !isUnconvertedInterpolatedStringAddition);
                Debug.Assert(chainedRelationalLeftOperand is null || (interpolatedStringHandlerData is null && !isUnconvertedInterpolatedStringAddition));
                // The three chained-relational fields are all-or-nothing.
                Debug.Assert((chainedRelationalLeftOperand is null) == (chainedRelationalLeftConvertedType is null));
                Debug.Assert((chainedRelationalLeftOperand is null) == !chainedRelationalLeftConversion.Exists);
                Debug.Assert(method is null or ErrorMethodSymbol { ParameterCount: 0 } or { MethodKind: MethodKind.UserDefinedOperator } or { ParameterCount: 2 });

                ConstantValue = constantValue;
                Method = method;
                ConstrainedToType = constrainedToType;
                OriginalUserDefinedOperatorsOpt = originalUserDefinedOperatorsOpt;
                IsUnconvertedInterpolatedStringAddition = isUnconvertedInterpolatedStringAddition;
                InterpolatedStringHandlerData = interpolatedStringHandlerData;
                ChainedRelationalLeftOperand = chainedRelationalLeftOperand;
                ChainedRelationalLeftConversion = chainedRelationalLeftConversion;
                ChainedRelationalLeftConvertedType = chainedRelationalLeftConvertedType;
            }

            public UncommonData WithUpdatedMethod(MethodSymbol? method)
            {
                if ((object?)method == Method)
                {
                    return this;
                }

                return new UncommonData(ConstantValue, method, ConstrainedToType, OriginalUserDefinedOperatorsOpt, IsUnconvertedInterpolatedStringAddition, InterpolatedStringHandlerData, ChainedRelationalLeftOperand, ChainedRelationalLeftConversion, ChainedRelationalLeftConvertedType);
            }
        }
    }
}
