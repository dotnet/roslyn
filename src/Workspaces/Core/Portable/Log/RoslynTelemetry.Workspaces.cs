// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Property names that telemetry consumers depend on by string. Kept as constants so the emitted shape
/// is stable and greppable.
/// </summary>
internal static class TelemetryKeys
{
    public const string Name = "Name";
    public const string Value = "Value";
    public const string LanguageName = "LanguageName";
}

/// <summary>
/// The parts of <see cref="RoslynTelemetry"/> that need <see cref="KeyValueLogMessage"/>, which lives in
/// the Workspaces layer and so is not available in the shared layer.
/// </summary>
internal static partial class RoslynTelemetry
{
    /// <summary>
    /// Posts a discrete event carrying the wall-clock duration of the returned scope, but only if it
    /// meets or exceeds <paramref name="minThresholdMs"/>. Unlike
    /// <see cref="RecordBlockTime(FunctionId, string, int)"/> this is not aggregated - each occurrence is
    /// its own event.
    /// </summary>
    public static IDisposable? LogBlockTime(FunctionId functionId, KeyValueLogMessage logMessage, int minThresholdMs = -1)
        => GetEventSinks().IsEmpty ? null : new TimedEventBlock(functionId, logMessage, minThresholdMs);

    private sealed class TimedEventBlock : IDisposable
    {
        private readonly FunctionId _functionId;
        private readonly KeyValueLogMessage _logMessage;
        private readonly int _minThresholdMs;
        private readonly int _tick;

        public TimedEventBlock(FunctionId functionId, KeyValueLogMessage logMessage, int minThresholdMs)
        {
            _functionId = functionId;
            _logMessage = logMessage;
            _minThresholdMs = minThresholdMs;
            _tick = Environment.TickCount;
        }

        public void Dispose()
        {
            // This delta is valid for durations of < 25 days
            var elapsed = Environment.TickCount - _tick;
            if (elapsed >= _minThresholdMs)
            {
                var logMessage = KeyValueLogMessage.Create(static (m, args) =>
                {
                    m[TelemetryKeys.Value] = (long)args.elapsed;
                    m.AddRange(args.properties);
                }, (elapsed, properties: _logMessage.Properties));

#if DEBUG
                logMessage.Free();
#else
                // Don't skew telemetry results by logging in debug bits or under debugger.
                if (Debugger.IsAttached)
                    logMessage.Free();
                else
                    Log(_functionId, logMessage);
#endif
            }

            _logMessage.Free();
        }
    }
}
