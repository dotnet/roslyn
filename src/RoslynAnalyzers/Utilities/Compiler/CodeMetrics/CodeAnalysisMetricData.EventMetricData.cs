// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeMetrics
{
    public abstract partial class CodeAnalysisMetricData
    {
        private sealed class EventMetricData : CodeAnalysisMetricData
        {
            internal EventMetricData(
                IEventSymbol symbol,
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

            internal static EventMetricData Compute(IEventSymbol @event, CodeMetricsAnalysisContext context)
            {
                var coupledTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
                ImmutableArray<SyntaxReference> declarations = @event.DeclaringSyntaxReferences;
                long linesOfCode = MetricsHelper.GetLinesOfCode(declarations, @event, context);
                (int cyclomaticComplexity, ComputationalComplexityMetrics computationalComplexityMetrics) =
                    MetricsHelper.ComputeCoupledTypesAndComplexityExcludingMemberDecls(declarations, @event, coupledTypesBuilder, context);
                MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, context.WellKnownTypeProvider, @event.Type);

                ImmutableArray<CodeAnalysisMetricData> children = ComputeSynchronously(GetAccessors(@event), context);
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
                MetricsHelper.RemoveContainingTypes(@event, coupledTypesBuilder);

                return new EventMetricData(@event, maintainabilityIndex, computationalComplexityMetrics,
                    coupledTypesBuilder.ToImmutable(), linesOfCode, cyclomaticComplexity, depthOfInheritance, children);
            }

            private static IEnumerable<IMethodSymbol> GetAccessors(IEventSymbol @event)
            {
                if (@event.AddMethod != null)
                {
                    yield return @event.AddMethod;
                }

                if (@event.RemoveMethod != null)
                {
                    yield return @event.RemoveMethod;
                }

                if (@event.RaiseMethod != null)
                {
                    yield return @event.RaiseMethod;
                }
            }
        }
    }
}
