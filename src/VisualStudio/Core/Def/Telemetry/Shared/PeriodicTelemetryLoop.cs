// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.Telemetry;

/// <summary>
/// Runs a telemetry action on a fixed interval until canceled.
/// </summary>
internal static class PeriodicTelemetryLoop
{
    /// <summary>
    /// Invokes <paramref name="action"/> every <paramref name="interval"/>, starting one interval from now, until
    /// <paramref name="cancellationToken"/> is canceled. A failing invocation is reported as a non-fatal error and does
    /// not stop later invocations. The returned task completes (without faulting) once canceled.
    /// </summary>
    public static async Task RunAsync(TimeSpan interval, Action action, CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                // Keep looping: one failed invocation must not stop every later one.
            }
        }
    }
}
