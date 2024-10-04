// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.CodeAnalysis.Telemetry;

/// <summary>
/// Manages creation and obtaining aggregated telemetry logs.
/// </summary>
internal sealed class AggregatingTelemetryLogManager
{
    private readonly TelemetrySession _session;

    private ImmutableDictionary<FunctionId, AggregatingTelemetryLog> _aggregatingLogs = ImmutableDictionary<FunctionId, AggregatingTelemetryLog>.Empty;

    public AggregatingTelemetryLogManager(TelemetrySession session)
    {
        _session = session;
    }

    public ITelemetryLog? GetLog(FunctionId functionId, double[]? bucketBoundaries)
    {
        if (!_session.IsOptedIn)
            return null;

        return ImmutableInterlocked.GetOrAdd(
            ref _aggregatingLogs,
            functionId,
            static (functionId, arg) => new AggregatingTelemetryLog(arg._session, functionId, arg.bucketBoundaries),
            factoryArgument: (_session, bucketBoundaries));
    }

    public void Flush()
    {
        if (!_session.IsOptedIn)
            return;

        foreach (var log in _aggregatingLogs.Values)
        {
            log.Flush();
        }
    }
}
