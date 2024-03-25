// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry;

internal sealed class VisualStudioTelemetryLogManager
{
    private readonly TelemetrySession _session;
    private readonly ILogger _telemetryLogger;

    private ImmutableDictionary<FunctionId, VisualStudioTelemetryLog> _logs = ImmutableDictionary<FunctionId, VisualStudioTelemetryLog>.Empty;

    public VisualStudioTelemetryLogManager(TelemetrySession session, ILogger telemetryLogger)
    {
        _session = session;
        _telemetryLogger = telemetryLogger;
    }

    public ITelemetryLog? GetLog(FunctionId functionId)
    {
        if (!_session.IsOptedIn)
            return null;

        return ImmutableInterlocked.GetOrAdd(ref _logs, functionId, functionId => new VisualStudioTelemetryLog(_telemetryLogger, functionId));
    }
}
