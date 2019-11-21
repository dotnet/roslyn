// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    /// <summary>
    /// Stores all the entities that got tracked during points to analysis
    /// </summary>
    internal sealed class TrackedEntitiesBuilder : IDisposable
    {
        /// <summary>
        /// Stores all the tracked entities.
        /// NOTE: Entities added to this set should not be removed.
        /// </summary>
        private PooledHashSet<AnalysisEntity> _allEntities { get; }

        public TrackedEntitiesBuilder()
        {
            _allEntities = PooledHashSet<AnalysisEntity>.GetInstance();
        }

        public void Dispose()
        {
            _allEntities.Free();
        }

        public void AddEntity(AnalysisEntity analysisEntity)
            => _allEntities.Add(analysisEntity);

        public IEnumerable<AnalysisEntity> EnumerateEntities()
            => _allEntities;

        public ImmutableHashSet<AnalysisEntity> ToImmutable()
            => _allEntities.ToImmutableHashSet();
    }
}
