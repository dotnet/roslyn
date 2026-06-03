// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.Logging;

/// <summary>
/// Implements an <see cref="ILogger"/> that sends log messages to the client via LSP's window/logMessage notification.
/// </summary>
internal sealed partial class LspLogMessageLogger(
    string categoryName,
    IClientLanguageServerManager clientLanguageServerManager,
    LogConfiguration logConfiguration,
    IExternalScopeProvider? externalScopeProvider) : ILogger
{
    private readonly IExternalScopeProvider? _externalScopeProvider = externalScopeProvider;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _externalScopeProvider?.Push(state);
    public bool IsEnabled(LogLevel logLevel) => logConfiguration.GetLogLevel() <= logLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel) || logLevel == LogLevel.None)
        {
            return;
        }

        var message = formatter(state, exception);

        // HACK: work around https://github.com/dotnet/runtime/issues/67597: the formatter function we passed the exception to completely ignores the exception,
        // we'll add an exception message back in. If we didn't have a message, we'll just replace it with the exception text.
        if (exception != null)
        {
            var exceptionString = exception.ToString();
            if (message == "[null]") // https://github.com/dotnet/runtime/blob/013ca673f6316dbbe71c7b327d7b8fa41cf8c992/src/libraries/Microsoft.Extensions.Logging.Abstractions/src/FormattedLogValues.cs#L19
                message = exceptionString;
            else
                message += " " + exceptionString;
        }

        string messagePrefix = "";

        _externalScopeProvider?.ForEachScope((scope, _) =>
        {
            if (scope is LspLoggingScope lspLoggingScope)
            {
                if (lspLoggingScope.Context is not null)
                {
                    messagePrefix += $"[{lspLoggingScope.Context}] ";
                }

                if (lspLoggingScope.Language is not null && lspLoggingScope.Language != LanguageServerConstants.DefaultLanguageName)
                {
                    messagePrefix += $"[{lspLoggingScope.Language}] ";
                }
            }
        }, state);

        if (!string.IsNullOrEmpty(categoryName))
        {
            messagePrefix += $"[{categoryName}]";
        }

        var logMessage = string.IsNullOrEmpty(messagePrefix) ? message : $"{messagePrefix} {message}";

        try
        {
            var _ = clientLanguageServerManager.SendNotificationAsync(Methods.WindowLogMessageName, new LogMessageParams()
            {
                Message = logMessage,
                MessageType = LogLevelToMessageType(logLevel),
            }, CancellationToken.None);
        }
        catch (Exception ex) when (ex is ObjectDisposedException or ConnectionLostException)
        {
            // It is entirely possible that we're shutting down and the connection is lost while we're trying to send a log notification
            // as this runs outside of the guaranteed ordering in the queue. We can safely ignore this exception.
        }
    }

    private static MessageType LogLevelToMessageType(LogLevel logLevel)
    {
        return logLevel switch
        {
            // Count "Trace" as "Debug", as right now the VS Code LSP client doesn't have a concept of "trace", and using a generic "Log" puts no severity at all which is even more confusing.
            LogLevel.Trace => MessageType.Debug,
            LogLevel.Debug => MessageType.Debug,
            LogLevel.Information => MessageType.Info,
            LogLevel.Warning => MessageType.Warning,
            LogLevel.Error => MessageType.Error,
            LogLevel.Critical => MessageType.Error,
            _ => throw ExceptionUtilities.UnexpectedValue(logLevel),
        };
    }
}
