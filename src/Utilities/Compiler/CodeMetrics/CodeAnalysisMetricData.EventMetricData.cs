// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

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

            internal async static Task<EventMetricData> ComputeAsync(IEventSymbol @event, SemanticModelProvider semanticModelProvider, CancellationToken cancellationToken)
            {
                var coupledTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
                ImmutableArray<SyntaxReference> declarations = @event.DeclaringSyntaxReferences;
                long linesOfCode = await MetricsHelper.GetLinesOfCodeAsync(declarations, @event, semanticModelProvider, cancellationToken).ConfigureAwait(false);
                (int cyclomaticComplexity, ComputationalComplexityMetrics computationalComplexityMetrics) =
                    await MetricsHelper.ComputeCoupledTypesAndComplexityExcludingMemberDeclsAsync(declarations, @event, coupledTypesBuilder, semanticModelProvider, cancellationToken).ConfigureAwait(false);
                MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, @event.Type);

                ImmutableArray<CodeAnalysisMetricData> children = await ComputeAsync(GetAccessors(@event), semanticModelProvider, cancellationToken).ConfigureAwait(false);
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
