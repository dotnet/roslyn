// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer;

public static class LanguageServerFatalError
{
    /// <summary>
    /// Use in an exception filter to report an error without catching the exception.
    /// Also logs the error using the provided logger.
    /// </summary>
    /// <returns><see langword="false"/> to avoid catching the exception.</returns>
    [DebuggerHidden]
    internal static bool ReportAndLogAndPropagate(Exception exception, ILogger logger, string logMessage, ErrorSeverity severity = ErrorSeverity.Uncategorized)
    {
        logger.LogError(exception, logMessage);
        FatalError.ReportAndPropagate(exception, severity);
        return false;
    }
}
