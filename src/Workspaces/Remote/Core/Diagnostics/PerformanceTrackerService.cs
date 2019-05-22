// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    /// <summary>
    /// Track diagnostic performance 
    /// </summary>
    [ExportWorkspaceService(typeof(IPerformanceTrackerService), WorkspaceKind.Host), Shared]
    internal class PerformanceTrackerService : IPerformanceTrackerService
    {
        private static readonly Func<IEnumerable<AnalyzerPerformanceInfo>, int, string> s_snapshotLogger = SnapshotLogger;

        private const double DefaultMinLOFValue = 20;
        private const double DefaultAverageThreshold = 100;
        private const double DefaultStddevThreshold = 100;

        private const int SampleSize = 300;
        private const double K_Value_Ratio = 2D / 3D;

        private readonly double _minLOFValue;
        private readonly double _averageThreshold;
        private readonly double _stddevThreshold;

        private readonly object _gate;
        private readonly PerformanceQueue _queue;
        private readonly ConcurrentDictionary<string, bool> _builtInMap = new ConcurrentDictionary<string, bool>(concurrencyLevel: 2, capacity: 10);

        public event EventHandler SnapshotAdded;

        [ImportingConstructor]
        public PerformanceTrackerService() :
            this(DefaultMinLOFValue, DefaultAverageThreshold, DefaultStddevThreshold)
        {
        }

        // internal for testing
        internal PerformanceTrackerService(double minLOFValue, double averageThreshold, double stddevThreshold)
        {
            _minLOFValue = minLOFValue;
            _averageThreshold = averageThreshold;
            _stddevThreshold = stddevThreshold;

            _gate = new object();
            _queue = new PerformanceQueue(SampleSize);
        }

        public void AddSnapshot(IEnumerable<AnalyzerPerformanceInfo> snapshot, int unitCount)
        {
            Logger.Log(FunctionId.PerformanceTrackerService_AddSnapshot, s_snapshotLogger, snapshot, unitCount);

            RecordBuiltInAnalyzers(snapshot);

            lock (_gate)
            {
                _queue.Add(snapshot.Select(entry => (entry.AnalyzerId, entry.TimeSpan)), unitCount);
            }

            OnSnapshotAdded();
        }

        public void GenerateReport(List<ExpensiveAnalyzerInfo> badAnalyzers)
        {
            using var pooledRaw = SharedPools.Default<Dictionary<string, (double average, double stddev)>>().GetPooledObject();

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

            using var generator = new ReportGenerator(this, _minLOFValue, _averageThreshold, _stddevThreshold, badAnalyzers);
            generator.Report(rawPerformanceData);
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

        private static string SnapshotLogger(IEnumerable<AnalyzerPerformanceInfo> snapshots, int unitCount)
        {
            using var pooledObject = SharedPools.Default<StringBuilder>().GetPooledObject();
            var sb = pooledObject.Object;

            sb.Append(unitCount);

            foreach (var snapshot in snapshots)
            {
                sb.Append("|");
                sb.Append(snapshot.AnalyzerId);
                sb.Append(":");
                sb.Append(snapshot.BuiltIn);
                sb.Append(":");
                sb.Append(snapshot.TimeSpan.TotalMilliseconds);
            }

            sb.Append("*");

            return sb.ToString();
        }

        private sealed class ReportGenerator : IDisposable, IComparer<ExpensiveAnalyzerInfo>
        {
            private readonly double _minLOFValue;
            private readonly double _averageThreshold;
            private readonly double _stddevThreshold;

            private readonly PerformanceTrackerService _owner;
            private readonly List<ExpensiveAnalyzerInfo> _badAnalyzers;
            private readonly PooledObject<List<IDisposable>> _pooledObjects;

            public ReportGenerator(
                PerformanceTrackerService owner,
                double minLOFValue,
                double averageThreshold,
                double stddevThreshold,
                List<ExpensiveAnalyzerInfo> badAnalyzers)
            {
                _pooledObjects = SharedPools.Default<List<IDisposable>>().GetPooledObject();

                _owner = owner;

                _minLOFValue = minLOFValue;
                _averageThreshold = averageThreshold;
                _stddevThreshold = stddevThreshold;

                _badAnalyzers = badAnalyzers;
            }

            public void Report(Dictionary<string, (double average, double stddev)> rawPerformanceData)
            {
                // this is implementation of local outlier factor (LOF)
                // see the wiki (https://en.wikipedia.org/wiki/Local_outlier_factor) for more information 

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

                    // if result performance is lower than our threshold, don't need to calculate
                    // LOF value for the analyzer
                    var (average, stddev) = rawPerformanceData[analyzerId];
                    if (average <= _averageThreshold && stddev <= _stddevThreshold)
                    {
                        continue;
                    }

                    // possible bad analyzer, calculate LOF
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
                    _badAnalyzers.Add(new ExpensiveAnalyzerInfo(_owner.AllowTelemetry(analyzerId), analyzerId, lof_value.Value, average, stddev));
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

                return (lrdb / rowKNeighborsIndices.Count) / lrda;
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

                var distanceSum = 0.0;
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

            private List<double> GetKDistances(List<List<double>> allDistances, int kValue)
            {
                var analyzerCount = allDistances.Count;
                var kDistances = GetPooledListAndSetCapacity<double>(analyzerCount);
                var sortedRowDistance = GetPooledList<double>();

                for (var index = 0; index < analyzerCount; index++)
                {
                    sortedRowDistance.Clear();
                    sortedRowDistance.AddRange(allDistances[index]);

                    sortedRowDistance.Sort();

                    kDistances[index] = sortedRowDistance[kValue];
                }

                return kDistances;
            }

            private List<List<double>> GetAllDistances(List<(double normalizedAverage, double normalizedStddev)> normalizedMap)
            {
                var analyzerCount = normalizedMap.Count;
                var allDistances = GetPooledListAndSetCapacity<List<double>>(analyzerCount);

                for (var rowIndex = 0; rowIndex < analyzerCount; rowIndex++)
                {
                    var rowDistances = GetPooledListAndSetCapacity<double>(analyzerCount);
                    var (normaliedAverage, normalizedStddev) = normalizedMap[rowIndex];

                    for (var colIndex = 0; colIndex < analyzerCount; colIndex++)
                    {
                        var colAnalyzer = normalizedMap[colIndex];
                        var distance = Math.Sqrt(Math.Pow(colAnalyzer.normalizedAverage - normaliedAverage, 2) +
                                                 Math.Pow(colAnalyzer.normalizedStddev - normalizedStddev, 2));

                        rowDistances[colIndex] = distance;
                    }

                    allDistances[rowIndex] = rowDistances;
                }

                return allDistances;
            }

            private List<(double normaliedAverage, double normalizedStddev)> GetNormalizedPerformanceMap(
                List<string> analyzerIdIndex, Dictionary<string, (double average, double stddev)> rawPerformanceData)
            {
                var averageMin = rawPerformanceData.Values.Select(kv => kv.average).Min();
                var averageMax = rawPerformanceData.Values.Select(kv => kv.average).Max();
                var averageDelta = averageMax - averageMin;

                var stddevMin = rawPerformanceData.Values.Select(kv => kv.stddev).Min();
                var stddevMax = rawPerformanceData.Values.Select(kv => kv.stddev).Max();
                var stddevDelta = stddevMax - stddevMin;

                // make sure delta is not 0
                averageDelta = averageDelta == 0 ? 1 : averageDelta;
                stddevDelta = stddevDelta == 0 ? 1 : stddevDelta;

                // calculate normalized average and stddev and convert analyzerId string to index
                var analyzerCount = analyzerIdIndex.Count;
                var normalizedMap = GetPooledListAndSetCapacity<(double normalizedAverage, double normalizedStddev)>(analyzerCount);

                for (var index = 0; index < analyzerCount; index++)
                {
                    var (average, stddev) = rawPerformanceData[analyzerIdIndex[index]];
                    var normalizedAverage = (average - averageMin) / averageDelta;
                    var normalizedStddev = (stddev - stddevMin) / stddevDelta;

                    normalizedMap[index] = (normalizedAverage, normalizedStddev);
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
                    pooledObject.Object.Add(default);
                }

                return pooledObject.Object;
            }

            public int Compare(ExpensiveAnalyzerInfo x, ExpensiveAnalyzerInfo y)
            {
                if (x.LocalOutlierFactor == y.LocalOutlierFactor)
                {
                    return 0;
                }

                // want reversed order
                if (x.LocalOutlierFactor - y.LocalOutlierFactor > 0)
                {
                    return -1;
                }

                return 1;
            }
        }
    }
}
