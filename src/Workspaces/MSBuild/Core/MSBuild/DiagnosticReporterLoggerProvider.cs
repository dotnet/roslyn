// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class DiagnosticReporterLoggerProvider : ILoggerProvider
{
    private readonly DiagnosticReporter _reporter;

    public DiagnosticReporterLoggerProvider(DiagnosticReporter reporter)
    {
        _reporter = reporter;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new Logger(_reporter, categoryName);
    }

    public void Dispose()
    {
    }

    private sealed class Logger(DiagnosticReporter reporter, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Warning;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // Despite returning IsEnabled somebody might still call us anyways, so filter it out
            if (logLevel < LogLevel.Warning)
                return;

            var kind = logLevel == LogLevel.Warning ? WorkspaceDiagnosticKind.Warning : WorkspaceDiagnosticKind.Failure;
            var message = formatter(state, exception);
            if (!string.IsNullOrEmpty(categoryName))
                message = $"[{categoryName}] {message}";

            // The standard formatters don't actually include the exception, so let's include it ourselves
            if (exception is not null)
                message += Environment.NewLine + exception.ToString();

            reporter.Report(new WorkspaceDiagnostic(kind, message));
        }
    }
}
