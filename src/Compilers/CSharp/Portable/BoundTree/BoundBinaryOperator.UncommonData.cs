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
        internal class UncommonData
        {
            public static UncommonData UnconvertedInterpolatedStringAddition(ConstantValue? constantValue) =>
                new UncommonData(
                    constantValue,
                    method: null,
                    constrainedToType: null,
                    originalUserDefinedOperatorsOpt: default,
                    isUnconvertedInterpolatedStringAddition: true,
                    interpolatedStringHandlerData: null);

            public static UncommonData InterpolatedStringHandlerAddition(InterpolatedStringHandlerData data)
                => new UncommonData(
                    constantValue: null,
                    method: null,
                    constrainedToType: null,
                    originalUserDefinedOperatorsOpt: default,
                    isUnconvertedInterpolatedStringAddition: false,
                    data);

            public static UncommonData? CreateIfNeeded(ConstantValue? constantValue, MethodSymbol? method, TypeSymbol? constrainedToType, ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt)
            {
                if (constantValue != null || method is not null || constrainedToType is not null || !originalUserDefinedOperatorsOpt.IsDefault)
                {
                    return new UncommonData(constantValue, method, constrainedToType, originalUserDefinedOperatorsOpt, isUnconvertedInterpolatedStringAddition: false, interpolatedStringHandlerData: null);
                }

                return null;
            }

            public readonly ConstantValue? ConstantValue;
            public readonly MethodSymbol? Method;
            public readonly TypeSymbol? ConstrainedToType;
            public readonly bool IsUnconvertedInterpolatedStringAddition;
            public readonly InterpolatedStringHandlerData? InterpolatedStringHandlerData;

            // The set of method symbols from which this operator's method was chosen.
            // Only kept in the tree if the operator was an error and overload resolution
            // was unable to choose a best method.
            public readonly ImmutableArray<MethodSymbol> OriginalUserDefinedOperatorsOpt;

            private UncommonData(ConstantValue? constantValue, MethodSymbol? method, TypeSymbol? constrainedToType, ImmutableArray<MethodSymbol> originalUserDefinedOperatorsOpt, bool isUnconvertedInterpolatedStringAddition, InterpolatedStringHandlerData? interpolatedStringHandlerData)
            {
                Debug.Assert(interpolatedStringHandlerData is null || !isUnconvertedInterpolatedStringAddition);
                ConstantValue = constantValue;
                Method = method;
                ConstrainedToType = constrainedToType;
                OriginalUserDefinedOperatorsOpt = originalUserDefinedOperatorsOpt;
                IsUnconvertedInterpolatedStringAddition = isUnconvertedInterpolatedStringAddition;
                InterpolatedStringHandlerData = interpolatedStringHandlerData;
            }

            public UncommonData WithUpdatedMethod(MethodSymbol? method)
            {
                if ((object?)method == Method)
                {
                    return this;
                }

                return new UncommonData(ConstantValue, method, ConstrainedToType, OriginalUserDefinedOperatorsOpt, IsUnconvertedInterpolatedStringAddition, InterpolatedStringHandlerData);
            }
        }
    }
}
