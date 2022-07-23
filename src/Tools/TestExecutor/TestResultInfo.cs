// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TestExecutor;

/// <summary>
/// The actual results from running the xunit tests.
/// </summary>
/// <remarks>
/// The difference between <see cref="TestResultInfo"/>  and <see cref="TestResult"/> is the former 
/// is specifically for the actual test execution results while the latter can contain extra metadata
/// about the results.  For example whether it was cached, or had diagnostic, output, etc ...
/// </remarks>
public readonly struct TestResultInfo
{
    public int ExitCode { get; }
    public TimeSpan Elapsed { get; }
    public string StandardOutput { get; }
    public string ErrorOutput { get; }
    public bool RanToCompletion { get; }

    internal TestResultInfo(int exitCode, TimeSpan elapsed, string standardOutput, string errorOutput, bool ranToCompletion)
    {
        ExitCode = exitCode;
        Elapsed = elapsed;
        StandardOutput = standardOutput;
        ErrorOutput = errorOutput;
        RanToCompletion = ranToCompletion;
    }
}
