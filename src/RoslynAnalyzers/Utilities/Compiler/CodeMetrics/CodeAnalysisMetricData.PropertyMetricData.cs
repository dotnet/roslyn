﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if HAS_IOPERATION

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeMetrics
{
    public abstract partial class CodeAnalysisMetricData
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

            internal static PropertyMetricData Compute(IPropertySymbol property, CodeMetricsAnalysisContext context)
            {
                var coupledTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
                ImmutableArray<SyntaxReference> declarations = property.DeclaringSyntaxReferences;
                long linesOfCode = MetricsHelper.GetLinesOfCode(declarations, property, context);
                (int cyclomaticComplexity, ComputationalComplexityMetrics computationalComplexityMetrics) =
                    MetricsHelper.ComputeCoupledTypesAndComplexityExcludingMemberDecls(declarations, property, coupledTypesBuilder, context);
                MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, context.WellKnownTypeProvider, property.Parameters);
                MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, context.WellKnownTypeProvider, property.Type);

                ImmutableArray<CodeAnalysisMetricData> children = ComputeSynchronously(GetAccessors(property), context);
                int maintainabilityIndexTotal = 0;
                foreach (CodeAnalysisMetricData child in children)
                {
                    MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, context.WellKnownTypeProvider, child.CoupledNamedTypes);
                    maintainabilityIndexTotal += child.MaintainabilityIndex;
                    cyclomaticComplexity += child.CyclomaticComplexity;
                    computationalComplexityMetrics = computationalComplexityMetrics.Union(child.ComputationalComplexityMetrics);
                }

                int? depthOfInheritance = null;
                int maintainabilityIndex = !children.IsEmpty ? MetricsHelper.GetAverageRoundedMetricValue(maintainabilityIndexTotal, children.Length) : 100;
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
