// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal abstract class IdleProcessor
    {
        private static readonly TimeSpan s_minimumDelay = TimeSpan.FromMilliseconds(50);

        protected readonly IAsynchronousOperationListener Listener;
        protected readonly CancellationToken CancellationToken;
        protected readonly TimeSpan BackOffTimeSpan;

        // points to processor task
        private Task? _processorTask;

        // there is one thread that writes to it and one thread reads from it
        private SharedStopwatch _timeSinceLastAccess;

        public IdleProcessor(
            IAsynchronousOperationListener listener,
            TimeSpan backOffTimeSpan,
            CancellationToken cancellationToken)
        {
            Listener = listener;
            CancellationToken = cancellationToken;

            BackOffTimeSpan = backOffTimeSpan;
            _timeSinceLastAccess = SharedStopwatch.StartNew();
        }

        protected abstract Task WaitAsync(CancellationToken cancellationToken);
        protected abstract Task ExecuteAsync();

        protected void Start()
        {
            Contract.ThrowIfFalse(_processorTask == null);
            _processorTask = Task.Factory.SafeStartNewFromAsync(ProcessAsync, CancellationToken, TaskScheduler.Default);
        }

        protected void UpdateLastAccessTime()
            => _timeSinceLastAccess = SharedStopwatch.StartNew();

        protected async Task WaitForIdleAsync(IExpeditableDelaySource expeditableDelaySource)
        {
            while (true)
            {
                if (CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var diff = _timeSinceLastAccess.Elapsed;
                if (diff >= BackOffTimeSpan)
                {
                    return;
                }

                // TODO: will safestart/unwarp capture cancellation exception?
                var timeLeft = BackOffTimeSpan - diff;
                if (!await expeditableDelaySource.Delay(TimeSpan.FromMilliseconds(Math.Max(s_minimumDelay.TotalMilliseconds, timeLeft.TotalMilliseconds)), CancellationToken).ConfigureAwait(false))
                {
                    // The delay terminated early to accommodate a blocking operation. Make sure to yield so low
                    // priority (on idle) operations get a chance to be triggered.
                    //
                    // 📝 At the time this was discovered, it was not clear exactly why the yield (previously delay)
                    // was needed in order to avoid live-lock scenarios.
                    await Task.Yield().ConfigureAwait(false);
                    return;
                }
            }
        }

        private async Task ProcessAsync()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                try
                {
                    // wait for next item available
                    await WaitAsync(CancellationToken).ConfigureAwait(false);

                    using (Listener.BeginAsyncOperation("ProcessAsync"))
                    {
                        // we have items but workspace is busy. wait for idle.
                        await WaitForIdleAsync(Listener).ConfigureAwait(false);

                        await ExecuteAsync().ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // ignore cancellation exception
                }
            }
        }

        public virtual Task AsyncProcessorTask
            => _processorTask ?? Task.CompletedTask;
    }
}
