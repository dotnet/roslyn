// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Razor.Logging;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.IntegrationTests.Logging;

internal class IntegrationTestOutputLoggerProvider(ITestOutputHelper output, LogLevel logLevel = LogLevel.Trace) : ILoggerProvider
{
    private ITestOutputHelper? _output = output;
    private readonly LogLevel _logLevel = logLevel;

    public bool HasOutput => _output is not null;

    public ILogger CreateLogger(string categoryName)
        => new IntegrationTestOutputLogger(this, categoryName, _logLevel);

    public void SetOutput(ITestOutputHelper? output)
    {
        _output = output;
    }

    public void WriteLine(string message)
    {
        _output?.WriteLine(message);
    }
}
