// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    /// <summary>
    /// Track diagnostic performance 
    /// </summary>
    [ExportWorkspaceService(typeof(IPerformanceTrackerService), WorkspaceKind.Host), Shared]
    internal class PerformanceTrackerService : IPerformanceTrackerService
    {
        private const double DefaultMinLOFValue = 20;
        private const double DefaultMeanThreshold = 500;
        private const double DefaultStddevThreshold = 500;

        private const int SampleSize = 300;
        private const double K_Value_Ratio = 2D / 3D;

        private readonly double _minLOFValue;
        private readonly double _meanThreshold;
        private readonly double _stddevThreshold;

        private readonly object _gate;
        private readonly PerformanceQueue _queue;
        private readonly ConcurrentDictionary<string, bool> _builtInMap = new ConcurrentDictionary<string, bool>(concurrencyLevel: 2, capacity: 10);

        public event EventHandler SnapshotAdded;

        public PerformanceTrackerService() :
            this(DefaultMinLOFValue, DefaultMeanThreshold, DefaultStddevThreshold)
        {
        }

        // internal for testing
        internal PerformanceTrackerService(double minLOFValue, double meanThreshold, double stddevThreshold)
        {
            _minLOFValue = minLOFValue;
            _meanThreshold = meanThreshold;
            _stddevThreshold = stddevThreshold;

            _gate = new object();
            _queue = new PerformanceQueue(SampleSize);
        }

        public void AddSnapshot(IEnumerable<AnalyzerPerformanceInfo> snapshot, int unitCount)
        {
            RecordBuiltInAnalyzers(snapshot);

            lock (_gate)
            {
                _queue.Add(snapshot.Select(entry => (entry.AnalyzerId, entry.TimeSpan)), unitCount);
            }

            OnSnapshotAdded();
        }

        public void GenerateReport(List<BadAnalyzerInfo> badAnalyzers)
        {
            using (var pooledRaw = SharedPools.Default<Dictionary<string, (double mean, double stddev)>>().GetPooledObject())
            {
                var rawPerformanceData = pooledRaw.Object;

                lock (_gate)
                {
                    // first get raw aggregated peformance data from the queue
                    _queue.GetPerformanceData(rawPerformanceData);
                }

                // make sure there are some data
                if (rawPerformanceData.Count == 0)
                {
                    return;
                }

                using (var generator = new ReportGenerator(this, _minLOFValue, _meanThreshold, _stddevThreshold, badAnalyzers))
                {
                    generator.Report(rawPerformanceData);
                }
            }
        }

        private void RecordBuiltInAnalyzers(IEnumerable<AnalyzerPerformanceInfo> snapshot)
        {
            foreach (var entry in snapshot)
            {
                _builtInMap[entry.AnalyzerId] = entry.BuiltIn;
            }
        }

        private bool AllowTelemetry(string analyzerId)
        {
            if (_builtInMap.TryGetValue(analyzerId, out var builtIn))
            {
                return builtIn;
            }

            return false;
        }

        private void OnSnapshotAdded()
        {
            SnapshotAdded?.Invoke(this, EventArgs.Empty);
        }

        private sealed class ReportGenerator : IDisposable, IComparer<BadAnalyzerInfo>
        {
            private readonly double _minLOFValue;
            private readonly double _meanThreshold;
            private readonly double _stddevThreshold;

            private readonly PerformanceTrackerService _owner;
            private readonly List<BadAnalyzerInfo> _badAnalyzers;
            private readonly PooledObject<List<IDisposable>> _pooledObjects;

            public ReportGenerator(
                PerformanceTrackerService owner,
                double minLOFValue,
                double meanThreshold,
                double stddevThreshold,
                List<BadAnalyzerInfo> badAnalyzers)
            {
                _pooledObjects = SharedPools.Default<List<IDisposable>>().GetPooledObject();

                _owner = owner;

                _minLOFValue = minLOFValue;
                _meanThreshold = meanThreshold;
                _stddevThreshold = stddevThreshold;

                _badAnalyzers = badAnalyzers;
            }

            public void Report(Dictionary<string, (double mean, double stddev)> rawPerformanceData)
            {
                // this is implementation of  https://en.wikipedia.org/wiki/Local_outlier_factor
                // see the wiki for more information

                // convert string (analyzerId) to index
                var analyzerIdIndex = GetAnalyzerIdIndex(rawPerformanceData.Keys);

                // now calculate normalized value per analyzer
                var normalizedMap = GetNormalizedPerformanceMap(analyzerIdIndex, rawPerformanceData);

                // get k value
                var k_value = (int)(rawPerformanceData.Count * K_Value_Ratio);

                // calculate distances

                // calculate all distance first
                var allDistances = GetAllDistances(normalizedMap);

                // find k distance from all distances
                var kDistances = GetKDistances(allDistances, k_value);

                // find k nearest neighbors
                var kNeighborIndices = GetKNeighborIndices(allDistances, kDistances);

                var analyzerCount = kNeighborIndices.Count;
                for (var index = 0; index < analyzerCount; index++)
                {
                    var analyzerId = analyzerIdIndex[index];

                    // if result performance is lower than our threshold, don't need to calcuate
                    // LOF value for the analyzer
                    var rawData = rawPerformanceData[analyzerId];
                    if (rawData.mean <= _meanThreshold && rawData.stddev <= _stddevThreshold)
                    {
                        continue;
                    }

                    // possible bad analyzer, calcuate LOF
                    var lof_value = TryGetLocalOutlierFactor(allDistances, kNeighborIndices, kDistances, index);
                    if (!lof_value.HasValue)
                    {
                        // this analyzer doesn't have lof value
                        continue;
                    }

                    if (lof_value <= _minLOFValue)
                    {
                        // this doesn't stand out from other analyzers
                        continue;
                    }

                    // report found possible bad analyzers
                    _badAnalyzers.Add(new BadAnalyzerInfo(_owner.AllowTelemetry(analyzerId), analyzerId, lof_value.Value, rawData.mean, rawData.stddev));
                }

                _badAnalyzers.Sort(this);
            }

            private double? TryGetLocalOutlierFactor(
                List<List<double>> allDistances, List<List<int>> kNeighborIndices, List<double> kDistances, int analyzerIndex)
            {
                var rowKNeighborsIndices = kNeighborIndices[analyzerIndex];
                if (rowKNeighborsIndices.Count == 0)
                {
                    // nothing to calculate if there is no neighbor to compare
                    return null;
                }

                var lrda = TryGetLocalReachabilityDensity(allDistances, kNeighborIndices, kDistances, analyzerIndex);
                if (!lrda.HasValue)
                {
                    // can't calculate reachability for the analyzer. can't calculate lof for this analyzer
                    return null;
                }

                var lrdb = 0D;
                foreach (var neighborIndex in rowKNeighborsIndices)
                {
                    var reachability = TryGetLocalReachabilityDensity(allDistances, kNeighborIndices, kDistances, neighborIndex);
                    if (!reachability.HasValue)
                    {
                        // this neighbor analyzer doesn't have its own neighbor. skip it
                        continue;
                    }

                    lrdb += reachability.Value;
                }

                return lrdb / rowKNeighborsIndices.Count / lrda;
            }

            private double GetReachabilityDistance(
                List<List<double>> allDistances, List<double> kDistances, int analyzerIndex1, int analyzerIndex2)
            {
                return Math.Max(allDistances[analyzerIndex1][analyzerIndex2], kDistances[analyzerIndex2]);
            }

            private double? TryGetLocalReachabilityDensity(
                List<List<double>> allDistances, List<List<int>> kNeighborIndices, List<double> kDistances, int analyzerIndex)
            {
                var rowKNeighborsIndices = kNeighborIndices[analyzerIndex];
                if (rowKNeighborsIndices.Count == 0)
                {
                    // no neighbor to get reachability
                    return null;
                }

                var distanceSum = 0D;
                foreach (var neighborIndex in rowKNeighborsIndices)
                {
                    distanceSum += GetReachabilityDistance(allDistances, kDistances, analyzerIndex, neighborIndex);
                }

                return 1 / distanceSum / rowKNeighborsIndices.Count;
            }

            private List<List<int>> GetKNeighborIndices(List<List<double>> allDistances, List<double> kDistances)
            {
                var analyzerCount = kDistances.Count;
                var kNeighborIndices = GetPooledListAndSetCapacity<List<int>>(analyzerCount);

                for (var rowIndex = 0; rowIndex < analyzerCount; rowIndex++)
                {
                    var rowKNeighborIndices = GetPooledList<int>();

                    var rowDistances = allDistances[rowIndex];
                    var kDistance = kDistances[rowIndex];

                    for (var colIndex = 0; colIndex < analyzerCount; colIndex++)
                    {
                        var value = rowDistances[colIndex];

                        // get neighbors closer than k distance
                        if (value > 0 && value <= kDistance)
                        {
                            rowKNeighborIndices.Add(colIndex);
                        }
                    }

                    kNeighborIndices[rowIndex] = rowKNeighborIndices;
                }

                return kNeighborIndices;
            }

            private List<double> GetKDistances(List<List<double>> allDistances, int k_value)
            {
                var analyzerCount = allDistances.Count;
                var kDistances = GetPooledListAndSetCapacity<double>(analyzerCount);
                var sortedRowDistance = GetPooledList<double>();

                for (var index = 0; index < analyzerCount; index++)
                {
                    sortedRowDistance.Clear();
                    sortedRowDistance.AddRange(allDistances[index]);

                    sortedRowDistance.Sort();

                    kDistances[index] = sortedRowDistance[k_value];
                }

                return kDistances;
            }

            private List<List<double>> GetAllDistances(List<(double normaliedMean, double normalizedStddev)> normalizedMap)
            {
                var analyzerCount = normalizedMap.Count;
                var allDistances = GetPooledListAndSetCapacity<List<double>>(analyzerCount);

                for (var rowIndex = 0; rowIndex < analyzerCount; rowIndex++)
                {
                    var rowDistances = GetPooledListAndSetCapacity<double>(analyzerCount);
                    var rowAnalyzer = normalizedMap[rowIndex];

                    for (var colIndex = 0; colIndex < analyzerCount; colIndex++)
                    {
                        var colAnalyzer = normalizedMap[colIndex];
                        var distance = Math.Sqrt(Math.Pow(colAnalyzer.normaliedMean - rowAnalyzer.normaliedMean, 2) +
                                                 Math.Pow(colAnalyzer.normalizedStddev - rowAnalyzer.normalizedStddev, 2));

                        rowDistances[colIndex] = distance;
                    }

                    allDistances[rowIndex] = rowDistances;
                }

                return allDistances;
            }

            private List<(double normaliedMean, double normalizedStddev)> GetNormalizedPerformanceMap(
                List<string> analyzerIdIndex, Dictionary<string, (double mean, double stddev)> rawPerformanceData)
            {
                var (meanMin, meanMax) = (rawPerformanceData.Values.Select(kv => kv.mean).Min(), rawPerformanceData.Values.Select(kv => kv.mean).Max());
                var meanDelta = meanMax - meanMin;

                var (stddevMin, stddevMax) = (rawPerformanceData.Values.Select(kv => kv.stddev).Min(), rawPerformanceData.Values.Select(kv => kv.stddev).Max());
                var stddevDelta = stddevMax - stddevMin;

                // make sure delta is not 0
                meanDelta = meanDelta == 0 ? 1 : meanDelta;
                stddevDelta = stddevDelta == 0 ? 1 : stddevDelta;

                // calculate normalized mean and stddev and convert analyzerId string to index
                var analyzerCount = analyzerIdIndex.Count;
                var normalizedMap = GetPooledListAndSetCapacity<(double normaliedMean, double normalizedStddev)>(analyzerCount);

                for (var index = 0; index < analyzerCount; index++)
                {
                    var value = rawPerformanceData[analyzerIdIndex[index]];
                    var normalizedMean = (value.mean - meanMin) / meanDelta;
                    var normalizedStddev = (value.stddev - stddevMin) / stddevDelta;

                    normalizedMap[index] = (normalizedMean, normalizedStddev);
                }

                return normalizedMap;
            }

            private List<string> GetAnalyzerIdIndex(IEnumerable<string> analyzerIds)
            {
                var analyzerIdIndex = GetPooledList<string>();
                analyzerIdIndex.AddRange(analyzerIds);

                return analyzerIdIndex;
            }

            public void Dispose()
            {
                foreach (var disposable in _pooledObjects.Object)
                {
                    disposable.Dispose();
                }

                _pooledObjects.Dispose();
            }

            private List<T> GetPooledList<T>()
            {
                var pooledObject = SharedPools.Default<List<T>>().GetPooledObject();
                _pooledObjects.Object.Add(pooledObject);

                return pooledObject.Object;
            }

            private List<T> GetPooledListAndSetCapacity<T>(int capacity)
            {
                var pooledObject = SharedPools.Default<List<T>>().GetPooledObject();
                _pooledObjects.Object.Add(pooledObject);

                for (var i = 0; i < capacity; i++)
                {
                    pooledObject.Object.Add(default(T));
                }

                return pooledObject.Object;
            }

            public int Compare(BadAnalyzerInfo x, BadAnalyzerInfo y)
            {
                if (x.LOF == y.LOF)
                {
                    return 0;
                }

                // want reversed order
                if (x.LOF - y.LOF > 0)
                {
                    return -1;
                }

                return 1;
            }
        }
    }
}
