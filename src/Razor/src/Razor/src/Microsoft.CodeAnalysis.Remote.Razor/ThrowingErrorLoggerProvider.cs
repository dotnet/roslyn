// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.Razor.Logging;

namespace Microsoft.CodeAnalysis.Remote.Razor;

/// <summary>
/// An error logger provider that provides a logger that simple throws an exception for any LogError call.
/// </summary>
internal class ThrowingErrorLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return Logger.Instance;
    }

    private class Logger : ILogger
    {
        public static readonly Logger Instance = new();

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == LogLevel.Error;
        }

        public void Log(LogLevel logLevel, string message, Exception? exception)
        {
            if (logLevel != LogLevel.Error)
            {
                return;
            }

            if (exception is not null)
            {
                throw exception;
            }

            throw new InvalidOperationException(message);
        }
    }
}
