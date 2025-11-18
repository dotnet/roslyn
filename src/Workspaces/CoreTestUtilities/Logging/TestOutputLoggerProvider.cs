// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class TestOutputLoggerProvider(ITestOutputHelper testOutputHelper) : ILoggerProvider
{
    /// <summary>
    /// The <see cref="ITestOutputHelper" /> given to us from xUnit. This is nulled out once we dispose this logger provider;
    /// xUnit will abort a test run if something writes to this when a test isn't running; that's helpful for debugging,
    /// but if a test fails we might still have asynchronous work logging in the background that didn't cleanly shut down. We don't want
    /// an entire test run failing for that.
    /// </summary>
    private ITestOutputHelper? _testOutputHelper = testOutputHelper;

    public ILogger CreateLogger(string categoryName)
    {
        return new TestOutputLogger(this, categoryName);
    }

    private sealed class TestOutputLogger(TestOutputLoggerProvider loggerProvider, string categoryName) : ILogger
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
            loggerProvider._testOutputHelper?.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}] [{logLevel}] [{categoryName}] {formatter(state, exception)}");

            if (exception is not null)
                loggerProvider._testOutputHelper?.WriteLine(exception.ToString());
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
        _testOutputHelper = null;
    }
}
