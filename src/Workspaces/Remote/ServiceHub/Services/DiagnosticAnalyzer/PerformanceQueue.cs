// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics;

/// <summary>
/// This queue hold onto raw performance data. this type itself is not thread safe. the one who uses this type
/// should take care of that.
/// </summary>
/// <threadsafety static="false" instance="false"/>
internal class PerformanceQueue
{
    private readonly int _maxSampleSize, _minSampleSize;
    private readonly LinkedList<Snapshot> _snapshots;

    private int _snapshotsSinceLastReport;

    public PerformanceQueue(int minSampleSize)
    {
        // We allow at most 3 times the number of samples in the queue and
        // use sliding window algorithm to choose the latest 'minSampleSize' samples.
        _maxSampleSize = minSampleSize * 3;

        _minSampleSize = minSampleSize;
        _snapshots = new LinkedList<Snapshot>();
        _snapshotsSinceLastReport = 0;
    }

    public int Count => _snapshots.Count;
    public void Add(IEnumerable<(string analyzerId, TimeSpan timeSpan)> rawData, int unitCount)
    {
        if (_snapshots.Count < _maxSampleSize)
        {
            _snapshots.AddLast(new Snapshot(rawData, unitCount));
        }
        else
        {
            // remove the first one
            var first = _snapshots.First;
            _snapshots.RemoveFirst();

            // update data to new data and put it back
            first.Value.Update(rawData, unitCount);
            _snapshots.AddLast(first);
        }

        _snapshotsSinceLastReport++;
    }

    public void GetPerformanceData(List<(string analyzerId, double average, double stddev)> aggregatedPerformanceDataPerAnalyzer)
    {
        if (_snapshotsSinceLastReport < _minSampleSize)
        {
            // we don't have enough data to report this
            return;
        }

        _snapshotsSinceLastReport = 0;

        using var pooledMap = SharedPools.Default<Dictionary<int, string>>().GetPooledObject();
        using var pooledSet = SharedPools.Default<HashSet<int>>().GetPooledObject();
        using var pooledList = SharedPools.Default<List<double>>().GetPooledObject();

        var reverseMap = pooledMap.Object;
        AnalyzerNumberAssigner.Instance.GetReverseMap(reverseMap);

        var analyzerSet = pooledSet.Object;

        // get all analyzers
        foreach (var snapshot in _snapshots)
        {
            snapshot.AppendAnalyzers(analyzerSet);
        }

        var list = pooledList.Object;

        // calculate aggregated data per analyzer
        foreach (var assignedAnalyzerNumber in analyzerSet)
        {
            foreach (var snapshot in _snapshots)
            {
                var timeSpan = snapshot.GetTimeSpanInMillisecond(assignedAnalyzerNumber);
                if (timeSpan == null)
                {
                    // not all snapshot contains all analyzers
                    continue;
                }

                list.Add(timeSpan.Value);
            }

            // data is only stable once we have more than certain set
            // of samples
            if (list.Count < _minSampleSize)
            {
                continue;
            }

            // set performance data
            var analyzerId = reverseMap[assignedAnalyzerNumber];
            var (average, stddev) = GetAverageAndAdjustedStandardDeviation(list);
            aggregatedPerformanceDataPerAnalyzer.Add((analyzerId, average, stddev));

            list.Clear();
        }
    }

    private static (double average, double stddev) GetAverageAndAdjustedStandardDeviation(List<double> data)
    {
        var average = data.Average();
        var stddev = Math.Sqrt(data.Select(ms => Math.Pow(ms - average, 2)).Average());
        var squareLength = Math.Sqrt(data.Count);

        return (average, stddev / squareLength);
    }

    private class Snapshot
    {
        /// <summary>
        /// Raw performance data. 
        /// Keyed by analyzer unique number got from AnalyzerNumberAssigner.
        /// Value is delta (TimeSpan - minSpan) among span in this snapshot
        /// </summary>
        private readonly Dictionary<int, double> _performanceMap;

        public Snapshot(IEnumerable<(string analyzerId, TimeSpan timeSpan)> snapshot, int unitCount)
            : this(Convert(snapshot), unitCount)
        {
        }

        public Snapshot(IEnumerable<(int assignedAnalyzerNumber, TimeSpan timeSpan)> rawData, int unitCount)
        {
            _performanceMap = [];

            Reset(_performanceMap, rawData, unitCount);
        }

        public void Update(IEnumerable<(string analyzerId, TimeSpan timeSpan)> rawData, int unitCount)
            => Reset(_performanceMap, Convert(rawData), unitCount);

        public void AppendAnalyzers(HashSet<int> analyzerSet)
            => analyzerSet.UnionWith(_performanceMap.Keys);

        public double? GetTimeSpanInMillisecond(int assignedAnalyzerNumber)
        {
            if (!_performanceMap.TryGetValue(assignedAnalyzerNumber, out var value))
            {
                return null;
            }

            return value;
        }

        private static void Reset(
            Dictionary<int, double> map, IEnumerable<(int assignedAnalyzerNumber, TimeSpan timeSpan)> rawData, int fileCount)
        {
            // get smallest timespan in the snapshot
            var minSpan = rawData.Select(kv => kv.timeSpan).Min();

            // for now, we just clear the map, if reusing dictionary blindly became an issue due to
            // dictionary grew too big, then we need to do a bit more work to determine such case
            // and re-create new dictionary
            map.Clear();

            // map is normalized to current timespan - min timspan of the snapshot
            foreach (var (assignedAnalyzerNumber, timeSpan) in rawData)
            {
                map[assignedAnalyzerNumber] = (timeSpan.TotalMilliseconds - minSpan.TotalMilliseconds) / fileCount;
            }
        }

        private static IEnumerable<(int assignedAnalyzerNumber, TimeSpan timeSpan)> Convert(IEnumerable<(string analyzerId, TimeSpan timeSpan)> rawData)
            => rawData.Select(kv => (AnalyzerNumberAssigner.Instance.GetUniqueNumber(kv.analyzerId), kv.timeSpan));
    }

    /// <summary>
    /// Assign unique number to diagnostic analyzers
    /// </summary>
    private class AnalyzerNumberAssigner
    {
        public static readonly AnalyzerNumberAssigner Instance = new AnalyzerNumberAssigner();

        private int _currentId;

        // use simple approach for now. we don't expect it to grow too much. so entry added
        // won't be removed until process goes away
        private readonly Dictionary<string, int> _idMap;

        private AnalyzerNumberAssigner()
        {
            _currentId = 0;
            _idMap = [];
        }

        public int GetUniqueNumber(DiagnosticAnalyzer analyzer)
            => GetUniqueNumber(analyzer.GetAnalyzerId());

        public int GetUniqueNumber(string analyzerName)
        {
            // AnalyzerNumberAssigner.Instance can be accessed concurrently from different PerformanceQueue instances,
            // so we need to take a lock on '_idMap' for all read/write operations.
            lock (_idMap)
            {
                if (!_idMap.TryGetValue(analyzerName, out var id))
                {
                    id = _currentId++;
                    _idMap.Add(analyzerName, id);
                }

                return id;
            }
        }

        public void GetReverseMap(Dictionary<int, string> reverseMap)
        {
            reverseMap.Clear();

            // AnalyzerNumberAssigner.Instance can be accessed concurrently from different PerformanceQueue instances,
            // so we need to take a lock on '_idMap' for all read/write operations.
            lock (_idMap)
            {
                foreach (var kv in _idMap)
                {
                    reverseMap.Add(kv.Value, kv.Key);
                }
            }
        }
    }
}
