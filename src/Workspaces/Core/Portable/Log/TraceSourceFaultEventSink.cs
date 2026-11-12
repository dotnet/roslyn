// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Writes faults to a host-provided trace source without subscribing to ordinary events.
/// </summary>
internal sealed class TraceSourceFaultEventSink(TraceSource traceSource) : IEventSink
{
    public bool IsEnabled(FunctionId functionId)
        => false;

    public void ReportFault(Exception exception, ErrorSeverity severity, bool forceDump)
    {
        using var currentProcess = Process.GetCurrentProcess();
        traceSource.TraceEvent(TraceEventType.Error, 1, $"[{currentProcess.ProcessName}:{currentProcess.Id}] Unexpected exception: {exception}");
    }

    public void Log(FunctionId functionId, LogMessage logMessage)
    {
    }

    public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
    {
    }

    public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
    {
    }
}
