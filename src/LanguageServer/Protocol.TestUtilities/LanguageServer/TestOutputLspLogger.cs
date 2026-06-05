// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Xunit.Abstractions;

namespace Roslyn.Test.Utilities;

internal sealed class TestOutputLspLogger : ILspLogger, ILspService
{
    public ITestOutputHelper? TestOutputHelper { private get; set; }

    public TestOutputLspLogger()
    {
    }

    public IDisposable? CreateContext(string context) => null;
    public IDisposable? CreateLanguageContext(string? context) => null;

    public void LogDebug(string message, params object[] @params) => Log("Debug", message, @params);

    public void LogError(string message, params object[] @params) => Log("Error", message, @params);

    public void LogException(Exception exception, string? message = null, params object[] @params)
        => Log("Warning", $"{message}{Environment.NewLine}{exception}", @params);

    public void LogInformation(string message, params object[] @params) => Log("Info", message, @params);

    public void LogWarning(string message, params object[] @params) => Log("Warning", message, @params);

    private void Log(string level, string message, params object[] @params)
    {
        Contract.ThrowIfNull(TestOutputHelper, "TestOutputHelper must be set before logging messages.");
        TestOutputHelper.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}][{level}]{message}", @params);
    }
}
