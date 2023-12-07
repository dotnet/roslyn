// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal sealed class RequestTelemetryScope(string name, RequestTelemetryLogger telemetryLogger)
 : AbstractLspRequestScope(name)
{
    private readonly RequestTelemetryLogger _telemetryLogger = telemetryLogger;
    private RequestTelemetryLogger.Result _result = RequestTelemetryLogger.Result.Succeeded;

    public override void Dispose()
    {
        base.Dispose();

        _telemetryLogger.UpdateTelemetryData(Name, QueuedDuration, RequestDuration, _result);
    }

    public override void RecordCancellation()
    {
        _result = RequestTelemetryLogger.Result.Cancelled;
    }

    public override void RecordException(Exception exception)
    {
        _result = RequestTelemetryLogger.Result.Failed;
    }

    public override void RecordWarning(string message)
    {
        _result = RequestTelemetryLogger.Result.Failed;
    }
}
