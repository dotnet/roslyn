// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using TelemetryLogLevel = Microsoft.CodeAnalysis.Internal.Log.LogLevel;

namespace Microsoft.CodeAnalysis.LanguageServer.Logging;

/// <summary>
/// Logs telemetry events only when Trace is enabled. Faults retain their own severity-based filtering.
/// Properties and collection items marked with <see cref="PiiValue"/> are omitted.
/// </summary>
internal sealed class LoggerEventSink(ILogger logger) : IEventSink
{
    public bool IsEnabled(FunctionId functionId)
        => logger.IsEnabled(LogLevel.Trace);

    public void ReportFault(Exception exception, ErrorSeverity severity, bool forceDump)
    {
        var logLevel = severity == ErrorSeverity.Critical ? LogLevel.Critical : LogLevel.Error;
        if (logger.IsEnabled(logLevel))
        {
            logger.Log(logLevel, GetEventId(FunctionId.NonFatalWatson), exception,
                "{FunctionId}: {Severity} fault (ForceDump={ForceDump})", FunctionId.NonFatalWatson, severity, forceDump);
        }
    }

    public void Log(FunctionId functionId, LogMessage logMessage)
    {
        var logLevel = GetLogLevel(logMessage.LogLevel);
        if (logLevel != LogLevel.None && logger.IsEnabled(logLevel))
        {
            logger.Log(logLevel, GetEventId(functionId), "{FunctionId}: {Message}", functionId, GetMessage(logMessage));
        }
    }

    public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
    {
        var logLevel = GetLogLevel(logMessage.LogLevel);
        if (logLevel != LogLevel.None && logger.IsEnabled(logLevel))
        {
            logger.Log(logLevel, GetEventId(functionId), "Start({BlockId}) {FunctionId}: {Message}",
                uniquePairId, functionId, GetMessage(logMessage));
        }
    }

    public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
    {
        var logLevel = GetLogLevel(logMessage.LogLevel);
        if (logLevel != LogLevel.None && logger.IsEnabled(logLevel))
        {
            logger.Log(logLevel, GetEventId(functionId), "End({BlockId}) {FunctionId}: {Message} ({ElapsedMilliseconds}ms, Canceled={Canceled})",
                uniquePairId, functionId, GetMessage(logMessage), delta, cancellationToken.IsCancellationRequested);
        }
    }

    private static string GetMessage(LogMessage logMessage)
    {
        if (logMessage is not KeyValueLogMessage keyValueMessage)
            return logMessage.GetMessage();

        // KeyValueLogMessage.GetMessage() debug-asserts against '|', '=' and ',' in values.
        // Format properties directly because telemetry can include JSON containing these characters.
        using var _ = PooledStringBuilder.GetInstance(out var builder);
        foreach (var (name, value) in keyValueMessage.Properties)
        {
            if (value is PiiValue)
                continue;

            if (builder.Length > 0)
                builder.Append('|');

            builder.Append(name).Append('=');
            if (value is IEnumerable<object> items)
            {
                var first = true;
                foreach (var item in items)
                {
                    if (item is PiiValue)
                        continue;

                    if (!first)
                        builder.Append(',');

                    builder.Append(item);
                    first = false;
                }
            }
            else
            {
                builder.Append(value);
            }
        }

        return builder.ToString();
    }

    private static EventId GetEventId(FunctionId functionId)
        => new((int)functionId, functionId.ToString());

    private static LogLevel GetLogLevel(TelemetryLogLevel logLevel)
        => logLevel switch
        {
            // Routine telemetry is opt-in through Trace, even when the event itself is informational.
            TelemetryLogLevel.Trace or TelemetryLogLevel.Debug or TelemetryLogLevel.Information => LogLevel.Trace,
            TelemetryLogLevel.Warning => LogLevel.Warning,
            TelemetryLogLevel.Error => LogLevel.Error,
            TelemetryLogLevel.Critical => LogLevel.Critical,
            TelemetryLogLevel.None => LogLevel.None,
            _ => throw ExceptionUtilities.UnexpectedValue(logLevel),
        };
}
