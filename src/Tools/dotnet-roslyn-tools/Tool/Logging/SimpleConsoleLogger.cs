// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Rendering;
using Microsoft.Extensions.Logging;

namespace Microsoft.RoslynTools.Logging;

internal class SimpleConsoleLogger : ILogger
{
    private readonly object _gate = new object();

    private readonly IConsole _console;
    private readonly ITerminal _terminal;
    private readonly LogLevel _minimalLogLevel;
    private readonly LogLevel _minimalErrorLevel;

    private static ImmutableDictionary<LogLevel, AnsiControlCode> LogLevelColorMap => new Dictionary<LogLevel, AnsiControlCode>()
    {
        [LogLevel.Critical] = Ansi.Color.Foreground.Red,
        [LogLevel.Error] = Ansi.Color.Foreground.Red,
        [LogLevel.Warning] = Ansi.Color.Foreground.LightYellow,
        [LogLevel.Information] = Ansi.Color.Foreground.Default,
        [LogLevel.Debug] = Ansi.Color.Foreground.LightGray,
        [LogLevel.Trace] = Ansi.Color.Foreground.LightGray,
        [LogLevel.None] = Ansi.Color.Foreground.Default,
    }.ToImmutableDictionary();

    public SimpleConsoleLogger(IConsole console, LogLevel minimalLogLevel, LogLevel minimalErrorLevel)
    {
        _terminal = console.GetTerminal();
        _console = console;
        _minimalLogLevel = minimalLogLevel;
        _minimalErrorLevel = minimalErrorLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        lock (_gate)
        {
            var message = formatter(state, exception);
            var logToErrorStream = logLevel >= _minimalErrorLevel;
            if (_terminal is null)
            {
                LogToConsole(_console, message, logToErrorStream);
            }
            else
            {
                LogToTerminal(message, logLevel, logToErrorStream);
            }
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return (int)logLevel >= (int)_minimalLogLevel;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return NullScope.Instance;
    }

    private void LogToTerminal(string message, LogLevel logLevel, bool logToErrorStream)
    {
        var messageColor = LogLevelColorMap[logLevel];
        _terminal.Out.Write(messageColor.EscapeSequence);

        LogToConsole(_terminal, message, logToErrorStream);

        _terminal.ResetColor();
    }

    private static void LogToConsole(IConsole console, string message, bool logToErrorStream)
    {
        if (logToErrorStream)
        {
            console.Error.WriteLine(message);
        }
        else
        {
            console.Out.WriteLine(message);
        }
    }
}
