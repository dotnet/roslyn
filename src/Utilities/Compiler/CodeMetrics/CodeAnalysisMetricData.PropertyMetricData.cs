// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeMetrics
{
    internal abstract partial class CodeAnalysisMetricData
    {
        private sealed class PropertyMetricData : CodeAnalysisMetricData
        {
            internal PropertyMetricData(
                IPropertySymbol symbol,
                int maintainabilityIndex,
                ComputationalComplexityMetrics computationalComplexityMetrics,
                ImmutableHashSet<INamedTypeSymbol> coupledNamedTypes,
                long linesOfCode,
                int cyclomaticComplexity,
                int? depthOfInheritance,
                ImmutableArray<CodeAnalysisMetricData> children)
                : base(symbol, maintainabilityIndex, computationalComplexityMetrics, coupledNamedTypes,
                      linesOfCode, cyclomaticComplexity, depthOfInheritance, children)
            {
            }

            internal static async Task<PropertyMetricData> ComputeAsync(IPropertySymbol property, SemanticModelProvider semanticModelProvider, CancellationToken cancellationToken)
            {
                var coupledTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
                ImmutableArray<SyntaxReference> declarations = property.DeclaringSyntaxReferences;
                long linesOfCode = await MetricsHelper.GetLinesOfCodeAsync(declarations, property, semanticModelProvider, cancellationToken).ConfigureAwait(false);
                (int cyclomaticComplexity, ComputationalComplexityMetrics computationalComplexityMetrics) =
                    await MetricsHelper.ComputeCoupledTypesAndComplexityExcludingMemberDeclsAsync(declarations, property, coupledTypesBuilder, semanticModelProvider, cancellationToken).ConfigureAwait(false);
                MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, property.Parameters);
                MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, property.Type);

                ImmutableArray<CodeAnalysisMetricData> children = await ComputeAsync(GetAccessors(property), semanticModelProvider, cancellationToken).ConfigureAwait(false);
                int maintainabilityIndexTotal = 0;
                foreach (CodeAnalysisMetricData child in children)
                {
                    MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, child.CoupledNamedTypes);
                    maintainabilityIndexTotal += child.MaintainabilityIndex;
                    cyclomaticComplexity += child.CyclomaticComplexity;
                    computationalComplexityMetrics = computationalComplexityMetrics.Union(child.ComputationalComplexityMetrics);
                }

                int? depthOfInheritance = null;
                int maintainabilityIndex = children.Length > 0 ? MetricsHelper.GetAverageRoundedMetricValue(maintainabilityIndexTotal, children.Length) : 100;
                MetricsHelper.RemoveContainingTypes(property, coupledTypesBuilder);

                return new PropertyMetricData(property, maintainabilityIndex, computationalComplexityMetrics,
                    coupledTypesBuilder.ToImmutable(), linesOfCode, cyclomaticComplexity, depthOfInheritance, children);
            }

            private static IEnumerable<IMethodSymbol> GetAccessors(IPropertySymbol property)
            {
                if (property.GetMethod != null)
                {
                    yield return property.GetMethod;
                }

                if (property.SetMethod != null)
                {
                    yield return property.SetMethod;
                }
            }
        }
    }
}

#endif
