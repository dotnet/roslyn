// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Remote.Razor.Formatting;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal sealed class FormattingLoggerFactoryAdapter(IFormattingLogger? formattingLogger) : ILoggerFactory
{
    private IFormattingLogger? _formattingLogger = formattingLogger;

    public void SetFormattingLogger(IFormattingLogger? formattingLogger)
        => _formattingLogger = formattingLogger;

    public void AddLoggerProvider(ILoggerProvider provider)
        => throw new NotSupportedException("Adding logger providers is not supported by the dotnet format adapter.");

    public ILogger GetOrCreateLogger(string categoryName)
        => new FormattingLoggerAdapter(this, categoryName);

    private sealed class FormattingLoggerAdapter(FormattingLoggerFactoryAdapter loggerFactory, string categoryName) : ILogger
    {
        private readonly FormattingLoggerFactoryAdapter _loggerFactory = loggerFactory;
        private readonly string _categoryName = categoryName;

        public bool IsEnabled(LogLevel logLevel)
            => _loggerFactory._formattingLogger is not null && logLevel != LogLevel.None;

        public void Log(LogLevel logLevel, string message, Exception? exception)
        {
            var formattingLogger = _loggerFactory._formattingLogger;
            if (formattingLogger is null)
            {
                return;
            }

            formattingLogger.LogMessage($"[{logLevel}] {_categoryName}: {message}");

            if (exception is not null)
            {
                formattingLogger.LogMessage(exception.ToString());
            }
        }
    }
}
