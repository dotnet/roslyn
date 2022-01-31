// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Analyzer.Utilities;

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

            internal static async Task<EventMetricData> ComputeAsync(IEventSymbol @event, CodeMetricsAnalysisContext context)
            {
                var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(context.Compilation);

                var coupledTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
                ImmutableArray<SyntaxReference> declarations = @event.DeclaringSyntaxReferences;
                long linesOfCode = await MetricsHelper.GetLinesOfCodeAsync(declarations, @event, context).ConfigureAwait(false);
                (int cyclomaticComplexity, ComputationalComplexityMetrics computationalComplexityMetrics) =
                    await MetricsHelper.ComputeCoupledTypesAndComplexityExcludingMemberDeclsAsync(declarations, @event, coupledTypesBuilder, context).ConfigureAwait(false);
                MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, wellKnownTypeProvider, @event.Type);

                ImmutableArray<CodeAnalysisMetricData> children = await ComputeAsync(GetAccessors(@event), context).ConfigureAwait(false);
                int maintainabilityIndexTotal = 0;
                foreach (CodeAnalysisMetricData child in children)
                {
                    MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, wellKnownTypeProvider, child.CoupledNamedTypes);
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

#endif
