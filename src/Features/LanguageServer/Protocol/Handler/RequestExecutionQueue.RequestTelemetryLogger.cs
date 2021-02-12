// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal partial class RequestExecutionQueue
    {
        /// <summary>
        /// Logs metadata on LSP requests (duration, success / failure metrics)
        /// for this particular LSP server instance.
        /// </summary>
        internal class RequestTelemetryLogger : IDisposable
        {
            private const string QueuedDurationKey = "QueuedDuration";

            private readonly string _serverTypeName;
            private HistogramLogAggregator? _histogramLogAggregator;

            /// <summary>
            /// Store request counters in a concurrent dictionary as non-mutating LSP requests can
            /// run alongside other non-mutating requests.
            /// </summary>
            private readonly ConcurrentDictionary<string, Counter> _requestCounters;

            public RequestTelemetryLogger(string serverTypeName)
            {
                _serverTypeName = serverTypeName;
                _requestCounters = new ConcurrentDictionary<string, Counter>();
                _histogramLogAggregator = CreateLogAggregator();
            }

            public void UpdateTelemetryData(string methodName, TimeSpan queuedDuration, TimeSpan requestDuration, Result result)
            {
                // Find the bucket corresponding to the queued duration and update the count of durations in that bucket.
                // This is not broken down per method as time in queue is not specific to an LSP method.
                _histogramLogAggregator?.IncreaseCount(QueuedDurationKey, Convert.ToDecimal(queuedDuration.TotalMilliseconds));

                // Store the request time metrics per LSP method.
                _histogramLogAggregator?.IncreaseCount(methodName, Convert.ToDecimal(requestDuration.TotalMilliseconds));
                _requestCounters.GetOrAdd(methodName, (_) => new Counter()).IncrementCount(result);
            }

            /// <summary>
            /// Only output aggregate telemetry to the vs logger when the server instance is disposed
            /// to avoid spamming the telemetry output with thousands of events
            /// </summary>
            public void Dispose()
            {
                if (_histogramLogAggregator is null || _histogramLogAggregator.IsEmpty)
                {
                    return;
                }

                var queuedDurationCounter = _histogramLogAggregator.GetValue(QueuedDurationKey);
                Logger.Log(FunctionId.LSP_TimeInQueue, KeyValueLogMessage.Create(LogType.Trace, m =>
                {
                    m["server"] = _serverTypeName;
                    m["bucketsize"] = queuedDurationCounter?.BucketSize;
                    m["maxbucketvalue"] = queuedDurationCounter?.MaxBucketValue;
                    m["buckets"] = queuedDurationCounter?.GetBucketsAsString();
                }));

                foreach (var kvp in _requestCounters)
                {
                    var requestExecutionDuration = _histogramLogAggregator.GetValue(kvp.Key);

                    Logger.Log(FunctionId.LSP_RequestCounter, KeyValueLogMessage.Create(LogType.Trace, m =>
                    {
                        m["server"] = _serverTypeName;
                        m["method"] = kvp.Key;
                        m["successful"] = kvp.Value.SucceededCount;
                        m["failed"] = kvp.Value.FailedCount;
                        m["cancelled"] = kvp.Value.CancelledCount;
                    }));

                    Logger.Log(FunctionId.LSP_RequestDuration, KeyValueLogMessage.Create(LogType.Trace, m =>
                    {
                        m["server"] = _serverTypeName;
                        m["method"] = kvp.Key;
                        m["bucketsize_ms"] = requestExecutionDuration?.BucketSize;
                        m["maxbucketvalue_ms"] = requestExecutionDuration?.MaxBucketValue;
                        m["bucketdata"] = requestExecutionDuration?.GetBucketsAsString();
                    }));
                }

                // Clear telemetry we've published in case dispose is called multiple times.
                _requestCounters.Clear();
                _histogramLogAggregator = null;
            }

            /// <summary>
            /// Creates a histogram log aggregator where request durations (ms) are distributed into buckets of
            /// size 50ms, with the largets bucket starting at 5000ms.
            /// </summary>
            private static HistogramLogAggregator CreateLogAggregator() => new HistogramLogAggregator(bucketSize: 50, maxBucketValue: 5000);

            private class Counter
            {
                private int _succeededCount;
                private int _failedCount;
                private int _cancelledCount;

                public int SucceededCount => _succeededCount;
                public int FailedCount => _failedCount;
                public int CancelledCount => _cancelledCount;

                public void IncrementCount(Result result)
                {
                    switch (result)
                    {
                        case Result.Succeeded:
                            Interlocked.Increment(ref _succeededCount);
                            break;
                        case Result.Failed:
                            Interlocked.Increment(ref _failedCount);
                            break;
                        case Result.Cancelled:
                            Interlocked.Increment(ref _cancelledCount);
                            break;
                    }
                }
            }

            internal enum Result
            {
                Succeeded,
                Failed,
                Cancelled
            }
        }
    }
}
