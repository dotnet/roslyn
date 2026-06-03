// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.Logging;

/// <summary>
/// Implements the global MEF <see cref="ILogger"/> by sending messages to all active LSP servers,
/// or to a fallback logger if no LSP servers are active yet.
/// </summary>
internal sealed class GlobalLogMessageLogger(
    string categoryName,
    ILoggerFactory fallbackLoggerFactory,
    LanguageServerConnectionManager connectionManager,
    LogConfiguration fallbackLogConfiguration) : ILogger
{
    private readonly Lazy<ILogger> _fallbackLogger = new(() => fallbackLoggerFactory.CreateLogger(categoryName));

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _fallbackLogger.Value.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel)
    {
        var enabled = false;
        var hasStartedServer = connectionManager.ForEachStartedServer(server =>
        {
            try
            {
                if (server.GlobalLogger.IsEnabled(logLevel))
                {
                    enabled = true;
                    return false;
                }
            }
            catch (ObjectDisposedException)
            {
            }

            return true;
        });

        return hasStartedServer
            ? enabled
            : fallbackLogConfiguration.GetLogLevel() <= logLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel) || logLevel == LogLevel.None)
        {
            return;
        }

        var logged = false;
        connectionManager.ForEachStartedServer(server =>
        {
            try
            {
                server.GlobalLogger.Log(logLevel, eventId, state, exception, formatter);
                logged = true;
            }
            catch (Exception ex) when (ex is ObjectDisposedException or ConnectionLostException)
            {
            }

            return true;
        });

        if (logged)
            return;

        _fallbackLogger.Value.Log(logLevel, eventId, state, exception, formatter);
    }
}