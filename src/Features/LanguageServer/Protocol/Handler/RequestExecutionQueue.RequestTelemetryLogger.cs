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
        /// Logs metadata on how many / failure rate of requests
        /// for this particular LSP server instance.
        /// </summary>
        internal class RequestTelemetryLogger : IDisposable
        {
            private readonly string _serverTypeName;

            /// <summary>
            /// Store request counters in a concurrent dictionary as non-mutating LSP requests can
            /// run alongside other non-mutating requests.
            /// </summary>
            private readonly ConcurrentDictionary<string, Counter> _requestCounters;

            public RequestTelemetryLogger(string serverTypeName)
            {
                _serverTypeName = serverTypeName;
                _requestCounters = new ConcurrentDictionary<string, Counter>();
            }

            public void LogSuccess(string methodName)
            {
                Interlocked.Increment(ref _requestCounters.GetOrAdd(methodName, new Counter())._successfulCount);
            }

            public void LogFailure(string methodName)
            {
                Interlocked.Increment(ref _requestCounters.GetOrAdd(methodName, new Counter())._failedCount);
            }

            /// <summary>
            /// Only output aggregate telemetry to the vs logger when the server instance is disposed
            /// to avoid spamming the telemetry output with thousands of events
            /// </summary>
            public void Dispose()
            {
                foreach (var kvp in _requestCounters)
                {
                    Logger.Log(FunctionId.LSP_RequestCounter, KeyValueLogMessage.Create(LogType.Trace, m =>
                    {
                        m["server"] = _serverTypeName;
                        m["method"] = kvp.Key;
                        m["successful"] = kvp.Value._successfulCount;
                        m["failed"] = kvp.Value._failedCount;
                    }));
                }

                // Clear telemetry we've published in case dispose is called multiple times.
                _requestCounters.Clear();
            }

            private class Counter
            {
                public int _successfulCount;
                public int _failedCount;
            }
        }
    }
}
