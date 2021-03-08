﻿// Licensed to the .NET Foundation under one or more agreements.
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

            /// <summary>
            /// Histogram to aggregate the time in queue metrics.
            /// </summary>
            private HistogramLogAggregator? _queuedDurationLogAggregator;

            /// <summary>
            /// Histogram to aggregate total request duration metrics.
            /// This histogram is log based as request latencies can be highly variable depending
            /// on the request being handled.  As such, we apply the log based function
            /// defined by ComputeLogValue to the request latencies for storing in the histogram.
            /// This provides highly detailed buckets when duration is in MS, but less detailed
            /// when the duration is in terms of seconds or minutes.
            /// </summary>
            private HistogramLogAggregator? _requestDurationLogAggregator;

            /// <summary>
            /// Store request counters in a concurrent dictionary as non-mutating LSP requests can
            /// run alongside other non-mutating requests.
            /// </summary>
            private readonly ConcurrentDictionary<string, Counter> _requestCounters;

            public RequestTelemetryLogger(string serverTypeName)
            {
                _serverTypeName = serverTypeName;
                _requestCounters = new ConcurrentDictionary<string, Counter>();

                // Buckets queued duration into 50ms buckets with the last bucket starting at 5000ms.
                // Queue times should generally be relatively short so tracking beyond 5000ms isn't that useful.
                _queuedDurationLogAggregator = new HistogramLogAggregator(bucketSize: 50, maxBucketValue: 5000);

                // Since this is a log based histogram, these are appropriate bucket sizes for the log data.
                // A bucket at 1 corresponds to ~26ms, while the max bucket value corresponds to ~17minutes
                _requestDurationLogAggregator = new HistogramLogAggregator(bucketSize: 1, maxBucketValue: 40);
            }

            public void UpdateTelemetryData(string methodName, TimeSpan queuedDuration, TimeSpan requestDuration, Result result)
            {
                // Find the bucket corresponding to the queued duration and update the count of durations in that bucket.
                // This is not broken down per method as time in queue is not specific to an LSP method.
                _queuedDurationLogAggregator?.IncreaseCount(QueuedDurationKey, Convert.ToDecimal(queuedDuration.TotalMilliseconds));

                // Store the request time metrics per LSP method.
                _requestDurationLogAggregator?.IncreaseCount(methodName, Convert.ToDecimal(ComputeLogValue(requestDuration.TotalMilliseconds)));
                _requestCounters.GetOrAdd(methodName, (_) => new Counter()).IncrementCount(result);
            }

            /// <summary>
            /// Given an input duration in MS, this transforms it using
            /// the log function below to put in reasonable log based buckets
            /// from 50ms to 1 hour.  Similar transformations must be done to read
            /// the data from kusto.
            /// </summary>
            private static double ComputeLogValue(double durationInMS)
            {
                return 10d * Math.Log10((durationInMS / 100d) + 1);
            }

            /// <summary>
            /// Only output aggregate telemetry to the vs logger when the server instance is disposed
            /// to avoid spamming the telemetry output with thousands of events
            /// </summary>
            public void Dispose()
            {
                if (_queuedDurationLogAggregator is null || _queuedDurationLogAggregator.IsEmpty
                    || _requestDurationLogAggregator is null || _requestDurationLogAggregator.IsEmpty)
                {
                    return;
                }

                var queuedDurationCounter = _queuedDurationLogAggregator.GetValue(QueuedDurationKey);
                Logger.Log(FunctionId.LSP_TimeInQueue, KeyValueLogMessage.Create(LogType.Trace, m =>
                {
                    m["server"] = _serverTypeName;
                    m["bucketsize_ms"] = queuedDurationCounter?.BucketSize;
                    m["maxbucketvalue_ms"] = queuedDurationCounter?.MaxBucketValue;
                    m["buckets"] = queuedDurationCounter?.GetBucketsAsString();
                }));

                foreach (var kvp in _requestCounters)
                {
                    Logger.Log(FunctionId.LSP_RequestCounter, KeyValueLogMessage.Create(LogType.Trace, m =>
                    {
                        m["server"] = _serverTypeName;
                        m["method"] = kvp.Key;
                        m["successful"] = kvp.Value.SucceededCount;
                        m["failed"] = kvp.Value.FailedCount;
                        m["cancelled"] = kvp.Value.CancelledCount;
                    }));

                    var requestExecutionDuration = _requestDurationLogAggregator.GetValue(kvp.Key);
                    Logger.Log(FunctionId.LSP_RequestDuration, KeyValueLogMessage.Create(LogType.Trace, m =>
                    {
                        m["server"] = _serverTypeName;
                        m["method"] = kvp.Key;
                        m["bucketsize_logms"] = requestExecutionDuration?.BucketSize;
                        m["maxbucketvalue_logms"] = requestExecutionDuration?.MaxBucketValue;
                        m["bucketdata_logms"] = requestExecutionDuration?.GetBucketsAsString();
                    }));
                }

                // Clear telemetry we've published in case dispose is called multiple times.
                _requestCounters.Clear();
                _queuedDurationLogAggregator = null;
                _requestDurationLogAggregator = null;
            }

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
