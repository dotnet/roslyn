// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Analyzer.Utilities.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    /// <summary>
    /// Stores all the entities that got tracked during points to analysis
    /// </summary>
    internal sealed class TrackedEntitiesBuilder : IDisposable
    {
        public TrackedEntitiesBuilder()
        {
            AllEntities = PooledHashSet<AnalysisEntity>.GetInstance();
        }

        public PooledHashSet<AnalysisEntity> AllEntities { get; }

        public void Dispose()
        {
            AllEntities.Free();
        }
    }
}
