// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Razor.Logging;

namespace Microsoft.AspNetCore.Razor.Test.Common.Logging;

internal partial class TestOutputLogger(TestOutputLoggerProvider provider, string categoryName, LogLevel logLevel = LogLevel.Trace) : ILogger
{
    private readonly TestOutputLoggerProvider _provider = provider;
    private readonly string _categoryName = categoryName;
    private readonly LogLevel _logLevel = logLevel;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel >= _logLevel;

    public void Log(LogLevel logLevel, string message, Exception? exception)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var formattedMessage = LogMessageFormatter.FormatMessage(message, _categoryName, exception);

        try
        {
            _provider.Output.WriteLine(formattedMessage);
        }
        catch (Exception ex)
        {
            // If an exception is thrown while writing a message, throw an AggregateException that includes
            // the message that was being logged, along with the exception that was thrown and any exception
            // that was being logged. This might provide clues to the cause.

            var innerExceptions = new List<Exception>
            {
                ex
            };

            // Were we logging an exception? If so, add that too.
            if (exception is not null)
            {
                innerExceptions.Add(exception);
            }

            var aggregateException = new AggregateException($"An exception occurred while logging: {formattedMessage}", innerExceptions);
            throw aggregateException.Flatten();
        }
    }
}
