// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public class TestOutputLogger : ILogger
{
    private readonly ITestOutputHelper _testOutputHelper;
    public readonly ILoggerFactory Factory;

    public TestOutputLogger(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        Factory = new LoggerFactory(new[] { new TestLoggerProvider(this) });
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return new NoOpDisposable();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _testOutputHelper.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}][{logLevel}]{formatter(state, exception)}");
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
