// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Telemetry;

/// <summary>
/// Provides a mechanism to log telemetry information containing the execution time between
/// creation and disposal of this object.
/// </summary>
internal sealed class TimedTelemetryLogBlock : IDisposable
{
    private readonly KeyValueLogMessage _logMessage;
    private readonly int _minThresholdMs;
#pragma warning disable IDE0052 // Remove unread private members - Not used in debug builds
    private readonly ITelemetryLog _telemetryLog;
#pragma warning restore IDE0052 // Remove unread private members
    private readonly SharedStopwatch _stopwatch;

    public TimedTelemetryLogBlock(KeyValueLogMessage logMessage, int minThresholdMs, ITelemetryLog telemetryLog)
    {
        _logMessage = logMessage;
        _minThresholdMs = minThresholdMs;
        _telemetryLog = telemetryLog;
        _stopwatch = SharedStopwatch.StartNew();
    }

    public void Dispose()
    {
        var elapsed = (long)_stopwatch.Elapsed.TotalMilliseconds;
        if (elapsed >= _minThresholdMs)
        {
            var logMessage = KeyValueLogMessage.Create(m =>
            {
                m[TelemetryLogging.KeyValue] = elapsed;

                m.AddRange(_logMessage.Properties);
            });

#if !DEBUG
            // Don't skew telemetry results by logging in debug bits or under debugger.
            if (!Debugger.IsAttached)
                _telemetryLog.Log(logMessage);
#endif
            logMessage.Free();
        }

        _logMessage.Free();
    }
}
