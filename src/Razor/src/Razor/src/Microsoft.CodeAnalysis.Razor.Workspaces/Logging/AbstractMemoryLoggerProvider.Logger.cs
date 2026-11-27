// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.CodeAnalysis.Razor.Logging;

internal partial class AbstractMemoryLoggerProvider
{
    private class Logger(Buffer buffer, string categoryName) : ILogger
    {
        private readonly Buffer _buffer = buffer;
        private readonly string _categoryName = categoryName;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log(LogLevel logLevel, string message, Exception? exception)
        {
            var formattedMessage = LogMessageFormatter.FormatMessage(message, _categoryName, exception);
            _buffer.Append(formattedMessage);
        }
    }
}
