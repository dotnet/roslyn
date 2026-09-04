// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal abstract partial class LanguageServerProjectLoader
{
    /// <summary>
    /// Reports percentage progress via <see cref="LSP.WorkDoneProgress"/> as items are processed,
    /// coalescing updates from parallel callers. Disposing sends the final 100% notification.
    /// </summary>
    internal sealed class WorkDoneProgressTracker : IAsyncDisposable
    {
        private readonly IProgress<LSP.WorkDoneProgress> _reporter;
        private readonly int _totalItems;
        private readonly AsyncBatchingWorkQueue _progressQueue;
        private int _itemsProcessed;
        private int _lastReportedPercentage = -1;

        public WorkDoneProgressTracker(IProgress<LSP.WorkDoneProgress> reporter, int totalItems, IAsynchronousOperationListener? listener = null)
        {
            _reporter = reporter;
            _totalItems = totalItems;
            _progressQueue = new AsyncBatchingWorkQueue(
                TimeSpan.Zero,
                ReportProgressAsync,
                listener ?? AsynchronousOperationListenerProvider.NullListener);

            reporter.Report(new LSP.WorkDoneProgressReport
            {
                Message = string.Format(LanguageServerResources.Loading_0_projects, totalItems),
                Percentage = 0,
            });
        }

        public void OnItemProcessed()
        {
            Interlocked.Increment(ref _itemsProcessed);
            _progressQueue.AddWork();
        }

        private ValueTask ReportProgressAsync(CancellationToken cancellationToken)
        {
            var processed = Volatile.Read(ref _itemsProcessed);
            var percentage = processed * 100 / _totalItems;
            percentage = Math.Min(percentage, 99);

            if (percentage > _lastReportedPercentage)
            {
                _lastReportedPercentage = percentage;
                _reporter.Report(new LSP.WorkDoneProgressReport
                {
                    Percentage = percentage,
                });
            }

            return ValueTask.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _progressQueue.WaitUntilCurrentBatchCompletesAsync();
                _reporter.Report(new LSP.WorkDoneProgressReport { Percentage = 100 });
            }
            finally
            {
                _progressQueue.Dispose();
            }
        }
    }
}
