// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Telemetry
{
    /// <summary>
    /// Provides a mechanism to log telemetry information containing the execution time between
    /// creation and disposal of this object.
    /// </summary>
    internal sealed class TimedTelemetryLogBlock : IDisposable
    {
#pragma warning disable IDE0052 // Remove unread private members - Not used in debug builds
        private readonly string _name;
        private readonly int _minThresholdMs;
        private readonly ITelemetryLog _telemetryLog;
        private readonly SharedStopwatch _stopwatch;
#pragma warning restore IDE0052 // Remove unread private members

        public TimedTelemetryLogBlock(string name, int minThresholdMs, ITelemetryLog telemetryLog)
        {
            _name = name;
            _minThresholdMs = minThresholdMs;
            _telemetryLog = telemetryLog;
            _stopwatch = SharedStopwatch.StartNew();
        }

        public void Dispose()
        {
            // Don't add elapsed information in debug bits or while under debugger.
#if !DEBUG
            if (Debugger.IsAttached)
                return;

            var elapsed = (int)_stopwatch.Elapsed.TotalMilliseconds;
            if (elapsed >= _minThresholdMs)
            {
                var logMessage = KeyValueLogMessage.Create(m =>
                {
                    m[TelemetryLogging.AggregatedKeyName] = _name;
                    m[TelemetryLogging.AggregatedKeyValue] = elapsed;
                });

                _telemetryLog.Log(logMessage);
            }
#endif
        }
    }
}
