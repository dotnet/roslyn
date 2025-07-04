// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Telemetry;

internal interface ITelemetryLogProvider
{
    /// <summary>
    /// Returns an <see cref="ITelemetryLog"/> for logging telemetry.
    /// </summary>
    /// <param name="functionId">FunctionId representing the telemetry operation</param>
    ITelemetryBlockLog? GetLog(FunctionId functionId);

    /// <summary>
    /// Returns an aggregating <see cref="ITelemetryLog"/> for logging histogram based telemetry.
    /// </summary>
    /// <param name="functionId">FunctionId representing the telemetry operation</param>
    /// <param name="bucketBoundaries">Optional values indicating bucket boundaries in milliseconds. If not specified, 
    /// all aggregating events created will use a default configuration</param>
    ITelemetryBlockLog? GetHistogramLog(FunctionId functionId, double[]? bucketBoundaries = null);

    /// <summary>
    /// Returns an aggregating <see cref="ITelemetryLog"/> for logging counter telemetry.
    /// </summary>
    /// <param name="functionId">FunctionId representing the telemetry operation</param>
    ITelemetryLog? GetCounterLog(FunctionId functionId);

    /// <summary>
    /// Flushes all telemetry logs
    /// </summary>
    void Flush();
}
