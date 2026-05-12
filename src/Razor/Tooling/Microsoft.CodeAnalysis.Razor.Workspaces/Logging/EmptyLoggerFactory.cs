// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.CodeAnalysis.Razor.Logging;

internal sealed class EmptyLoggerFactory : ILoggerFactory
{
    public static ILoggerFactory Instance { get; } = new EmptyLoggerFactory();

    private EmptyLoggerFactory()
    {
    }

    public void AddLoggerProvider(ILoggerProvider provider)
    {
        // This is an empty logger factory. Do nothing.
    }

    public ILogger GetOrCreateLogger(string categoryName)
    {
        return Logger.Instance;
    }

    private sealed class Logger : ILogger
    {
        public static readonly ILogger Instance = new Logger();

        private Logger()
        {
        }

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log(LogLevel logLevel, string message, Exception? exception)
        {
            // This is an empty logger. Do nothing.
        }
    }
}
