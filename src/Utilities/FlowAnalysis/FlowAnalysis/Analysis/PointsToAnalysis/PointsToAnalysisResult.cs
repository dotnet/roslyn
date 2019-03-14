// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    /// <summary>
    /// Analysis result from execution of <see cref="PointsToAnalysis"/> on a control flow graph.
    /// </summary>
    public sealed class PointsToAnalysisResult : DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>
    {
        private readonly ImmutableDictionary<IOperation, ImmutableHashSet<AbstractLocation>> _escapedLocationsThroughOperationsMap;
        private readonly ImmutableDictionary<AnalysisEntity, ImmutableHashSet<AbstractLocation>> _escapedLocationsThroughEntitiesMap;

        internal PointsToAnalysisResult(
            DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> corePointsToAnalysisResult,
            ImmutableDictionary<IOperation, ImmutableHashSet<AbstractLocation>> escapedLocationsThroughOperationsMap,
            ImmutableDictionary<AnalysisEntity, ImmutableHashSet<AbstractLocation>> escapedLocationsThroughEntitiesMap)
            : base(corePointsToAnalysisResult)
        {
            _escapedLocationsThroughOperationsMap = escapedLocationsThroughOperationsMap;
            _escapedLocationsThroughEntitiesMap = escapedLocationsThroughEntitiesMap;
        }

        public ImmutableHashSet<AbstractLocation> GetEscapedAbstractLocations(IOperation operation)
            => GetEscapedAbstractLocations(operation, _escapedLocationsThroughOperationsMap);

        public ImmutableHashSet<AbstractLocation> GetEscapedAbstractLocations(AnalysisEntity analysisEntity)
            => GetEscapedAbstractLocations(analysisEntity, _escapedLocationsThroughEntitiesMap);

        private static ImmutableHashSet<AbstractLocation> GetEscapedAbstractLocations<TKey>(
            TKey key,
            ImmutableDictionary<TKey, ImmutableHashSet<AbstractLocation>> map)
            where TKey : class
        {
            if (map.TryGetValue(key, out var escapedLocations))
            {
                return escapedLocations;
            }

            return ImmutableHashSet<AbstractLocation>.Empty;
        }
    }
}
