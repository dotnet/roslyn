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

            // The outer link's LeftConversion selected by overload resolution: applied at
            // lowering time to the temp holding Y (the shared middle operand) to produce
            // the outer link's left operand. May be <see cref="Conversion.Identity"/> for
            // common same-type chains, in which case the lowerer uses the temp directly.
            //
            // Y itself is deliberately not cached here - it's always
            // <c>((BoundBinaryOperator)Left).Right</c>. Deriving it on demand keeps Y on the
            // standard bound-tree descent path (so <c>NullableWalker.DebugVerifier</c>
            // reaches it automatically) and avoids stale state if the outer node is ever
            // rebuilt with a different <c>Left</c>.
            //
            // <see cref="Conversion.NoConversion"/> means "not chained"; consumers should
            // guard on <see cref="BoundBinaryOperator.IsChainedRelational"/> rather than
            // inspect this field directly.
            public readonly Conversion ChainedRelationalLeftConversion;

            // Target type of <see cref="ChainedRelationalLeftConversion"/> (signature.LeftType
            // at bind time). Non-null iff this node is a chained relational comparison;
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
