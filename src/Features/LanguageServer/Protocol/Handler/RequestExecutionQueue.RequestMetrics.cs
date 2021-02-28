// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal partial class RequestExecutionQueue
    {
        internal class RequestMetrics
        {
            private readonly string _methodName;
            private readonly SharedStopwatch _sharedStopWatch;
            private TimeSpan? _queuedDuration;

            private readonly RequestTelemetryLogger _requestTelemetryLogger;

            public RequestMetrics(string methodName, RequestTelemetryLogger requestTelemetryLogger)
            {
                _methodName = methodName;
                _requestTelemetryLogger = requestTelemetryLogger;
                _sharedStopWatch = SharedStopwatch.StartNew();
            }

            public void RecordExecutionStart()
            {
                // Request has de-queued and is starting execution.  Record the time it spent in queue.
                _queuedDuration = _sharedStopWatch.Elapsed;
            }

            public void RecordSuccess()
            {
                RecordCompletion(RequestTelemetryLogger.Result.Succeeded);
            }

            public void RecordFailure()
            {
                RecordCompletion(RequestTelemetryLogger.Result.Failed);
            }

            public void RecordCancellation()
            {
                RecordCompletion(RequestTelemetryLogger.Result.Cancelled);
            }

            private void RecordCompletion(RequestTelemetryLogger.Result result)
            {
                Contract.ThrowIfNull(_queuedDuration, "RecordExecutionStart was not called");
                var overallDuration = _sharedStopWatch.Elapsed;
                _requestTelemetryLogger.UpdateTelemetryData(_methodName, _queuedDuration.Value, overallDuration, result);
            }
        }
    }
}
