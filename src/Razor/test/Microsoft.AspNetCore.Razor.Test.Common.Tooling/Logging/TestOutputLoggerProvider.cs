// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Razor.Logging;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.Test.Common.Logging;

internal class TestOutputLoggerProvider(ITestOutputHelper output, LogLevel logLevel = LogLevel.Trace) : ILoggerProvider
{
    private readonly ITestOutputHelper _output = output;
    private readonly LogLevel _logLevel = logLevel;

    public ITestOutputHelper Output => _output;

    public ILogger CreateLogger(string categoryName)
        => new TestOutputLogger(this, categoryName, _logLevel);
}
