// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote;

internal partial class RemoteProcessTelemetryService
{
    /// <summary>
    /// Track when last time report has sent and send new report if there is update after given internal
    /// </summary>
    private sealed class PerformanceReporter
    {
        private readonly IPerformanceTrackerService _diagnosticAnalyzerPerformanceTracker;
        private readonly TelemetrySession _telemetrySession;
        private readonly AsyncBatchingWorkQueue _workQueue;

        public PerformanceReporter(
            TelemetrySession telemetrySession,
            IPerformanceTrackerService diagnosticAnalyzerPerformanceTracker,
            CancellationToken shutdownToken)
        {
            _telemetrySession = telemetrySession;
            _diagnosticAnalyzerPerformanceTracker = diagnosticAnalyzerPerformanceTracker;

            _workQueue = new AsyncBatchingWorkQueue(
                TimeSpan.FromMinutes(2),
                ProcessWorkAsync,
                AsynchronousOperationListenerProvider.NullListener,
                shutdownToken);

            _diagnosticAnalyzerPerformanceTracker.SnapshotAdded += (_, _) => _workQueue.AddWork();
        }

        private ValueTask ProcessWorkAsync(CancellationToken cancellationToken)
        {
            if (!_telemetrySession.IsOptedIn)
                return ValueTaskFactory.CompletedTask;

            using (RoslynLogger.LogBlock(FunctionId.Diagnostics_GeneratePerformaceReport, cancellationToken))
            {
                foreach (var forSpanAnalysis in new[] { false, true })
                {
                    using var pooledObject = SharedPools.Default<List<AnalyzerInfoForPerformanceReporting>>().GetPooledObject();
                    _diagnosticAnalyzerPerformanceTracker.GenerateReport(pooledObject.Object, forSpanAnalysis);
                    var isInternalUser = _telemetrySession.IsUserMicrosoftInternal;

                    foreach (var analyzerInfo in pooledObject.Object)
                    {
                        // this will report telemetry under VS. this will let us see how accurate our performance tracking is
                        RoslynLogger.Log(FunctionId.Diagnostics_AnalyzerPerformanceInfo2, KeyValueLogMessage.Create(m =>
                        {
                            // since it is telemetry, we hash analyzer name if it is not builtin analyzer
                            m[nameof(analyzerInfo.AnalyzerId)] = isInternalUser ? analyzerInfo.AnalyzerId : analyzerInfo.PIISafeAnalyzerId;
                            m[nameof(analyzerInfo.Average)] = analyzerInfo.Average;
                            m[nameof(analyzerInfo.AdjustedStandardDeviation)] = analyzerInfo.AdjustedStandardDeviation;
                            m[nameof(forSpanAnalysis)] = forSpanAnalysis;
                        }, LogLevel.Debug));
                    }
                }
            }

            return ValueTaskFactory.CompletedTask;
        }
    }
}
