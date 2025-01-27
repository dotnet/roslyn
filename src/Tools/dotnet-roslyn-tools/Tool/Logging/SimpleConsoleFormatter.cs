// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using ConsoleColors = (string Foreground, string Background);

namespace Microsoft.RoslynTools.Logging;

internal sealed class SimplerConsoleFormatter() : ConsoleFormatter("simpler")
{
    private static readonly Lazy<bool> s_lazyDisableColors = new(()
        => Console.IsOutputRedirected || Environment.GetEnvironmentVariable("NO_COLOR") is not null);

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (logEntry.Exception == null && message == null)
        {
            return;
        }

        // We extract most of the work into a non-generic method to save code size. If this was left in the generic
        // method, we'd get generic specialization for all TState parameters, but that's unnecessary.
        WriteInternal(textWriter, message, logEntry.LogLevel, logEntry.Exception?.ToString());
    }

    private static void WriteInternal(
        TextWriter textWriter,
        string message,
        LogLevel logLevel,
        string? exception)
    {
        if (s_lazyDisableColors.Value)
        {
            WriteMessage(textWriter, message, exception);
        }
        else
        {
            var logLevelColors = logLevel switch
            {
                LogLevel.Trace => NormalConsoleColors,
                LogLevel.Debug => NormalConsoleColors,
                LogLevel.Information => NormalConsoleColors,
                LogLevel.Warning => WarningConsoleColors,
                LogLevel.Error => ErrorConsoleColors,
                LogLevel.Critical => CriticalConsoleColors,
                _ => DefaultConsoleColors
            };
            WriteColoredMessage(textWriter, message, exception, logLevelColors);
        }
    }

    private static void WriteMessage(TextWriter textWriter, string message, string? exception)
    {
        textWriter.Write(message);
        if (exception != null)
        {
            textWriter.Write(Environment.NewLine);
            textWriter.Write(exception);
        }
        textWriter.Write(Environment.NewLine);
    }

    public static void WriteColoredMessage(TextWriter textWriter, string message, string? exception, ConsoleColors colors)
    {
        // Order: backgroundcolor, foregroundcolor, Message, reset foregroundcolor, reset backgroundcolor
        textWriter.Write(colors.Background);
        textWriter.Write(colors.Foreground);

        textWriter.Write(message);

        if (exception != null)
        {
            textWriter.Write(Environment.NewLine);
            textWriter.Write(exception);
        }

        textWriter.Write(DefaultConsoleColors.Foreground); // reset to default foreground color
        textWriter.Write(DefaultConsoleColors.Background); // reset to the background color

        textWriter.Write(Environment.NewLine);
    }

    internal static readonly ConsoleColors DefaultConsoleColors = ("\u001b[39m\u001b[22m", "\u001b[49m"); // (default, default)
    internal static readonly ConsoleColors NormalConsoleColors = ("\u001b[37m", "\u001b[40m"); // (grey, black)
    internal static readonly ConsoleColors WarningConsoleColors = ("\u001b[1m\u001b[33m", "\u001b[40m"); // (yellow, black)
    internal static readonly ConsoleColors ErrorConsoleColors = ("\u001b[1m\u001b[31m", "\u001b[40m"); // (red, black)
    internal static readonly ConsoleColors CriticalConsoleColors = ("\u001b[1m\u001b[37m", "\u001b[41m"); // (white, darkred)
}
