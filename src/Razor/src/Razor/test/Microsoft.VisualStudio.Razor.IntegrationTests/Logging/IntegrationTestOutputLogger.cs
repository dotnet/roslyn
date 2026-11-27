// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Razor.Logging;

namespace Microsoft.VisualStudio.Razor.IntegrationTests.Logging;

internal partial class IntegrationTestOutputLogger(IntegrationTestOutputLoggerProvider provider, string categoryName, LogLevel logLevel = LogLevel.Trace) : ILogger
{
    private readonly IntegrationTestOutputLoggerProvider _provider = provider;
    private readonly string _categoryName = categoryName;
    private readonly LogLevel _logLevel = logLevel;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel >= _logLevel;

    public void Log(LogLevel logLevel, string message, Exception? exception)
    {
        if (!IsEnabled(logLevel) || !_provider.HasOutput)
        {
            return;
        }

        var formattedMessage = LogMessageFormatter.FormatMessage(message, _categoryName, exception);

        try
        {
            _provider.WriteLine(formattedMessage);
        }
        catch (InvalidOperationException ex) when (ex.Message == "There is no currently active test.")
        {
            // Ignore, something is logging a message outside of a test. Other loggers will capture it.
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
