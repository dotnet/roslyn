// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;

namespace Microsoft.CodeAnalysis.Tools.Logging
{
    internal class SimpleConsoleLogger : ILogger
    {
        private readonly ITerminal _terminal;
        private readonly LogLevel _logLevel;

        private static readonly ImmutableDictionary<LogLevel, ConsoleColor> _logLevelColorMap = new Dictionary<LogLevel, ConsoleColor>
        {
            [LogLevel.Critical] = ConsoleColor.Red,
            [LogLevel.Error] = ConsoleColor.Red,
            [LogLevel.Warning] = ConsoleColor.Yellow,
            [LogLevel.Information] = ConsoleColor.White,
            [LogLevel.Debug] = ConsoleColor.Gray,
            [LogLevel.Trace] = ConsoleColor.Gray,
            [LogLevel.None] = ConsoleColor.White,
        }.ToImmutableDictionary();

        public SimpleConsoleLogger(IConsole console, LogLevel logLevel)
        {
            _terminal = console.GetTerminal();
            _logLevel = logLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var messageColor = _logLevelColorMap[logLevel];
            _terminal.ForegroundColor = messageColor;

            var message = formatter(state, exception);
            _terminal.Out.WriteLine($"  {message}");

            _terminal.ResetColor();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return (int)logLevel >= (int)_logLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }
    }
}
