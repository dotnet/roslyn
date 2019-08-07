// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeMetrics
{
    internal abstract partial class CodeAnalysisMetricData
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

            internal static async Task<MethodMetricData> ComputeAsync(IMethodSymbol method, SemanticModelProvider semanticModelProvider, CancellationToken cancellationToken)
            {
                var coupledTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
                ImmutableArray<SyntaxReference> declarations = method.DeclaringSyntaxReferences;
                long linesOfCode = await MetricsHelper.GetLinesOfCodeAsync(declarations, method, semanticModelProvider, cancellationToken).ConfigureAwait(false);
                (int cyclomaticComplexity, ComputationalComplexityMetrics computationalComplexityMetrics) =
                    await MetricsHelper.ComputeCoupledTypesAndComplexityExcludingMemberDeclsAsync(declarations, method, coupledTypesBuilder, semanticModelProvider, cancellationToken).ConfigureAwait(false);
                MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, method.Parameters);
                if (!method.ReturnsVoid)
                {
                    MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, method.ReturnType);
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

#endif
