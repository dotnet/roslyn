// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace TestRunner.RunTests
{
    /// <summary>
    /// The actual results from running the xunit tests.
    /// </summary>
    /// <remarks>
    /// The difference between <see cref="TestResultInfo"/> and <see cref="TestResult"/> is the former
    /// is specifically for the actual test execution results while the latter can contain extra metadata
    /// about the results. For example whether it was cached, or had diagnostic output.
    /// </remarks>
    internal readonly struct TestResultInfo
    {
        internal int ExitCode { get; }
        internal TimeSpan Elapsed { get; }
        internal string StandardOutput { get; }
        internal string ErrorOutput { get; }

        /// <summary>
        /// Path to the XML results file.
        /// </summary>
        internal string? ResultsFilePath { get; }

        /// <summary>
        /// Path to the HTML results file if HTML output is enabled, otherwise, <see langword="null"/>.
        /// </summary>
        internal string? HtmlResultsFilePath { get; }

        internal TestResultInfo(int exitCode, string? resultsFilePath, string? htmlResultsFilePath, TimeSpan elapsed, string standardOutput, string errorOutput)
        {
            ExitCode = exitCode;
            ResultsFilePath = resultsFilePath;
            HtmlResultsFilePath = htmlResultsFilePath;
            Elapsed = elapsed;
            StandardOutput = standardOutput;
            ErrorOutput = errorOutput;
        }
    }
}
