// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;
internal class TestOutputLspLogger : TextWriter, ILspServiceLogger
{
    private readonly ITestOutputHelper _testOutputHelper;
    public TestOutputLspLogger(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string message)
    {
        Log("Console", message);
    }
    public override void WriteLine(string format, params object[] args)
    {
        Log("Console", format, args);
    }

    public override void Write(char value)
    {
        throw new NotSupportedException("This text writer only supports WriteLine(string) and WriteLine(string, params object[]).");
    }

    public void LogEndContext(string message, params object[] @params) => Log("End", message, @params);

    public void LogError(string message, params object[] @params) => Log("Error", message, @params);

    public void LogException(Exception exception, string? message = null, params object[] @params)
        => Log("Warning", $"{message}{Environment.NewLine}{exception}", @params);

    public void LogInformation(string message, params object[] @params) => Log("Info", message, @params);

    public void LogStartContext(string message, params object[] @params) => Log("Start", message, @params);

    public void LogWarning(string message, params object[] @params) => Log("Warning", message, @params);

    private void Log(string level, string message, params object[] @params)
        => _testOutputHelper.WriteLine($"[{DateTime.UtcNow:hh:mm:ss.fff}][{level}]{message}", @params);
}
