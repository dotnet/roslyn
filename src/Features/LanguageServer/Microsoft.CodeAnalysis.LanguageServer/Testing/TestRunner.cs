// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Extensions.Logging;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

[Export, Shared]
internal class TestRunner
{
    /// <summary>
    /// TODO - localize messages. https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1799066/
    /// </summary>
    private const string StageName = "Running tests...";

    private readonly IAsynchronousOperationListener _listener;
    private readonly ILogger _logger;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TestRunner(IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider, ILoggerFactory loggerFactory)
    {
        _listener = asynchronousOperationListenerProvider.GetListener(nameof(TestRunner));
        _logger = loggerFactory.CreateLogger<TestRunner>();
    }

    public async Task RunTestsAsync(
        ImmutableArray<TestCase> testCases,
        BufferedProgress<RunTestsPartialResult> progress,
        VsTestConsoleWrapper vsTestConsoleWrapper,
        CancellationToken cancellationToken)
    {
        var initialProgres = new TestProgress
        {
            TotalTests = testCases.Length
        };
        progress.Report(new RunTestsPartialResult
        {
            Stage = StageName,
            Message = $"{Environment.NewLine}Starting test run",
            Progress = initialProgres
        });

        var handler = new TestRunHandler(progress, initialProgres, _logger, _listener, cancellationToken);

        // The async APIs for vs test are broken (current impl ends up just hanging), so we must use the sync API instead.
        // TODO - run settings. https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1799066/
        var runTask = Task.Run(() => vsTestConsoleWrapper.RunTests(testCases, runSettings: null, handler), cancellationToken);
        cancellationToken.Register(() => vsTestConsoleWrapper.CancelTestRun());
        await runTask;
    }

    private class TestRunHandler : ITestRunEventsHandler
    {
        private readonly ILogger _logger;
        private readonly BufferedProgress<RunTestsPartialResult> _progress;
        private readonly AsyncBatchingWorkQueue<ITestRunStatistics?> _batchingWorkQueue;

        /// <summary>
        /// Serial access is gauranteed by the <see cref="_batchingWorkQueue"/> 
        /// </summary>
        private TestProgress _lastReport;

        private bool _isComplete = false;

        public TestRunHandler(BufferedProgress<RunTestsPartialResult> progress, TestProgress initialProgress, ILogger logger, IAsynchronousOperationListener listener, CancellationToken cancellationToken)
        {
            _progress = progress;
            _lastReport = initialProgress;
            _logger = logger;

            _batchingWorkQueue = new(
                TimeSpan.FromMicroseconds(100),
                OnBatchReportAsync,
                listener,
                cancellationToken);
        }

        public void HandleLogMessage(TestMessageLevel level, string? message)
        {
            if (message != null)
            {
                _progress.Report(new RunTestsPartialResult
                {
                    Stage = StageName,
                    Message = message,
                });
            }
        }

        public void HandleRawMessage(string rawMessage)
        {
            // No need to do anything with raw messages.
            return;
        }

        public void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs, TestRunChangedEventArgs? lastChunkArgs, ICollection<AttachmentSet>? runContextAttachments, ICollection<string>? executorUris)
        {
            _batchingWorkQueue.AddWork(testRunCompleteArgs.TestRunStatistics);
            _isComplete = true;
            _logger.LogDebug($"Test run completed in {testRunCompleteArgs.ElapsedTimeInRunningTests}");

            // Block until the last report goes through.
            _batchingWorkQueue.WaitUntilCurrentBatchCompletesAsync().Wait();

            if (testRunCompleteArgs.Error != null)
            {
                _progress.Report(new RunTestsPartialResult
                {
                    Stage = StageName,
                    Message = $"Test run error: {testRunCompleteArgs.Error}",
                    Progress = _lastReport,
                });

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
            else if (_lastReport.TestsFailed != 0)
            {
                state = "Failed";
            }

            var message = $"{state}!    - Failed:    {_lastReport.TestsFailed}, Passed:    {_lastReport.TestsPassed}, Skipped:    {_lastReport.TestsSkipped}, Total:    {_lastReport.TotalTests}, Duration: {testRunCompleteArgs.ElapsedTimeInRunningTests:g}";

            _progress.Report(new RunTestsPartialResult
            {
                Stage = StageName,
                Message = message,
                Progress = _lastReport
            });
        }

        public void HandleTestRunStatsChange(TestRunChangedEventArgs? testRunChangedArgs)
        {
            Contract.ThrowIfTrue(_isComplete);
            _batchingWorkQueue.AddWork(testRunChangedArgs?.TestRunStatistics);
        }

        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            // TODO - implement debug tests.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1799066/
            throw new NotImplementedException();
        }

        private ValueTask OnBatchReportAsync(ImmutableSegmentedList<ITestRunStatistics?> testRunStatistics, CancellationToken cancellationToken)
        {
            // We only care about the latest report in the batch since it contains
            // the aggregated statistics from all previous reports.
            var latestReport = testRunStatistics.Last(r => r?.Stats != null);
            if (latestReport == null)
            {
                return ValueTask.CompletedTask;
            }

            long passed = 0, failed = 0, skipped = 0;
            // Verified stats is not null above.
            foreach (var (outcome, amount) in latestReport.Stats!)
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

            var stats = _lastReport with { TestsPassed = passed, TestsFailed = failed, TestsSkipped = skipped };
            _lastReport = stats;

            _progress.Report(new RunTestsPartialResult
            {
                Stage = StageName,
                Message = string.Empty,
                Progress = _lastReport
            });

            return ValueTask.CompletedTask;
        }
    }
}
