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
                    chainedRelationalLeftConversion: Conversion.NoConversion,
                    chainedRelationalLeftConvertedType: null);

            public static UncommonData ChainedRelational(
                ConstantValue? constantValue,
                MethodSymbol? method,
                TypeSymbol? constrainedToType,
                ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt,
                Conversion chainedRelationalLeftConversion,
                TypeSymbol chainedRelationalLeftConvertedType)
                => new UncommonData(
                    constantValue,
                    method,
                    constrainedToType,
                    originalUserDefinedOperatorsOpt,
                    isUnconvertedInterpolatedStringAddition: false,
                    interpolatedStringHandlerData: null,
                    chainedRelationalLeftConversion,
                    chainedRelationalLeftConvertedType);

            public static UncommonData? CreateIfNeeded(ConstantValue? constantValue, MethodSymbol? method, TypeSymbol? constrainedToType, ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt)
            {
                if (constantValue != null || method is not null || constrainedToType is not null || !originalUserDefinedOperatorsOpt.IsDefault)
                {
                    return new UncommonData(constantValue, method, constrainedToType, originalUserDefinedOperatorsOpt, isUnconvertedInterpolatedStringAddition: false, interpolatedStringHandlerData: null, chainedRelationalLeftConversion: Conversion.NoConversion, chainedRelationalLeftConvertedType: null);
                }

                return null;
            }

            public readonly ConstantValue? ConstantValue;
            public readonly MethodSymbol? Method;
            public readonly TypeSymbol? ConstrainedToType;
            public readonly bool IsUnconvertedInterpolatedStringAddition;
            public readonly InterpolatedStringHandlerData? InterpolatedStringHandlerData;

            // The conversion the outer link's overload resolution selected for its left
            // operand - i.e. the chain's shared middle operand `Y`, post-inner-link. Applied
            // at lowering time to the temp holding Y's inner-link value to produce the
            // outer link's left operand. May be <see cref="Conversion.Identity"/> (common
            // same-type chains), in which case the lowerer uses the temp directly.
            //
            // `Y` itself is always <c>((BoundBinaryOperator)BoundBinaryOperator.Left).Right</c>:
            // the right operand of the (guaranteed-bool-typed) inner link. So we derive it
            // at each use site rather than cache it on UncommonData - that keeps Y on the
            // standard bound-tree descent path (which makes <c>NullableWalker.DebugVerifier</c>
            // reach it automatically) and avoids any stale-state risk if the outer node
            // is ever rebuilt with a different <c>Left</c>.
            //
            // <see cref="Conversion.NoConversion"/> means "not chained" (no conversion info
            // present); consumers should guard on <see cref="BoundBinaryOperator.IsChainedRelational"/>
            // instead of inspecting this field directly.
            public readonly Conversion ChainedRelationalLeftConversion;

            // The target type of <see cref="ChainedRelationalLeftConversion"/>: the outer
            // link's left-operand type (<c>signature.LeftType</c> at bind time). Non-null
            // exactly when this node is a chained relational comparison; also doubles as
            // the signal <see cref="BoundBinaryOperator.IsChainedRelational"/> keys off.
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
                Conversion chainedRelationalLeftConversion,
                TypeSymbol? chainedRelationalLeftConvertedType)
            {
                Debug.Assert(interpolatedStringHandlerData is null || !isUnconvertedInterpolatedStringAddition);
                Debug.Assert(chainedRelationalLeftConvertedType is null || (interpolatedStringHandlerData is null && !isUnconvertedInterpolatedStringAddition));
                // The two chained-relational fields are all-or-nothing.
                Debug.Assert((chainedRelationalLeftConvertedType is null) == !chainedRelationalLeftConversion.Exists);
                Debug.Assert(method is null or ErrorMethodSymbol { ParameterCount: 0 } or { MethodKind: MethodKind.UserDefinedOperator } or { ParameterCount: 2 });

                ConstantValue = constantValue;
                Method = method;
                ConstrainedToType = constrainedToType;
                OriginalUserDefinedOperatorsOpt = originalUserDefinedOperatorsOpt;
                IsUnconvertedInterpolatedStringAddition = isUnconvertedInterpolatedStringAddition;
                InterpolatedStringHandlerData = interpolatedStringHandlerData;
                ChainedRelationalLeftConversion = chainedRelationalLeftConversion;
                ChainedRelationalLeftConvertedType = chainedRelationalLeftConvertedType;
            }

            public UncommonData WithUpdatedMethod(MethodSymbol? method)
            {
                if ((object?)method == Method)
                {
                    return this;
                }

                return new UncommonData(ConstantValue, method, ConstrainedToType, OriginalUserDefinedOperatorsOpt, IsUnconvertedInterpolatedStringAddition, InterpolatedStringHandlerData, ChainedRelationalLeftConversion, ChainedRelationalLeftConvertedType);
            }
        }
    }
}
