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
        var servers = connectionManager.GetStartedServers();
        if (servers.IsEmpty)
        {
            // If there are no started servers, then we should use the fallback logger's log level to determine if logging is enabled.
            return fallbackLogConfiguration.LogLevel <= logLevel;
        }

        foreach (var server in servers)
        {
            try
            {
                if (server.GlobalLogger.IsEnabled(logLevel))
                {
                    return true;
                }
            }
            // Servers can asynchronously start / stop, so by the time we get here a server (and its LSP services) could have been disposed.
            catch (ObjectDisposedException)
            {
            }
        }

        return false;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.None || !IsEnabled(logLevel))
        {
            return;
        }

        var servers = connectionManager.GetStartedServers();
        if (servers.IsEmpty)
        {
            // If there are no started servers, then we should log to the fallback logger.
            _fallbackLogger.Value.Log(logLevel, eventId, state, exception, formatter);
            return;
        }

        foreach (var server in servers)
        {
            try
            {
                server.GlobalLogger.Log(logLevel, eventId, state, exception, formatter);
            }
            catch (Exception ex) when (ex is ObjectDisposedException or ConnectionLostException)
            {
            }
        }
    }
}