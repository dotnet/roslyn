// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using TestExecutor;

namespace RunTests
{
    internal readonly struct TestResult
    {
        internal TestResultInfo TestResultInfo { get; }
        internal WorkItemInfo WorkItemInfo { get; }
        internal string DisplayName => WorkItemInfo.DisplayName;
        internal bool Succeeded => ExitCode == 0;
        internal int ExitCode => TestResultInfo.ExitCode;
        internal TimeSpan Elapsed => TestResultInfo.Elapsed;
        internal string StandardOutput => TestResultInfo.StandardOutput;
        internal string ErrorOutput => TestResultInfo.ErrorOutput;
        internal string? ResultsDisplayFilePath { get; }
        internal string? HtmlResultsFilePath { get; }

        internal TestResult(WorkItemInfo workItemInfo, TestResultInfo testResultInfo, string? resultsFilePath, string? htmlResultsFilePath)
        {
            WorkItemInfo = workItemInfo;
            TestResultInfo = testResultInfo;
            ResultsDisplayFilePath = resultsFilePath;
            HtmlResultsFilePath = htmlResultsFilePath;
        }
    }
}
