// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeMetrics
{
    public abstract partial class CodeAnalysisMetricData
    {
        private sealed class NamedTypeMetricData : CodeAnalysisMetricData
        {
            internal NamedTypeMetricData(
                INamedTypeSymbol symbol,
                int maintainabilityIndex,
                ComputationalComplexityMetrics computationalComplexityMetrics,
                ImmutableHashSet<INamedTypeSymbol> coupledNamedTypes,
                long linesOfCode,
                int cyclomaticComplexity,
                int? depthOfInheritance,
                ImmutableArray<CodeAnalysisMetricData> children)
                : base(symbol, maintainabilityIndex, computationalComplexityMetrics,
                      coupledNamedTypes, linesOfCode, cyclomaticComplexity, depthOfInheritance, children)
            {
            }

            internal static async Task<NamedTypeMetricData> ComputeAsync(INamedTypeSymbol namedType, CodeMetricsAnalysisContext context)
            {
                var members = GetMembers(namedType, context);

                ImmutableArray<CodeAnalysisMetricData> children = await ComputeAsync(members, context).ConfigureAwait(false);

                return ComputeFromChildren(namedType, children, context);
            }

            internal static NamedTypeMetricData ComputeSynchronously(INamedTypeSymbol namedType, CodeMetricsAnalysisContext context)
            {
                var members = GetMembers(namedType, context);

                ImmutableArray<CodeAnalysisMetricData> children = ComputeSynchronously(members, context);

                return ComputeFromChildren(namedType, children, context);
            }

            private static IEnumerable<ISymbol> GetMembers(INamedTypeSymbol namedType, CodeMetricsAnalysisContext context)
            {
                // Compat: Filter out nested types as they are children of most closest containing namespace.
                var members = namedType.GetMembers().Where(m => m.Kind != SymbolKind.NamedType);

#if LEGACY_CODE_METRICS_MODE
                // Legacy mode skips metrics for field/property/event symbols, and explicitly includes accessors as methods.
                members = members.Where(m => m.Kind is not SymbolKind.Field and not SymbolKind.Property and not SymbolKind.Event);
#else
                // Filter out accessors as they are children of their associated symbols, for which we generate a separate node.
                members = members.Where(m => m.Kind != SymbolKind.Method || ((IMethodSymbol)m).AssociatedSymbol == null);
#endif

                return members;
            }

            private static NamedTypeMetricData ComputeFromChildren(INamedTypeSymbol namedType, ImmutableArray<CodeAnalysisMetricData> children, CodeMetricsAnalysisContext context)
            {
                var coupledTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
                ImmutableArray<SyntaxReference> declarations = namedType.DeclaringSyntaxReferences;
                (int cyclomaticComplexity, ComputationalComplexityMetrics computationalComplexityMetrics) =
                    MetricsHelper.ComputeCoupledTypesAndComplexityExcludingMemberDecls(declarations, namedType, coupledTypesBuilder, context);

                // Heuristic to prevent simple fields (no initializer or simple initializer) from skewing the complexity.
                ImmutableHashSet<IFieldSymbol> filteredFieldsForComplexity = getFilteredFieldsForComplexity();

                int effectiveChildrenCountForComplexity = 0;
                int singleEffectiveChildMaintainabilityIndex = -1;
                foreach (CodeAnalysisMetricData child in children)
                {
                    MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, context.WellKnownTypeProvider, child.CoupledNamedTypes);

                    if (child.Symbol.Kind != SymbolKind.Field ||
                        filteredFieldsForComplexity.Contains((IFieldSymbol)child.Symbol))
                    {
                        singleEffectiveChildMaintainabilityIndex = effectiveChildrenCountForComplexity == 0 && computationalComplexityMetrics.IsDefault ?
                                child.MaintainabilityIndex :
                                -1;
                        effectiveChildrenCountForComplexity++;
                        cyclomaticComplexity += child.CyclomaticComplexity;
                        computationalComplexityMetrics = computationalComplexityMetrics.Union(child.ComputationalComplexityMetrics);
                    }
                }

                if (cyclomaticComplexity == 0 && !namedType.IsStatic)
                {
                    // Empty named type, account for implicit constructor.
                    cyclomaticComplexity = 1;
                }

                int depthOfInheritance = CalculateDepthOfInheritance(namedType, context.IsExcludedFromInheritanceCountFunc);
                long linesOfCode = MetricsHelper.GetLinesOfCode(declarations, namedType, context);
                int maintainabilityIndex = singleEffectiveChildMaintainabilityIndex != -1 ?
                    singleEffectiveChildMaintainabilityIndex :
                    CalculateMaintainabilityIndex(computationalComplexityMetrics, cyclomaticComplexity, effectiveChildrenCountForComplexity);
                MetricsHelper.RemoveContainingTypes(namedType, coupledTypesBuilder);

                return new NamedTypeMetricData(namedType, maintainabilityIndex, computationalComplexityMetrics,
                    coupledTypesBuilder.ToImmutable(), linesOfCode, cyclomaticComplexity, depthOfInheritance, children);

                ImmutableHashSet<IFieldSymbol> getFilteredFieldsForComplexity()
                {
                    ImmutableHashSet<IFieldSymbol>.Builder? builder = null;
                    var orderedFieldDatas = children.Where(c => c.Symbol.Kind == SymbolKind.Field).OrderBy(c => c.MaintainabilityIndex);
                    var indexThreshold = 99;
                    foreach (CodeAnalysisMetricData fieldData in orderedFieldDatas)
                    {
                        if (fieldData.MaintainabilityIndex > indexThreshold)
                        {
                            break;
                        }

                        builder ??= ImmutableHashSet.CreateBuilder<IFieldSymbol>();
                        builder.Add((IFieldSymbol)fieldData.Symbol);
                        indexThreshold -= 4;
                    }

                    return builder?.ToImmutable() ?? ImmutableHashSet<IFieldSymbol>.Empty;
                }
            }

            private static int CalculateDepthOfInheritance(INamedTypeSymbol namedType, Func<INamedTypeSymbol, bool> isExcludedFromInheritanceCount)
            {
                switch (namedType.TypeKind)
                {
                    case TypeKind.Class:
                    case TypeKind.Interface:
                        int depth = 0;
                        var parent = namedType.BaseType;
                        while (parent != null && !isExcludedFromInheritanceCount(parent))
                        {
                            depth++;
                            parent = parent.BaseType;
                        }

                        return depth;

                    case TypeKind.Struct:
                    case TypeKind.Enum:
                    case TypeKind.Delegate:
                        // Compat: For structs, enums and delegates, we consider the depth to be 1.
                        return 1;

                    default:
                        return 0;
                }
            }

            private static int CalculateMaintainabilityIndex(
                ComputationalComplexityMetrics computationalComplexityMetrics,
                int cyclomaticComplexity,
                int effectiveChildrenCount)
            {
                double avgComputationalComplexityVolume = 1.0;
                double avgEffectiveLinesOfCode = 0.0;
                double avgCyclomaticComplexity = 0.0;

                if (effectiveChildrenCount > 0)
                {
                    avgComputationalComplexityVolume = computationalComplexityMetrics.Volume / effectiveChildrenCount;
                    avgEffectiveLinesOfCode = (double)computationalComplexityMetrics.EffectiveLinesOfCode / effectiveChildrenCount;
                    avgCyclomaticComplexity = (double)cyclomaticComplexity / effectiveChildrenCount;
                }

                double logAvgComputationalComplexityVolume = Math.Max(0.0, Math.Log(avgComputationalComplexityVolume));   //avoid Log(0) = -Infinity
                double logAvgLinesOfCode = Math.Max(0.0, Math.Log(avgEffectiveLinesOfCode));          //avoid Log(0) = -Infinity
                return MetricsHelper.NormalizeAndRoundMaintainabilityIndex(171 - 5.2 * logAvgComputationalComplexityVolume - 0.23 * avgCyclomaticComplexity - 16.2 * logAvgLinesOfCode);
            }
        }
    }
}
