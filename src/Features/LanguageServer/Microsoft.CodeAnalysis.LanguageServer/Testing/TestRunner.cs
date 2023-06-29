// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

[Export, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class TestRunner(ILoggerFactory loggerFactory)
{
    /// <summary>
    /// TODO - localize messages. https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1799066/
    /// </summary>
    private const string StageName = "Running tests...";

    private readonly ILogger _logger = loggerFactory.CreateLogger<TestRunner>();

    public async Task RunTestsAsync(
        ImmutableArray<TestCase> testCases,
        BufferedProgress<RunTestsPartialResult> progress,
        VsTestConsoleWrapper vsTestConsoleWrapper,
        CancellationToken cancellationToken)
    {
        var initialProgress = new TestProgress
        {
            TotalTests = testCases.Length
        };
        progress.Report(new RunTestsPartialResult(StageName, $"{Environment.NewLine}Starting test run", initialProgress));

        var handler = new TestRunHandler(progress, initialProgress, _logger);

        // The async APIs for vs test are broken (current impl ends up just hanging), so we must use the sync API instead.
        // TODO - run settings. https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1799066/
        var runTask = Task.Run(() => vsTestConsoleWrapper.RunTests(testCases, runSettings: null, handler), cancellationToken);
        cancellationToken.Register(() => vsTestConsoleWrapper.CancelTestRun());
        await runTask;
    }

    private class TestRunHandler(BufferedProgress<RunTestsPartialResult> progress, TestProgress initialProgress, ILogger logger) : ITestRunEventsHandler
    {
        private readonly ILogger _logger = logger;
        private readonly BufferedProgress<RunTestsPartialResult> _progress = progress;
        private readonly TestProgress _initialProgress = initialProgress;

        private bool _isComplete = false;

        public void HandleLogMessage(TestMessageLevel level, string? message)
        {
            // Don't report log messages here.  The log output is dependent on the test framework being used and does not consistently
            // report the information we desire (for example the test names that passed).  Instead we report the test run information manually.
            // Any information here is also reported in the vs test console logs (written to the extension logs directory).
            return;
        }

        public void HandleRawMessage(string rawMessage)
        {
            // No need to do anything with raw messages.
            return;
        }

        public void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs, TestRunChangedEventArgs? lastChunkArgs, ICollection<AttachmentSet>? runContextAttachments, ICollection<string>? executorUris)
        {
            _isComplete = true;
            _logger.LogDebug($"Test run completed in {testRunCompleteArgs.ElapsedTimeInRunningTests}");

            // Report the last set of tests.
            var stats = CreateReport(testRunCompleteArgs.TestRunStatistics);
            var message = CreateTestCaseReportMessage(lastChunkArgs);
            var partialResult = new RunTestsPartialResult(StageName, message ?? string.Empty, stats);
            _progress.Report(partialResult);

            // Report any errors running the tests
            if (testRunCompleteArgs.Error != null)
            {
                _progress.Report(partialResult with { Message = $"Test run error: {testRunCompleteArgs.Error}" });

                return;
            }

            var state = "Passed";
            if (testRunCompleteArgs.IsCanceled)
            {
                state = "Canceled";
            }
            else if (testRunCompleteArgs.IsAborted)
            {
                state = "Aborted";
            }
            else if (stats?.TestsFailed != 0)
            {
                state = "Failed";
            }

            // Report the test summary (similar to the dotnet test output).
            message = @$"==== Summary ===={Environment.NewLine}{state}!  - Failed:    {stats?.TestsFailed}, Passed:    {stats?.TestsPassed}, Skipped:    {stats?.TestsSkipped}, Total:    {stats?.TotalTests}, Duration: {testRunCompleteArgs.ElapsedTimeInRunningTests:g}{Environment.NewLine}";

            _progress.Report(partialResult with { Message = message });
        }

        public void HandleTestRunStatsChange(TestRunChangedEventArgs? testRunChangedArgs)
        {
            Contract.ThrowIfTrue(_isComplete);
            if (testRunChangedArgs?.TestRunStatistics != null)
            {
                var stats = CreateReport(testRunChangedArgs.TestRunStatistics);
                var message = CreateTestCaseReportMessage(testRunChangedArgs);
                _progress.Report(new RunTestsPartialResult(StageName, message, stats));
            }
        }

        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            // TODO - implement debug tests.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1799066/
            throw new NotImplementedException();
        }

        private TestProgress? CreateReport(ITestRunStatistics? testRunStatistics)
        {
            if (testRunStatistics?.Stats == null)
            {
                return null;
            }

            long passed = 0, failed = 0, skipped = 0;
            foreach (var (outcome, amount) in testRunStatistics.Stats)
            {
                switch (outcome)
                {
                    case TestOutcome.Passed:
                        passed = amount;
                        break;
                    case TestOutcome.Failed:
                        failed = amount;
                        break;
                    case TestOutcome.Skipped:
                        skipped = amount;
                        break;
                    default:
                        break;
                }
            }

            var stats = _initialProgress with { TestsPassed = passed, TestsFailed = failed, TestsSkipped = skipped };
            return stats;
        }

        /// <summary>
        /// Create a nicely formatted report of the test outcome including any error message or stack trace.
        /// Ensures that we're not creating duplicate blank lines and have proper indentation.
        /// </summary>
        private static string CreateTestCaseReportMessage(TestRunChangedEventArgs? testRunChangedEventArgs)
        {
            if (testRunChangedEventArgs?.NewTestResults == null)
            {
                return string.Empty;
            }

            var results = testRunChangedEventArgs.NewTestResults.Select(result =>
            {
                var messageBuilder = new StringBuilder();
                messageBuilder.Append($"[{result.Outcome}] {result.TestCase.DisplayName}");
                if (result.ErrorMessage != null || result.ErrorStackTrace != null)
                {
                    messageBuilder.AppendLine();
                }

                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    messageBuilder.AppendLine(IndentString("Message:", 4));
                    messageBuilder.AppendLine(IndentString(result.ErrorMessage, 8));
                }

                if (!string.IsNullOrWhiteSpace(result.ErrorStackTrace))
                {
                    messageBuilder.AppendLine(value: IndentString("StackTrace:", 4));
                    messageBuilder.AppendLine(IndentString(result.ErrorStackTrace, 8));
                }

                return messageBuilder.ToString();
            });

            return string.Join(Environment.NewLine, results);

            static string IndentString(string text, int count)
            {
                return text.Replace(Environment.NewLine, $"{Environment.NewLine}        ").TrimEnd().Insert(0, new string(' ', count));
            }
        }
    }
}
