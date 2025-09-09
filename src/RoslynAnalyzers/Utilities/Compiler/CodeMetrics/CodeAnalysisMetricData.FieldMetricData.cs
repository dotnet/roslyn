// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeMetrics
{
    public abstract partial class CodeAnalysisMetricData
    {
        private sealed class FieldMetricData : CodeAnalysisMetricData
        {
            internal FieldMetricData(
                IFieldSymbol symbol,
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

            internal static FieldMetricData Compute(IFieldSymbol field, CodeMetricsAnalysisContext context)
            {
                var coupledTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
                ImmutableArray<SyntaxReference> declarations = field.DeclaringSyntaxReferences;
                long linesOfCode = MetricsHelper.GetLinesOfCode(declarations, field, context);
                (int cyclomaticComplexity, ComputationalComplexityMetrics computationalComplexityMetrics) =
                    MetricsHelper.ComputeCoupledTypesAndComplexityExcludingMemberDecls(declarations, field, coupledTypesBuilder, context);
                MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, context.WellKnownTypeProvider, field.Type);
                int? depthOfInheritance = null;
                int maintainabilityIndex = CalculateMaintainabilityIndex(computationalComplexityMetrics, cyclomaticComplexity);
                MetricsHelper.RemoveContainingTypes(field, coupledTypesBuilder);

                return new FieldMetricData(field, maintainabilityIndex, computationalComplexityMetrics,
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
