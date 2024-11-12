// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace BuildValidator
{
    file class DemoLogger : ILogger
    {
        private const int IndentIncrement = 2;

        private sealed class Scope : IDisposable
        {
            private readonly DemoLogger _demoLogger;

            public Scope(DemoLogger demoLogger)
            {
                _demoLogger = demoLogger;
                _demoLogger._indent += IndentIncrement;
            }

            public void Dispose()
            {
                _demoLogger._indent -= IndentIncrement;
            }
        }

        private int _indent;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            LogCore(state?.ToString());
            return new Scope(this);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => LogCore(formatter(state, exception));

        private void LogCore(string? message)
        {
            Console.Write(new string(' ', _indent));
            Console.WriteLine(message);
        }
    }

    internal sealed class DemoLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new DemoLogger();

        public void Dispose()
        {
        }
    }

    internal sealed class EmptyLogger : ILogger, IDisposable
    {
        public static EmptyLogger Instance { get; } = new EmptyLogger();

        public void Dispose()
        {
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => this;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }
}
