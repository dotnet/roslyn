// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

/// <summary>
/// ILogger implementation that logs via the razor/log LSP method
/// </summary>
/// <remarks>
/// The handler for this custom log message is implemented in the C# extension and is responsible for writing the message to the output window.
/// </remarks>
internal class LspLogger(string categoryName, RazorClientServerManagerProvider razorClientServerManagerProvider) : ILogger
{
    private readonly string _categoryName = categoryName;
    private readonly RazorClientServerManagerProvider _razorClientServerManagerProvider = razorClientServerManagerProvider;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log(LogLevel logLevel, string message, Exception? exception)
    {
        if (_razorClientServerManagerProvider.ClientLanguageServerManager is not { } clientLanguageServerManager)
        {
            return;
        }

        if (!IsEnabled(logLevel))
        {
            return;
        }

        var messageType = logLevel switch
        {
            LogLevel.Critical => MessageType.Error,
            LogLevel.Error => MessageType.Error,
            LogLevel.Warning => MessageType.Warning,
            LogLevel.Information => MessageType.Info,
            LogLevel.Debug => MessageType.Log,
            LogLevel.Trace => MessageType.Log,
            _ => throw new NotImplementedException(),
        };

        var formattedMessage = LogMessageFormatter.FormatMessage(message, _categoryName, exception, includeTimeStamp: false);

        var @params = new LogMessageParams
        {
            MessageType = messageType,
            Message = formattedMessage,
        };

        clientLanguageServerManager.SendNotificationAsync("razor/log", @params, CancellationToken.None).Forget();
    }
}
