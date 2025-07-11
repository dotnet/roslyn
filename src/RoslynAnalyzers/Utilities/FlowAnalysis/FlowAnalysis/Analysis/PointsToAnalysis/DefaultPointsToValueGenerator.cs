// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    /// <summary>
    /// Generates and stores the default <see cref="PointsToAbstractValue"/> for <see cref="AnalysisEntity"/> instances generated for member and element reference operations.
    /// </summary>
    internal sealed class DefaultPointsToValueGenerator
    {
        private readonly TrackedEntitiesBuilder _trackedEntitiesBuilder;
        private readonly ImmutableDictionary<AnalysisEntity, PointsToAbstractValue>.Builder _defaultPointsToValueMapBuilder;

        public DefaultPointsToValueGenerator(TrackedEntitiesBuilder trackedEntitiesBuilder)
        {
            _trackedEntitiesBuilder = trackedEntitiesBuilder;
            _defaultPointsToValueMapBuilder = ImmutableDictionary.CreateBuilder<AnalysisEntity, PointsToAbstractValue>();
        }

        public PointsToAnalysisKind PointsToAnalysisKind => _trackedEntitiesBuilder.PointsToAnalysisKind;

        public PointsToAbstractValue GetOrCreateDefaultValue(AnalysisEntity analysisEntity)
        {
            if (!_defaultPointsToValueMapBuilder.TryGetValue(analysisEntity, out var value))
            {
                if (analysisEntity.Symbol?.Kind == SymbolKind.Local ||
                    analysisEntity.Symbol is IParameterSymbol parameter && parameter.RefKind == RefKind.Out ||
                    analysisEntity.CaptureId != null)
                {
                    return PointsToAbstractValue.Undefined;
                }
                else if (analysisEntity.Type.IsNonNullableValueType())
                {
                    return PointsToAbstractValue.NoLocation;
                }
                else if (analysisEntity.HasUnknownInstanceLocation)
                {
                    return PointsToAbstractValue.Unknown;
                }

                value = PointsToAbstractValue.Create(AbstractLocation.CreateAnalysisEntityDefaultLocation(analysisEntity), mayBeNull: true);

                // PERF: Do not track entity and its points to value for partial analysis for entities requiring complete analysis.
                if (analysisEntity.ShouldBeTrackedForPointsToAnalysis(PointsToAnalysisKind))
                {
                    _trackedEntitiesBuilder.AddEntityAndPointsToValue(analysisEntity, value);
                    _defaultPointsToValueMapBuilder.Add(analysisEntity, value);
                }
            }

            return value;
        }

        public bool IsTrackedEntity(AnalysisEntity analysisEntity) => _defaultPointsToValueMapBuilder.ContainsKey(analysisEntity);
        public bool IsTrackedPointsToValue(PointsToAbstractValue value) => _trackedEntitiesBuilder.IsTrackedPointsToValue(value);
        public void AddTrackedPointsToValue(PointsToAbstractValue value) => _trackedEntitiesBuilder.AddTrackedPointsToValue(value);
        public bool HasAnyTrackedEntity => _defaultPointsToValueMapBuilder.Count > 0;
    }
}
