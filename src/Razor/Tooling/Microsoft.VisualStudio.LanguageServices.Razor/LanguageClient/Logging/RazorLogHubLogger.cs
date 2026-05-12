// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.VisualStudio.Razor.Logging;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Logging;

internal sealed class RazorLogHubLogger(string categoryName, RazorLogHubTraceProvider traceProvider) : ILogger
{
    private readonly string _categoryName = categoryName;
    private readonly RazorLogHubTraceProvider _traceProvider = traceProvider;

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log(LogLevel logLevel, string message, Exception? exception)
    {
        if (!_traceProvider.TryGetTraceSource(out var traceSource))
        {
            // We can't log if there is no trace source to log to
            return;
        }

        switch (logLevel)
        {
            case LogLevel.Information:
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.None:
                traceSource.TraceEvent(TraceEventType.Information, id: 0, "[{0}] {1}", _categoryName, message);
                break;
            case LogLevel.Warning:
                traceSource.TraceEvent(TraceEventType.Warning, id: 0, "[{0}] {1}", _categoryName, message);
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                traceSource.TraceEvent(TraceEventType.Error, id: 0, "[{0}] {1} {2}", _categoryName, message, exception!);
                break;
        }
    }
}
