// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.LanguageServer;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Logging;

/// <summary>
/// Implements an ILogger that seamlessly switches from a fallback logger
/// to LSP log messages as soon as the server initializes.
/// </summary>
internal sealed class LspLogMessageLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ILogger _fallbackLogger;

    public LspLogMessageLogger(string categoryName, ILoggerFactory fallbackLoggerFactory)
    {
        _categoryName = categoryName;
        _fallbackLogger = fallbackLoggerFactory.CreateLogger(categoryName);
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var server = LanguageServerHost.Instance;
        if (server == null)
        {
            // If the language server has not been initialized yet, log using the fallback logger.
            _fallbackLogger.Log(logLevel, eventId, state, exception, formatter);
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

        if (message != null && logLevel != LogLevel.None)
        {
            message = $"[{_categoryName}] {message}";
            var _ = server.GetRequiredLspService<IClientLanguageServerManager>().SendNotificationAsync(Methods.WindowLogMessageName, new LogMessageParams()
            {
                Message = message,
                MessageType = logLevel switch
                {
                    LogLevel.Trace => MessageType.Log,
                    LogLevel.Debug => MessageType.Log,
                    LogLevel.Information => MessageType.Info,
                    LogLevel.Warning => MessageType.Warning,
                    LogLevel.Error => MessageType.Error,
                    LogLevel.Critical => MessageType.Error,
                    _ => throw new InvalidOperationException($"Unexpected logLevel argument {logLevel}"),
                }
            }, CancellationToken.None);
        }
    }
}
