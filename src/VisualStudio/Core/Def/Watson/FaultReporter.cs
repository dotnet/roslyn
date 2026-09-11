// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.ErrorReporting;

internal static class FaultReporter
{
    private static readonly object _guard = new();
    private static ImmutableArray<TraceSource> s_loggers = [];

    public static void InitializeFatalErrorHandlers()
    {
        FatalError.ErrorReporterHandler handler = ReportFault;
        FatalError.SetHandlers(handler, nonFatalHandler: handler);
        FatalError.CopyHandlersTo(typeof(Compilation).Assembly);
    }

    public static void RegisterLogger(TraceSource logger)
    {
        lock (_guard)
        {
            s_loggers = s_loggers.Add(logger);
        }
    }

    public static void UnregisterLogger(TraceSource logger)
    {
        lock (_guard)
        {
            s_loggers = s_loggers.Remove(logger);
        }
    }

    /// <summary>
    /// Report Non-Fatal Watson for a given unhandled exception.
    /// </summary>
    /// <param name="exception">Exception that triggered this non-fatal error</param>
    /// <param name="forceDump">Force a dump to be created, even if the telemetry system is not
    /// requesting one; we will still do a client-side limit to avoid sending too much at once.</param>
    public static void ReportFault(Exception exception, ErrorSeverity severity, bool forceDump)
    {
        try
        {
            if (exception is OperationCanceledException { InnerException: { } oceInnerException })
            {
                ReportFault(oceInnerException, severity, forceDump);
                return;
            }

            if (exception is AggregateException aggregateException)
            {
                // We (potentially) have multiple exceptions; let's just report each of them
                foreach (var innerException in aggregateException.Flatten().InnerExceptions)
                    ReportFault(innerException, severity, forceDump);

                return;
            }

            var currentProcess = Process.GetCurrentProcess();

            // write the exception to a log file:
            var logMessage = $"[{currentProcess.ProcessName}:{currentProcess.Id}] Unexpected exception: {exception}";
            foreach (var logger in s_loggers)
            {
                logger.TraceEvent(TraceEventType.Error, 1, logMessage);
            }

            RoslynTelemetry.Current.ReportFault(exception, severity, forceDump);
        }
        catch (OutOfMemoryException)
        {
            FailFast.OnFatalException(exception);
        }
        catch (Exception e)
        {
            FailFast.OnFatalException(e);
        }
    }
}
