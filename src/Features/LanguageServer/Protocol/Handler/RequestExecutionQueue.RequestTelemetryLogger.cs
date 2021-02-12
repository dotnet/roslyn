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
                // Update the overall queue time metrics since the time in queue is not specific to the LSP method.
                _histogramLogAggregator?.IncreaseCount(QueuedDurationKey, Convert.ToDecimal(queuedDuration.TotalMilliseconds));

                // Store the request time metrics per LSP method.
                _histogramLogAggregator?.IncreaseCount(methodName, Convert.ToDecimal(requestDuration.TotalMilliseconds));
                switch (result)
                {
                    case Result.Succeeded:
                        Interlocked.Increment(ref _requestCounters.GetOrAdd(methodName, new Counter())._succeededCount);
                        break;
                    case Result.Failed:
                        Interlocked.Increment(ref _requestCounters.GetOrAdd(methodName, new Counter())._failedCount);
                        break;
                    case Result.Cancelled:
                        Interlocked.Increment(ref _requestCounters.GetOrAdd(methodName, new Counter())._cancelledCount);
                        break;
                }
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
                        m["successful"] = kvp.Value._succeededCount;
                        m["failed"] = kvp.Value._failedCount;
                        m["cancelled"] = kvp.Value._cancelledCount;
                        m["bucketsize"] = requestExecutionDuration?.BucketSize;
                        m["maxbucketvalue"] = requestExecutionDuration?.MaxBucketValue;
                        m["buckets"] = requestExecutionDuration?.GetBucketsAsString();
                    }));
                }

                // Clear telemetry we've published in case dispose is called multiple times.
                _requestCounters.Clear();
                _histogramLogAggregator = null;
            }

            private static HistogramLogAggregator CreateLogAggregator() => new HistogramLogAggregator(bucketSize: 50, maxBucketValue: 5000);

            private class Counter
            {
                public int _succeededCount;
                public int _failedCount;
                public int _cancelledCount;
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
