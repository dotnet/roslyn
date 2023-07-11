﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.Telemetry;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteProcessTelemetryService
    {
        /// <summary>
        /// Track when last time report has sent and send new report if there is update after given internal
        /// </summary>
        private class PerformanceReporter : GlobalOperationAwareIdleProcessor
        {
            private readonly SemaphoreSlim _event;

            private readonly IPerformanceTrackerService _diagnosticAnalyzerPerformanceTracker;
            private readonly TelemetrySession _telemetrySession;

            public PerformanceReporter(
                TelemetrySession telemetrySession,
                IPerformanceTrackerService diagnosticAnalyzerPerformanceTracker,
                IGlobalOperationNotificationService globalOperationNotificationService,
                CancellationToken shutdownToken)
                : base(
                    AsynchronousOperationListenerProvider.NullListener,
                    globalOperationNotificationService,
                    backOffTimeSpan: TimeSpan.FromMinutes(2),
                    shutdownToken)
            {
                _event = new SemaphoreSlim(initialCount: 0);

                _telemetrySession = telemetrySession;
                _diagnosticAnalyzerPerformanceTracker = diagnosticAnalyzerPerformanceTracker;
                _diagnosticAnalyzerPerformanceTracker.SnapshotAdded += OnSnapshotAdded;
                Start();
            }

            protected override void OnPaused()
            {
                // we won't cancel report already running. we will just prevent
                // new one from starting.
            }

            protected override Task ExecuteAsync()
            {
                if (!_telemetrySession.IsOptedIn)
                    return Task.CompletedTask;

                using (RoslynLogger.LogBlock(FunctionId.Diagnostics_GeneratePerformaceReport, CancellationToken))
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

                    return Task.CompletedTask;
                }
            }

            protected override Task WaitAsync(CancellationToken cancellationToken)
            {
                return _event.WaitAsync(cancellationToken);
            }

            private void OnSnapshotAdded(object sender, EventArgs e)
            {
                // this acts like Monitor.Pulse. (wake up event if it is currently waiting
                // if not, ignore. this can have race, but that's fine for this usage case)
                // not using Monitor.Pulse since that doesn't support WaitAsync
                if (_event.CurrentCount > 0)
                {
                    return;
                }

                _event.Release();
            }
        }
    }
}
