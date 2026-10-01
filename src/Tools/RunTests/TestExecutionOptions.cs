// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace TestRunner.RunTests
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
}
