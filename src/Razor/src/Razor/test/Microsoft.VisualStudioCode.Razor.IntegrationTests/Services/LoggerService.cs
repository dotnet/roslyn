// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Abstractions;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;

/// <summary>
/// Service for logging test output.
/// </summary>
public class LoggerService(ITestOutputHelper output)
{
    /// <summary>
    /// Logs a timestamped message to the test output.
    /// </summary>
    public void Log(string message)
    {
        var timestampedMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        output.WriteLine(timestampedMessage);
    }
}
