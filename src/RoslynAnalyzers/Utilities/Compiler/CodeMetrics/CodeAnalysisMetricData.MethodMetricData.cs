// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeMetrics
{
    public abstract partial class CodeAnalysisMetricData
    {
        private sealed class MethodMetricData : CodeAnalysisMetricData
        {
            internal MethodMetricData(
                IMethodSymbol symbol,
                int maintainabilityIndex,
                ComputationalComplexityMetrics computationalComplexityMetrics,
                ImmutableHashSet<INamedTypeSymbol> coupledNamedTypes,
                long linesOfCode,
                int cyclomaticComplexity,
                int? depthOfInheritance)
                : base(symbol, maintainabilityIndex, computationalComplexityMetrics, coupledNamedTypes,
                      linesOfCode, cyclomaticComplexity, depthOfInheritance, children: ImmutableArray<CodeAnalysisMetricData>.Empty)
            {
            }

            internal static MethodMetricData Compute(IMethodSymbol method, CodeMetricsAnalysisContext context)
            {
                var coupledTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
                ImmutableArray<SyntaxReference> declarations = method.DeclaringSyntaxReferences;
                long linesOfCode = MetricsHelper.GetLinesOfCode(declarations, method, context);
                (int cyclomaticComplexity, ComputationalComplexityMetrics computationalComplexityMetrics) =
                    MetricsHelper.ComputeCoupledTypesAndComplexityExcludingMemberDecls(declarations, method, coupledTypesBuilder, context);
                MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, context.WellKnownTypeProvider, method.Parameters);
                if (!method.ReturnsVoid)
                {
                    MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, context.WellKnownTypeProvider, method.ReturnType);
                }

                int? depthOfInheritance = null;
                int maintainabilityIndex = CalculateMaintainabilityIndex(computationalComplexityMetrics, cyclomaticComplexity);
                MetricsHelper.RemoveContainingTypes(method, coupledTypesBuilder);

                if (cyclomaticComplexity == 0)
                {
                    // Empty method, such as auto-generated accessor.
                    cyclomaticComplexity = 1;
                }

                return new MethodMetricData(method, maintainabilityIndex, computationalComplexityMetrics,
                    coupledTypesBuilder.ToImmutable(), linesOfCode, cyclomaticComplexity, depthOfInheritance);
            }

            private static int CalculateMaintainabilityIndex(ComputationalComplexityMetrics computationalComplexityMetrics, int cyclomaticComplexity)
            {
                double computationalComplexityVolume = Math.Max(0.0, Math.Log(computationalComplexityMetrics.Volume));   //avoid Log(0) = -Infinity
                double logEffectiveLinesOfCode = Math.Max(0.0, Math.Log(computationalComplexityMetrics.EffectiveLinesOfCode));          //avoid Log(0) = -Infinity
                return MetricsHelper.NormalizeAndRoundMaintainabilityIndex(171 - 5.2 * computationalComplexityVolume - 0.23 * cyclomaticComplexity - 16.2 * logEffectiveLinesOfCode);
            }
        }
    }
}
