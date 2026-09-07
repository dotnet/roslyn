// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Property names that telemetry consumers depend on by string.
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
    /// <see cref="RecordBlockTime(FunctionId, string)"/> this is not aggregated - each occurrence is
    /// its own event.
    /// </summary>
    public static IDisposable? LogBlockTime(FunctionId functionId, KeyValueLogMessage logMessage, int minThresholdMs = -1)
    {
        if (TryGetEnabledSinks(functionId, out _))
            return new TimedEventBlock(functionId, logMessage, minThresholdMs);

        logMessage.Free();
        return null;
    }

    private sealed class TimedEventBlock(FunctionId functionId, KeyValueLogMessage logMessage, int minThresholdMs) : IDisposable
    {
        private readonly SharedStopwatch _stopwatch = SharedStopwatch.StartNew();

        public void Dispose()
        {
            var elapsed = (long)_stopwatch.Elapsed.TotalMilliseconds;
            if (elapsed >= minThresholdMs)
            {
                // Properties is read inside the setter so that the source message's map is only
                // materialized on the path that actually posts.
                var message = KeyValueLogMessage.Create(static (m, args) =>
                {
                    m[TelemetryKeys.Value] = args.elapsed;
                    m.AddRange(args.source.Properties);
                }, (elapsed, source: logMessage));

                // Don't skew telemetry results by logging in debug bits or under a debugger.
                if (IsDebugging)
                    message.Free();
                else
                    Log(functionId, message);
            }

            logMessage.Free();
        }
    }
}
