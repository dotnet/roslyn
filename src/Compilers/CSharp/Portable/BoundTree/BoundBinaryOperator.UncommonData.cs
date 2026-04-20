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
                    isChainedRelational: false,
                    chainedRelationalLeftOperand: null);

            public static UncommonData InterpolatedStringHandlerAddition(InterpolatedStringHandlerData data)
                => new UncommonData(
                    constantValue: null,
                    method: null,
                    constrainedToType: null,
                    originalUserDefinedOperatorsOpt: default,
                    isUnconvertedInterpolatedStringAddition: false,
                    data,
                    isChainedRelational: false,
                    chainedRelationalLeftOperand: null);

            public static UncommonData ChainedRelational(ConstantValue? constantValue, MethodSymbol? method, TypeSymbol? constrainedToType, ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt, BoundExpression chainedRelationalLeftOperand)
                => new UncommonData(
                    constantValue,
                    method,
                    constrainedToType,
                    originalUserDefinedOperatorsOpt,
                    isUnconvertedInterpolatedStringAddition: false,
                    interpolatedStringHandlerData: null,
                    isChainedRelational: true,
                    chainedRelationalLeftOperand);

            public static UncommonData? CreateIfNeeded(ConstantValue? constantValue, MethodSymbol? method, TypeSymbol? constrainedToType, ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt)
            {
                if (constantValue != null || method is not null || constrainedToType is not null || !originalUserDefinedOperatorsOpt.IsDefault)
                {
                    return new UncommonData(constantValue, method, constrainedToType, originalUserDefinedOperatorsOpt, isUnconvertedInterpolatedStringAddition: false, interpolatedStringHandlerData: null, isChainedRelational: false, chainedRelationalLeftOperand: null);
                }

                return null;
            }

            public readonly ConstantValue? ConstantValue;
            public readonly MethodSymbol? Method;
            public readonly TypeSymbol? ConstrainedToType;
            public readonly bool IsUnconvertedInterpolatedStringAddition;
            public readonly InterpolatedStringHandlerData? InterpolatedStringHandlerData;

            // True when this BoundBinaryOperator is a chained relational comparison
            // (spec §11.11.13). The left operand is a bool-typed BoundBinaryOperator whose
            // right operand (call it Y) is the shared middle operand, and this node's Method
            // is the operator selected by isolated overload resolution on `Y op Right`.
            // At lowering time, such a node is rewritten to a short-circuit && form.
            public readonly bool IsChainedRelational;

            // The converted Y value (the shared middle operand of the chain link): Y is the
            // right operand of <see cref="BoundBinaryOperator.Left"/> with the conversion
            // required by <see cref="Method"/> (or by the resolved operator's signature)
            // applied. Stored at bind time so the lowerer does not need to re-run overload
            // resolution to figure out how to combine Y with the outer node's right operand.
            // Non-null iff <see cref="IsChainedRelational"/> is true.
            public readonly BoundExpression? ChainedRelationalLeftOperand;

            // The set of method symbols from which this operator's method was chosen.
            // Only kept in the tree if the operator was an error and overload resolution
            // was unable to choose a best method.
            public readonly ImmutableArray<MethodSymbol> OriginalUserDefinedOperatorsOpt;

            private UncommonData(ConstantValue? constantValue, MethodSymbol? method, TypeSymbol? constrainedToType, ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt, bool isUnconvertedInterpolatedStringAddition, InterpolatedStringHandlerData? interpolatedStringHandlerData, bool isChainedRelational, BoundExpression? chainedRelationalLeftOperand)
            {
                Debug.Assert(interpolatedStringHandlerData is null || !isUnconvertedInterpolatedStringAddition);
                Debug.Assert(!isChainedRelational || (interpolatedStringHandlerData is null && !isUnconvertedInterpolatedStringAddition));
                Debug.Assert(isChainedRelational == (chainedRelationalLeftOperand is not null));
                Debug.Assert(method is null or ErrorMethodSymbol { ParameterCount: 0 } or { MethodKind: MethodKind.UserDefinedOperator } or { ParameterCount: 2 });

                ConstantValue = constantValue;
                Method = method;
                ConstrainedToType = constrainedToType;
                OriginalUserDefinedOperatorsOpt = originalUserDefinedOperatorsOpt;
                IsUnconvertedInterpolatedStringAddition = isUnconvertedInterpolatedStringAddition;
                InterpolatedStringHandlerData = interpolatedStringHandlerData;
                IsChainedRelational = isChainedRelational;
                ChainedRelationalLeftOperand = chainedRelationalLeftOperand;
            }

            public UncommonData WithUpdatedMethod(MethodSymbol? method)
            {
                if ((object?)method == Method)
                {
                    return this;
                }

                return new UncommonData(ConstantValue, method, ConstrainedToType, OriginalUserDefinedOperatorsOpt, IsUnconvertedInterpolatedStringAddition, InterpolatedStringHandlerData, IsChainedRelational, ChainedRelationalLeftOperand);
            }
        }
    }
}
