// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry;

internal sealed class VisualStudioTelemetryLog : ITelemetryLog
{
    private readonly ILogger _telemetryLogger;
    private readonly FunctionId _functionId;

    public VisualStudioTelemetryLog(ILogger telemetryLogger, FunctionId functionId)
    {
        _telemetryLogger = telemetryLogger;
        _functionId = functionId;
    }

    public void Log(KeyValueLogMessage logMessage)
    {
        _telemetryLogger.Log(_functionId, logMessage);
    }

    public IDisposable? LogBlockTime(KeyValueLogMessage logMessage, int minThresholdMs)
    {
        return new TimedTelemetryLogBlock(logMessage, minThresholdMs, telemetryLog: this);
    }
}
