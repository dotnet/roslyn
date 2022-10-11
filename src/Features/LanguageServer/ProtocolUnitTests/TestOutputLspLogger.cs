// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;
internal class TestOutputLspLogger : ILspServiceLogger
{
    private readonly ITestOutputHelper _testOutputHelper;
    public TestOutputLspLogger(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }
    public void LogEndContext(string message, params object[] @params) => _testOutputHelper.WriteLine($"[End]{message}", @params);

    public void LogError(string message, params object[] @params) => _testOutputHelper.WriteLine($"[Error]{message}", @params);

    public void LogException(Exception exception, string? message = null, params object[] @params) => _testOutputHelper.WriteLine(format: $"[Exception]{message}{Environment.NewLine}{exception}", @params);

    public void LogInformation(string message, params object[] @params) => _testOutputHelper.WriteLine($"[Info]{message}", @params);

    public void LogStartContext(string message, params object[] @params) => _testOutputHelper.WriteLine(format: $"[Start]{message}", @params);

    public void LogWarning(string message, params object[] @params) => _testOutputHelper.WriteLine($"[Warning]{message}", @params);
}
