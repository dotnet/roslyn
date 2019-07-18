// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    /// <summary>
    /// This queue hold onto raw performance data. this type itself is not thread safe. the one who uses this type
    /// should take care of that.
    /// </summary>
    /// <threadsafety static="false" instance="false"/>
    internal class PerformanceQueue
    {
        // we need at least 100 samples for result to be stable
        private const int MinSampleSize = 100;

        private readonly int _maxSampleSize;
        private readonly LinkedList<Snapshot> _snapshots;

        public PerformanceQueue(int maxSampleSize)
        {
            Contract.ThrowIfFalse(maxSampleSize > MinSampleSize);

            _maxSampleSize = maxSampleSize;
            _snapshots = new LinkedList<Snapshot>();
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
        }

        public void GetPerformanceData(Dictionary<string, (double average, double stddev)> aggregatedPerformanceDataPerAnalyzer)
        {
            if (_snapshots.Count < MinSampleSize)
            {
                // we don't have enough data to report this
                return;
            }

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
                if (list.Count < MinSampleSize)
                {
                    continue;
                }

                // set performance data
                aggregatedPerformanceDataPerAnalyzer[reverseMap[assignedAnalyzerNumber]] = GetAverageAndAdjustedStandardDeviation(list);

                list.Clear();
            }
        }

        private (double average, double stddev) GetAverageAndAdjustedStandardDeviation(List<double> data)
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

            public Snapshot(IEnumerable<(string analyzerId, TimeSpan timeSpan)> snapshot, int unitCount) :
                this(Convert(snapshot), unitCount)
            {
            }

            public Snapshot(IEnumerable<(int assignedAnalyzerNumber, TimeSpan timeSpan)> rawData, int unitCount)
            {
                _performanceMap = new Dictionary<int, double>();

                Reset(_performanceMap, rawData, unitCount);
            }

            public void Update(IEnumerable<(string analyzerId, TimeSpan timeSpan)> rawData, int unitCount)
            {
                Reset(_performanceMap, Convert(rawData), unitCount);
            }

            public void AppendAnalyzers(HashSet<int> analyzerSet)
            {
                analyzerSet.UnionWith(_performanceMap.Keys);
            }

            public double? GetTimeSpanInMillisecond(int assignedAnalyzerNumber)
            {
                if (!_performanceMap.TryGetValue(assignedAnalyzerNumber, out var value))
                {
                    return null;
                }

                return value;
            }

            private void Reset(
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
            {
                return rawData.Select(kv => (AnalyzerNumberAssigner.Instance.GetUniqueNumber(kv.analyzerId), kv.timeSpan));
            }
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
                _idMap = new Dictionary<string, int>();
            }

            public int GetUniqueNumber(DiagnosticAnalyzer analyzer)
            {
                return GetUniqueNumber(analyzer.GetAnalyzerId());
            }

            public int GetUniqueNumber(string analyzerName)
            {
                if (!_idMap.TryGetValue(analyzerName, out var id))
                {
                    id = _currentId++;
                    _idMap.Add(analyzerName, id);
                }

                return id;
            }

            public void GetReverseMap(Dictionary<int, string> reverseMap)
            {
                reverseMap.Clear();

                foreach (var kv in _idMap)
                {
                    reverseMap.Add(kv.Value, kv.Key);
                }
            }
        }
    }
}
