// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    /// <summary>
    /// Stores all the <see cref="AnalysisEntity"/>s and <see cref="PointsToAbstractValue"/>s that got tracked during points to analysis
    /// </summary>
    internal sealed class TrackedEntitiesBuilder : IDisposable
    {
        /// <summary>
        /// Stores all the tracked entities.
        /// NOTE: Entities added to this set should not be removed.
        /// </summary>
        private PooledHashSet<AnalysisEntity> _allEntities { get; }

        /// <summary>
        /// Stores all the tracked <see cref="PointsToAbstractValue"/> that some entity from <see cref="_allEntities"/>
        /// points to during points to analysis.
        /// NOTE: Values added to this set should not be removed.
        /// </summary>
        private PooledHashSet<PointsToAbstractValue> _pointsToValues { get; }

        public TrackedEntitiesBuilder()
        {
            _allEntities = PooledHashSet<AnalysisEntity>.GetInstance();
            _pointsToValues = PooledHashSet<PointsToAbstractValue>.GetInstance();
        }

        public void Dispose()
        {
            _allEntities.Free();
            _pointsToValues.Free();
        }

        public void AddEntityAndPointsToValue(AnalysisEntity analysisEntity, PointsToAbstractValue value)
        {
            _allEntities.Add(analysisEntity);
            AddTrackedPointsToValue(value);
        }

        public void AddTrackedPointsToValue(PointsToAbstractValue value)
            => _pointsToValues.Add(value);

        public IEnumerable<AnalysisEntity> EnumerateEntities()
            => _allEntities;

        public bool IsTrackedPointsToValue(PointsToAbstractValue value)
            => _pointsToValues.Contains(value);

        public (ImmutableHashSet<AnalysisEntity>, ImmutableHashSet<PointsToAbstractValue>) ToImmutable()
            => (_allEntities.ToImmutable(), _pointsToValues.ToImmutable());
    }
}
