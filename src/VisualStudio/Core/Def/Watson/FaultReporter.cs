// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.ErrorReporting;

internal static class FaultReporter
{
    public static void InitializeFatalErrorHandlers()
    {
        FatalError.ErrorReporterHandler handler = ReportFault;
        FatalError.SetHandlers(handler, nonFatalHandler: handler);
        FatalError.CopyHandlersTo(typeof(Compilation).Assembly);
    }

    /// <summary>
    /// Report Non-Fatal Watson for a given unhandled exception.
    /// </summary>
    /// <param name="exception">Exception that triggered this non-fatal error</param>
    /// <param name="forceDump">Force a dump to be created, even if the telemetry system is not
    /// requesting one; we will still do a client-side limit to avoid sending too much at once.</param>
    public static void ReportFault(Exception exception, ErrorSeverity severity, bool forceDump)
        => RoslynTelemetry.Current.ReportFault(exception, severity, forceDump);
}
