// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace TestRunner.RunTests
{
    internal readonly struct TestResult
    {
        internal TestResultInfo TestResultInfo { get; }
        internal WorkItemInfo WorkItemInfo { get; }
        internal string CommandLine { get; }
        internal string? Diagnostics { get; }

        /// <summary>
        /// Collection of processes the runner explicitly ran to get the result.
        /// </summary>
        internal ImmutableArray<ProcessResult> ProcessResults { get; }

        internal string DisplayName => WorkItemInfo.DisplayName;
        internal bool Succeeded => ExitCode == 0;
        internal int ExitCode => TestResultInfo.ExitCode;
        internal TimeSpan Elapsed => TestResultInfo.Elapsed;
        internal string StandardOutput => TestResultInfo.StandardOutput;
        internal string ErrorOutput => TestResultInfo.ErrorOutput;
        internal string? ResultsDisplayFilePath => TestResultInfo.HtmlResultsFilePath ?? TestResultInfo.ResultsFilePath;

        internal TestResult(WorkItemInfo workItemInfo, TestResultInfo testResultInfo, string commandLine, ImmutableArray<ProcessResult> processResults = default, string? diagnostics = null)
        {
            WorkItemInfo = workItemInfo;
            TestResultInfo = testResultInfo;
            CommandLine = commandLine;
            ProcessResults = processResults.IsDefault ? ImmutableArray<ProcessResult>.Empty : processResults;
            Diagnostics = diagnostics;
        }
    }
}
