// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

internal partial class TestRunner
{
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
            var partialResult = new RunTestsPartialResult(LanguageServerResources.Running_tests, message ?? string.Empty, stats);
            _progress.Report(partialResult);

            // Report any errors running the tests
            if (testRunCompleteArgs.Error != null)
            {
                _progress.Report(partialResult with { Message = string.Format(LanguageServerResources.Test_run_error, testRunCompleteArgs.Error) });
                return;
            }

            var state = LanguageServerResources.Passed;
            if (testRunCompleteArgs.IsCanceled)
            {
                state = LanguageServerResources.Canceled;
            }
            else if (testRunCompleteArgs.IsAborted)
            {
                state = LanguageServerResources.Aborted;
            }
            else if (stats?.TestsFailed != 0)
            {
                state = LanguageServerResources.Failed;
            }

            // Report the test summary (similar to the dotnet test output).
            message = @$"==== {LanguageServerResources.Summary} ===={Environment.NewLine}{state}  - {string.Format(LanguageServerResources.Failed_0_Passed_1_Skipped_2_Total_3_Duration_4, stats?.TestsFailed, stats?.TestsPassed, stats?.TestsSkipped, stats?.TotalTests, RunTestsHandler.GetShortTimespan(testRunCompleteArgs.ElapsedTimeInRunningTests))}{Environment.NewLine}";

            _progress.Report(partialResult with { Message = message });
        }

        public void HandleTestRunStatsChange(TestRunChangedEventArgs? testRunChangedArgs)
        {
            Contract.ThrowIfTrue(_isComplete);
            if (testRunChangedArgs?.TestRunStatistics != null)
            {
                var stats = CreateReport(testRunChangedArgs.TestRunStatistics);
                var message = CreateTestCaseReportMessage(testRunChangedArgs);
                _progress.Report(new RunTestsPartialResult(LanguageServerResources.Running_tests, message, stats));
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
                messageBuilder.AppendLine($"[{result.Outcome}] {result.TestCase.DisplayName}");

                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    messageBuilder.AppendLine(IndentString($"{LanguageServerResources.Message}:", 4));
                    messageBuilder.AppendLine(IndentString(result.ErrorMessage, 8));
                }

                if (!string.IsNullOrWhiteSpace(result.ErrorStackTrace))
                {
                    messageBuilder.AppendLine(value: IndentString($"{LanguageServerResources.Stack_Trace}:", 4));
                    messageBuilder.AppendLine(IndentString(result.ErrorStackTrace, 8));
                }

                var standardOutputMessages = GetTestMessages(result.Messages, TestResultMessage.StandardOutCategory);
                if (standardOutputMessages.Length > 0)
                {
                    messageBuilder.AppendLine(value: IndentString($"{LanguageServerResources.Standard_Output_Messages}:", 4));
                    messageBuilder.AppendLine(FormatMessages(standardOutputMessages, 8));
                }

                var standardErrorMessages = GetTestMessages(result.Messages, TestResultMessage.StandardErrorCategory);
                if (standardErrorMessages.Length > 0)
                {
                    messageBuilder.AppendLine(value: IndentString($"{LanguageServerResources.Standard_Error_Messages}:", 4));
                    messageBuilder.AppendLine(FormatMessages(standardErrorMessages, 8));
                }

                var debugTraceMessages = GetTestMessages(result.Messages, TestResultMessage.DebugTraceCategory);
                if (debugTraceMessages.Length > 0)
                {
                    messageBuilder.AppendLine(value: IndentString($"{LanguageServerResources.Debug_Trace_Messages}:", 4));
                    messageBuilder.AppendLine(FormatMessages(debugTraceMessages, 8));
                }

                var additionalInfoMessages = GetTestMessages(result.Messages, TestResultMessage.AdditionalInfoCategory);
                if (additionalInfoMessages.Length > 0)
                {
                    messageBuilder.AppendLine(value: IndentString($"{LanguageServerResources.Additional_Info_Messages}:", 4));
                    messageBuilder.AppendLine(FormatMessages(additionalInfoMessages, 8));
                }

                return messageBuilder.ToString();
            });

            return string.Join("", results);

            static string IndentString(string text, int count)
            {
                var indentation = new string(' ', count);
                return text.Replace(Environment.NewLine, $"{Environment.NewLine}{indentation}").TrimEnd().Insert(0, indentation);
            }

            static ImmutableArray<TestResultMessage> GetTestMessages(Collection<TestResultMessage> messages, string requiredCategory)
            {
                return messages.WhereAsArray(static (msg, category) => msg.Category.Equals(category, StringComparison.OrdinalIgnoreCase), requiredCategory);
            }

            static string FormatMessages(ImmutableArray<TestResultMessage> messages, int indentation)
            {
                var builder = new StringBuilder();
                foreach (var message in messages)
                {
                    if (message.Text is null)
                        continue;

                    var indentedMessage = IndentString(message.Text, indentation);
                    if (!string.IsNullOrWhiteSpace(indentedMessage))
                        builder.Append(indentedMessage);
                }

                return builder.ToString();
            }
        }
    }
}
