// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class TestOutputLoggerProvider(ITestOutputHelper testOutputHelper) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new TestOutputLogger(testOutputHelper, categoryName);
    }

    private sealed class TestOutputLogger(ITestOutputHelper testOutputHelper, string categoryName) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return new NoOpDisposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            testOutputHelper.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] [{logLevel}] [{categoryName}] {formatter(state, exception)}");

            if (exception is not null)
                testOutputHelper.WriteLine(exception.ToString());
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    public void Dispose()
    {
    }
}
