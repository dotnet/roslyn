// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using RunTestsUtils;

namespace RunTests
{
    internal readonly struct TestExecutionOptions
    {
        internal string DotnetFilePath { get; }
        internal string TestResultsDirectory { get; }
        internal string? TestFilter { get; }
        internal bool IncludeHtml { get; }
        internal bool Retry { get; }
        internal bool CollectDumps { get; }

        internal TestExecutionOptions(string dotnetFilePath, string testResultsDirectory, string? testFilter, bool includeHtml, bool retry, bool collectDumps)
        {
            DotnetFilePath = dotnetFilePath;
            TestResultsDirectory = testResultsDirectory;
            TestFilter = testFilter;
            IncludeHtml = includeHtml;
            Retry = retry;
            CollectDumps = collectDumps;
        }
    }

    /// <summary>
    /// The actual results from running the xunit tests.
    /// </summary>
    /// <remarks>
    /// The difference between <see cref="TestResultInfo"/>  and <see cref="TestResult"/> is the former 
    /// is specifically for the actual test execution results while the latter can contain extra metadata
    /// about the results.  For example whether it was cached, or had diagnostic, output, etc ...
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

        internal TestResultInfo(int exitCode, string? resultsFilePath, TimeSpan elapsed, string standardOutput, string errorOutput)
        {
            ExitCode = exitCode;
            ResultsFilePath = resultsFilePath;
            Elapsed = elapsed;
            StandardOutput = standardOutput;
            ErrorOutput = errorOutput;
        }
    }

    internal readonly struct TestResult
    {
        internal TestResultInfo TestResultInfo { get; }
        internal string AssemblyPath { get; }
        internal string CommandLine { get; }
        internal string? Diagnostics { get; }

        /// <summary>
        /// Collection of processes the runner explicitly ran to get the result.
        /// </summary>
        internal ImmutableArray<ProcessResult> ProcessResults { get; }

        internal string DisplayName => Path.GetFileName(AssemblyPath);
        internal bool Succeeded => ExitCode == 0;
        internal int ExitCode => TestResultInfo.ExitCode;
        internal TimeSpan Elapsed => TestResultInfo.Elapsed;
        internal string StandardOutput => TestResultInfo.StandardOutput;
        internal string ErrorOutput => TestResultInfo.ErrorOutput;
        internal string? ResultsDisplayFilePath => TestResultInfo.ResultsFilePath;

        internal TestResult(string assemblyPath, TestResultInfo testResultInfo, string commandLine, ImmutableArray<ProcessResult> processResults = default, string? diagnostics = null)
        {
            AssemblyPath = assemblyPath;
            TestResultInfo = testResultInfo;
            CommandLine = commandLine;
            ProcessResults = processResults.IsDefault ? ImmutableArray<ProcessResult>.Empty : processResults;
            Diagnostics = diagnostics;
        }
    }
}
