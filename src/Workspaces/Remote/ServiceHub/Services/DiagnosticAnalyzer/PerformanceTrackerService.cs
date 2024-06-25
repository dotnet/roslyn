// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    /// <summary>
    /// Track diagnostic performance 
    /// </summary>
    [ExportWorkspaceService(typeof(IPerformanceTrackerService), WorkspaceKinds.RemoteWorkspace), Shared]
    internal class PerformanceTrackerService : IPerformanceTrackerService
    {
        // We require at least 100 samples for background document analysis result to be stable.
        private const int MinSampleSizeForDocumentAnalysis = 100;
        // We require at least 20 samples for span/lightbulb analysis result to be stable.
        // Note that each lightbulb invocation produces 4 samples, one for each of the below diagnostic computaion:
        //      1. Compiler syntax diagnostics
        //      2. Analyzer syntax diagnostics
        //      3. Compiler semantic diagnostics
        //      4. Analyzer semantic diagnostics
        private const int MinSampleSizeForSpanAnalysis = 20;

        private static readonly Func<IEnumerable<AnalyzerPerformanceInfo>, int, bool, string> s_snapshotLogger = SnapshotLogger;

        private readonly PerformanceQueue _queueForDocumentAnalysis, _queueForSpanAnalysis;
        private readonly ConcurrentDictionary<string, bool> _builtInMap = new ConcurrentDictionary<string, bool>(concurrencyLevel: 2, capacity: 10);

        public event EventHandler SnapshotAdded;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PerformanceTrackerService()
            : this(MinSampleSizeForDocumentAnalysis, MinSampleSizeForSpanAnalysis)
        {
        }

        // internal for testing
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used incorrectly by tests")]
        internal PerformanceTrackerService(int minSampleSizeForDocumentAnalysis, int minSampleSizeForSpanAnalysis)
        {
            _queueForDocumentAnalysis = new PerformanceQueue(minSampleSizeForDocumentAnalysis);

            _queueForSpanAnalysis = new PerformanceQueue(minSampleSizeForSpanAnalysis);
        }

        private PerformanceQueue GetQueue(bool forSpanAnalysis)
            => forSpanAnalysis ? _queueForSpanAnalysis : _queueForDocumentAnalysis;

        public void AddSnapshot(IEnumerable<AnalyzerPerformanceInfo> snapshot, int unitCount, bool forSpanAnalysis)
        {
            foreach (var perfInfo in snapshot)
            {
                const int PerformAnalysisTelemetryDelay = 250;

                var delay = (int)perfInfo.TimeSpan.TotalMilliseconds;

                TelemetryLogging.LogAggregated(FunctionId.PerformAnalysis_Summary, $"IndividualTimes", delay);

                if (delay > PerformAnalysisTelemetryDelay)
                {
                    const string AnalyzerId = nameof(AnalyzerId);
                    const string Delay = nameof(Delay);
                    const string ForSpanAnalysis = nameof(ForSpanAnalysis);

                    var analyzerId = perfInfo.BuiltIn ? perfInfo.AnalyzerId : perfInfo.BuiltIn.GetHashCode().ToString();

                    var logMessage = KeyValueLogMessage.Create(m =>
                    {
                        m[AnalyzerId] = analyzerId;
                        m[Delay] = delay;
                        m[ForSpanAnalysis] = forSpanAnalysis;
                    });

                    TelemetryLogging.Log(FunctionId.PerformAnalysis_Delay, logMessage);
                }
            }

            Logger.Log(FunctionId.PerformanceTrackerService_AddSnapshot, s_snapshotLogger, snapshot, unitCount, forSpanAnalysis);

            RecordBuiltInAnalyzers(snapshot);

            var queue = GetQueue(forSpanAnalysis);
            lock (queue)
            {
                queue.Add(snapshot.Select(entry => (entry.AnalyzerId, entry.TimeSpan)), unitCount);
            }

            OnSnapshotAdded();
        }

        public void GenerateReport(List<AnalyzerInfoForPerformanceReporting> analyzerInfos, bool forSpanAnalysis)
        {
            using var pooledRaw = SharedPools.Default<List<(string analyzerId, double average, double stddev)>>().GetPooledObject();

            var rawPerformanceData = pooledRaw.Object;

            var queue = GetQueue(forSpanAnalysis);
            lock (queue)
            {
                // first get raw aggregated peformance data from the queue
                queue.GetPerformanceData(rawPerformanceData);
            }

            // make sure there are some data
            if (rawPerformanceData.Count == 0)
            {
                return;
            }

            foreach (var (analyzerId, average, stddev) in rawPerformanceData.OrderByDescending(k => k.average))
            {
                analyzerInfos.Add(new AnalyzerInfoForPerformanceReporting(AllowTelemetry(analyzerId), analyzerId, average, stddev));
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
            => SnapshotAdded?.Invoke(this, EventArgs.Empty);

        private static string SnapshotLogger(IEnumerable<AnalyzerPerformanceInfo> snapshots, int unitCount, bool forSpan)
        {
            using var pooledObject = SharedPools.Default<StringBuilder>().GetPooledObject();
            var sb = pooledObject.Object;

            sb.Append(unitCount);

            sb.Append('(');
            sb.Append(forSpan ? "SpanAnalysis" : "DocumentAnalysis");
            sb.Append(')');

            foreach (var snapshot in snapshots)
            {
                sb.Append('|');
                sb.Append(snapshot.AnalyzerId);
                sb.Append(':');
                sb.Append(snapshot.BuiltIn);
                sb.Append(':');
                sb.Append(snapshot.TimeSpan.TotalMilliseconds);
            }

            sb.Append('*');

            return sb.ToString();
        }
    }
}
