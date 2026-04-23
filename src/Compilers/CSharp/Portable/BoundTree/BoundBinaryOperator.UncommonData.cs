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
            else
            {
                // No method stored => operator must not be user-defined (intrinsic, or
                // dynamic handled by the runtime binder). For chained relational comparison
                // specifically, this means the outer link's LeftType is recoverable from
                // OperatorKind alone - not needed today because we store the converted type,
                // but a useful invariant to pin regardless.
                Debug.Assert(!OperatorKind.IsUserDefined());
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
                    chainedRelationalLeftConversion: null);

            public static UncommonData InterpolatedStringHandlerAddition(InterpolatedStringHandlerData data)
                => new UncommonData(
                    constantValue: null,
                    method: null,
                    constrainedToType: null,
                    originalUserDefinedOperatorsOpt: default,
                    isUnconvertedInterpolatedStringAddition: false,
                    data,
                    chainedRelationalLeftConversion: null);

            public static UncommonData ChainedRelational(
                ConstantValue? constantValue,
                MethodSymbol? method,
                TypeSymbol? constrainedToType,
                ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt,
                Conversion chainedRelationalLeftConversion)
                => new UncommonData(
                    constantValue,
                    method,
                    constrainedToType,
                    originalUserDefinedOperatorsOpt,
                    isUnconvertedInterpolatedStringAddition: false,
                    interpolatedStringHandlerData: null,
                    chainedRelationalLeftConversion);

            public static UncommonData? CreateIfNeeded(ConstantValue? constantValue, MethodSymbol? method, TypeSymbol? constrainedToType, ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt)
            {
                if (constantValue != null || method is not null || constrainedToType is not null || !originalUserDefinedOperatorsOpt.IsDefault)
                {
                    return new UncommonData(constantValue, method, constrainedToType, originalUserDefinedOperatorsOpt, isUnconvertedInterpolatedStringAddition: false, interpolatedStringHandlerData: null, chainedRelationalLeftConversion: null);
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
            // Non-null iff this node is a chained relational comparison; this is the signal
            // <see cref="BoundBinaryOperator.IsChainedRelational"/> keys off. Consumers
            // should use that helper rather than inspecting this field directly. The
            // conversion's target type is re-derived on demand from
            // <c>BinaryOperatorMethod.Parameters[0].Type</c> (user-defined ops, possibly
            // asymmetric) or <c>Right.Type</c> (intrinsic ops have symmetric signatures so
            // <c>LeftType == RightType == Right.Type</c>).
            //
            // Y itself is also deliberately not cached here - it's always
            // <c>((BoundBinaryOperator)Left).Right</c>. Deriving it on demand keeps Y on the
            // standard bound-tree descent path (so <c>NullableWalker.DebugVerifier</c>
            // reaches it automatically) and avoids stale state if the outer node is ever
            // rebuilt with a different <c>Left</c>.
            public readonly Conversion? ChainedRelationalLeftConversion;

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
                Conversion? chainedRelationalLeftConversion)
            {
                Debug.Assert(interpolatedStringHandlerData is null || !isUnconvertedInterpolatedStringAddition);
                Debug.Assert(chainedRelationalLeftConversion is null || (interpolatedStringHandlerData is null && !isUnconvertedInterpolatedStringAddition));
                // A stored chained-relational conversion must actually exist (Identity at
                // minimum); NoConversion would be ambiguous with "not chained".
                Debug.Assert(chainedRelationalLeftConversion is null || chainedRelationalLeftConversion.Value.Exists);
                Debug.Assert(method is null or ErrorMethodSymbol { ParameterCount: 0 } or { MethodKind: MethodKind.UserDefinedOperator } or { ParameterCount: 2 });

                ConstantValue = constantValue;
                Method = method;
                ConstrainedToType = constrainedToType;
                OriginalUserDefinedOperatorsOpt = originalUserDefinedOperatorsOpt;
                IsUnconvertedInterpolatedStringAddition = isUnconvertedInterpolatedStringAddition;
                InterpolatedStringHandlerData = interpolatedStringHandlerData;
                ChainedRelationalLeftConversion = chainedRelationalLeftConversion;
            }

            public UncommonData WithUpdatedMethod(MethodSymbol? method)
            {
                if ((object?)method == Method)
                {
                    return this;
                }

                return new UncommonData(ConstantValue, method, ConstrainedToType, OriginalUserDefinedOperatorsOpt, IsUnconvertedInterpolatedStringAddition, InterpolatedStringHandlerData, ChainedRelationalLeftConversion);
            }
        }
    }
}
