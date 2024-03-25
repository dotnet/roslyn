// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.MSBuild;

internal class DiagnosticReporterLoggerProvider : ILoggerProvider
{
    private readonly DiagnosticReporter _reporter;

    private DiagnosticReporterLoggerProvider(DiagnosticReporter reporter)
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

    public static ILoggerFactory CreateLoggerFactoryForDiagnosticReporter(DiagnosticReporter reporter)
    {
        // Note: it's important we set MinLevel here, or otherwise we'll still get called in Log() for things below LogLevel.Warning.
        return new LoggerFactory(
            [new DiagnosticReporterLoggerProvider(reporter)],
            new LoggerFilterOptions() { MinLevel = LogLevel.Warning });
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
            var kind = logLevel == LogLevel.Warning ? WorkspaceDiagnosticKind.Warning : WorkspaceDiagnosticKind.Failure;
            var message = formatter(state, exception);
            if (!string.IsNullOrEmpty(categoryName))
                message = $"[{categoryName}] {message}";

            reporter.Report(new WorkspaceDiagnostic(kind, message));
        }
    }
}
