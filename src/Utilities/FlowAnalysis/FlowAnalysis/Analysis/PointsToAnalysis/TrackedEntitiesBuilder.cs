// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
        private PooledHashSet<AnalysisEntity> AllEntities { get; }

        /// <summary>
        /// Stores all the tracked <see cref="PointsToAbstractValue"/> that some entity from <see cref="AllEntities"/>
        /// points to during points to analysis.
        /// NOTE: Values added to this set should not be removed.
        /// </summary>
        private PooledHashSet<PointsToAbstractValue> PointsToValues { get; }

        public TrackedEntitiesBuilder(PointsToAnalysisKind pointsToAnalysisKind)
        {
            Debug.Assert(pointsToAnalysisKind != PointsToAnalysisKind.None);

            PointsToAnalysisKind = pointsToAnalysisKind;
            AllEntities = PooledHashSet<AnalysisEntity>.GetInstance();
            PointsToValues = PooledHashSet<PointsToAbstractValue>.GetInstance();
        }

        public PointsToAnalysisKind PointsToAnalysisKind { get; }

        public void Dispose()
        {
            AllEntities.Dispose();
            PointsToValues.Dispose();
        }

        public void AddEntityAndPointsToValue(AnalysisEntity analysisEntity, PointsToAbstractValue value)
        {
            Debug.Assert(PointsToAnalysisKind == PointsToAnalysisKind.Complete || !analysisEntity.IsChildOrInstanceMemberNeedingCompletePointsToAnalysis());

            AllEntities.Add(analysisEntity);
            AddTrackedPointsToValue(value);
        }

        public void AddTrackedPointsToValue(PointsToAbstractValue value)
            => PointsToValues.Add(value);

        public IEnumerable<AnalysisEntity> EnumerateEntities()
            => AllEntities;

        public bool IsTrackedPointsToValue(PointsToAbstractValue value)
            => PointsToValues.Contains(value);

        public (ImmutableHashSet<AnalysisEntity>, ImmutableHashSet<PointsToAbstractValue>) ToImmutable()
            => (AllEntities.ToImmutable(), PointsToValues.ToImmutable());
    }
}
