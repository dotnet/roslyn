// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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

            internal static async Task<NamedTypeMetricData> ComputeAsync(INamedTypeSymbol namedType, SemanticModelProvider semanticModelProvider, CancellationToken cancellationToken)
            {
                var coupledTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
                ImmutableArray<SyntaxReference> declarations = namedType.DeclaringSyntaxReferences;
                (int cyclomaticComplexity, ComputationalComplexityMetrics computationalComplexityMetrics) =
                    await MetricsHelper.ComputeCoupledTypesAndComplexityExcludingMemberDeclsAsync(declarations, namedType, coupledTypesBuilder, semanticModelProvider, cancellationToken).ConfigureAwait(false);

                // Compat: Filter out nested types as they are children of most closest containing namespace.
                var members = namedType.GetMembers().Where(m => m.Kind != SymbolKind.NamedType);

#if LEGACY_CODE_METRICS_MODE
                // Legacy mode skips metrics for field/property/event symbols, and explicitly includes accessors as methods.
                members = members.Where(m => m.Kind != SymbolKind.Field && m.Kind != SymbolKind.Property && m.Kind != SymbolKind.Event);
#else
                // Filter out accessors as they are children of their associated symbols, for which we generate a separate node.
                members = members.Where(m => m.Kind != SymbolKind.Method || ((IMethodSymbol)m).AssociatedSymbol == null);
#endif

                ImmutableArray<CodeAnalysisMetricData> children = await ComputeAsync(members, semanticModelProvider, cancellationToken).ConfigureAwait(false);

                // Heuristic to prevent simple fields (no initializer or simple initializer) from skewing the complexity.
                ImmutableHashSet<IFieldSymbol> filteredFieldsForComplexity = getFilteredFieldsForComplexity();

                int effectiveChildrenCountForComplexity = 0;
                int singleEffectiveChildMaintainabilityIndex = -1;
                foreach (CodeAnalysisMetricData child in children)
                {
                    MetricsHelper.AddCoupledNamedTypes(coupledTypesBuilder, child.CoupledNamedTypes);

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

                int depthOfInheritance = CalculateDepthOfInheritance(namedType);
                long linesOfCode = await MetricsHelper.GetLinesOfCodeAsync(declarations, namedType, semanticModelProvider, cancellationToken).ConfigureAwait(false);
                int maintainabilityIndex = singleEffectiveChildMaintainabilityIndex != -1 ?
                    singleEffectiveChildMaintainabilityIndex :
                    CalculateMaintainabilityIndex(computationalComplexityMetrics, cyclomaticComplexity, effectiveChildrenCountForComplexity);
                MetricsHelper.RemoveContainingTypes(namedType, coupledTypesBuilder);

                return new NamedTypeMetricData(namedType, maintainabilityIndex, computationalComplexityMetrics,
                    coupledTypesBuilder.ToImmutable(), linesOfCode, cyclomaticComplexity, depthOfInheritance, children);

                ImmutableHashSet<IFieldSymbol> getFilteredFieldsForComplexity()
                {
                    ImmutableHashSet<IFieldSymbol>.Builder builderOpt = null;
                    var orderedFieldDatas = children.Where(c => c.Symbol.Kind == SymbolKind.Field).OrderBy(c => c.MaintainabilityIndex);
                    var indexThreshold = 99;
                    foreach (CodeAnalysisMetricData fieldData in orderedFieldDatas)
                    {
                        if (fieldData.MaintainabilityIndex > indexThreshold)
                        {
                            break;
                        }

                        builderOpt ??= ImmutableHashSet.CreateBuilder<IFieldSymbol>();
                        builderOpt.Add((IFieldSymbol)fieldData.Symbol);
                        indexThreshold -= 4;
                    }

                    return builderOpt?.ToImmutable() ?? ImmutableHashSet<IFieldSymbol>.Empty;
                }
            }

            private static int CalculateDepthOfInheritance(INamedTypeSymbol namedType)
            {
                switch (namedType.TypeKind)
                {
                    case TypeKind.Class:
                    case TypeKind.Interface:
                        int depth = 0;
                        for (; namedType.BaseType != null; namedType = namedType.BaseType)
                        {
                            depth++;
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
                    avgEffectiveLinesOfCode = computationalComplexityMetrics.EffectiveLinesOfCode / effectiveChildrenCount;
                    avgCyclomaticComplexity = cyclomaticComplexity / effectiveChildrenCount;
                }

                double logAvgComputationalComplexityVolume = Math.Max(0.0, Math.Log(avgComputationalComplexityVolume));   //avoid Log(0) = -Infinity
                double logAvgLinesOfCode = Math.Max(0.0, Math.Log(avgEffectiveLinesOfCode));          //avoid Log(0) = -Infinity
                return MetricsHelper.NormalizeAndRoundMaintainabilityIndex(171 - 5.2 * logAvgComputationalComplexityVolume - 0.23 * avgCyclomaticComplexity - 16.2 * logAvgLinesOfCode);
            }
        }
    }
}

#endif
