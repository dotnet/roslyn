// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry;

/// <summary>
/// Provides access to an appropriate <see cref="ITelemetryLogProvider"/> for logging telemetry.
/// </summary>
internal sealed class TelemetryLogProvider : ITelemetryLogProvider
{
    private readonly TelemetrySession _session;
    private readonly ILogger _telemetryLogger;

    /// <summary>
    /// Manages instances of <see cref="VisualStudioTelemetryLog"/> to provide in <see cref="GetLog(FunctionId)"/>
    /// </summary>
    private ImmutableDictionary<FunctionId, VisualStudioTelemetryLog> _logs = ImmutableDictionary<FunctionId, VisualStudioTelemetryLog>.Empty;

    /// <summary>
    /// Manages instances of <see cref="AggregatingHistogramLog"/> to provide in <see cref="GetHistogramLog(FunctionId, double[])"/>
    /// </summary>
    private ImmutableDictionary<FunctionId, AggregatingHistogramLog> _histogramLogs = ImmutableDictionary<FunctionId, AggregatingHistogramLog>.Empty;

    /// <summary>
    /// Manages instances of <see cref="AggregatingCounterLog"/> to provide in <see cref="GetCounterLog(FunctionId)"/>
    /// </summary>
    private ImmutableDictionary<FunctionId, AggregatingCounterLog> _counterLogs = ImmutableDictionary<FunctionId, AggregatingCounterLog>.Empty;

    private TelemetryLogProvider(TelemetrySession session, ILogger telemetryLogger)
    {
        _session = session;
        _telemetryLogger = telemetryLogger;
    }

    public static TelemetryLogProvider Create(TelemetrySession session, ILogger telemetryLogger)
    {
        var logProvider = new TelemetryLogProvider(session, telemetryLogger);

        TelemetryLogging.SetLogProvider(logProvider);

        return logProvider;
    }

    /// <summary>
    /// Returns an <see cref="ITelemetryLog"/> for logging telemetry.
    /// </summary>
    public ITelemetryBlockLog? GetLog(FunctionId functionId)
    {
        if (!_session.IsOptedIn)
            return null;

        return ImmutableInterlocked.GetOrAdd(ref _logs, functionId, functionId => new VisualStudioTelemetryLog(_telemetryLogger, functionId));
    }

    /// <summary>
    /// Returns an aggregating <see cref="ITelemetryLog"/> for logging telemetry.
    /// </summary>
    public ITelemetryBlockLog? GetHistogramLog(FunctionId functionId, double[]? bucketBoundaries)
    {
        if (!_session.IsOptedIn)
            return null;

        return ImmutableInterlocked.GetOrAdd(
            ref _histogramLogs,
            functionId,
            static (functionId, arg) => new AggregatingHistogramLog(arg._session, functionId, arg.bucketBoundaries),
            factoryArgument: (_session, bucketBoundaries));
    }

    public ITelemetryLog? GetCounterLog(FunctionId functionId)
    {
        if (!_session.IsOptedIn)
            return null;

        return ImmutableInterlocked.GetOrAdd(
            ref _counterLogs,
            functionId,
            static (functionId, session) => new AggregatingCounterLog(session, functionId),
            factoryArgument: _session);
    }

    /// <summary>
    /// Flushes all telemetry logs
    /// </summary>
    public void Flush()
    {
        if (!_session.IsOptedIn)
            return;

        foreach (var log in _histogramLogs.Values)
        {
            log.Flush();
        }

        foreach (var log in _counterLogs.Values)
        {
            log.Flush();
        }
    }
}
