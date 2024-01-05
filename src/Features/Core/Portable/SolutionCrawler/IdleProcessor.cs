// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal abstract class IdleProcessor(
        IAsynchronousOperationListener listener,
        TimeSpan backOffTimeSpan,
        CancellationToken cancellationToken)
    {
        private static readonly TimeSpan s_minimumDelay = TimeSpan.FromMilliseconds(50);

        private readonly object _gate = new();

        protected readonly IAsynchronousOperationListener Listener = listener;
        protected readonly CancellationToken CancellationToken = cancellationToken;
        protected readonly TimeSpan BackOffTimeSpan = backOffTimeSpan;

        // points to processor task
        private Task? _processorTask;

        // there is one thread that writes to it and one thread reads from it
        private SharedStopwatch _timeSinceLastAccess = SharedStopwatch.StartNew();

        /// <summary>
        /// Whether or not this processor is paused.  As long as it is paused, it will not start executing new work,
        /// even if <see cref="BackOffTimeSpan"/> has been met.
        /// </summary>
        private bool _isPaused_doNotAccessDirectly;

        protected abstract Task WaitAsync(CancellationToken cancellationToken);
        protected abstract Task ExecuteAsync();

        /// <summary>
        /// Will be called in a serialized fashion (i.e. never concurrently).
        /// </summary>
        protected abstract void OnPaused();

        protected void Start()
        {
            Contract.ThrowIfFalse(_processorTask == null);
            _processorTask = Task.Factory.SafeStartNewFromAsync(ProcessAsync, CancellationToken, TaskScheduler.Default);
        }

        protected void UpdateLastAccessTime()
            => _timeSinceLastAccess = SharedStopwatch.StartNew();

        /// <summary>
        /// Whether or not we are paused due to a global operation being in effect.
        /// </summary>
        protected bool GetIsPaused()
        {
            lock (_gate)
                return _isPaused_doNotAccessDirectly;
        }

        /// <summary>
        /// Whether or not enough time has passed since the last time we were asked to back off.
        /// </summary>
        protected bool ShouldContinueToBackOff()
            => _timeSinceLastAccess.Elapsed < BackOffTimeSpan;

        protected void SetIsPaused(bool isPaused)
        {
            lock (_gate)
            {
                // We should never try to transition from paused state to paused state.  That would indicate we
                // missed some resume call, or that the pause-notification are not serialized.  Note: we cannot make
                // the opposite assertion.  We start in the resumed state, and we might then get a call to resume if
                // we were started while in the *middle* of some global operation.
                if (isPaused)
                    Contract.ThrowIfTrue(_isPaused_doNotAccessDirectly);

                _isPaused_doNotAccessDirectly = isPaused;
            }

            // Let subclasses know we're paused so they can change what they're doing accordingly.
            if (isPaused)
                OnPaused();
        }

        /// <returns><see langword="true"/> if the delay compeleted normally; otherwise, <see langword="false"/> if the
        /// delay completed due to a request to expedite the delay.</returns>
        protected async Task<bool> WaitForIdleAsync(IExpeditableDelaySource expeditableDelaySource)
        {
            while (true)
            {
                this.CancellationToken.ThrowIfCancellationRequested();

                // If we're not paused, and enough time has elapsed, then we're done.  Otherwise, ensure we wait at
                // least s_minimumDelay and check again in the future.
                if (!GetIsPaused() && !ShouldContinueToBackOff())
                    return true;

                var timeLeft = BackOffTimeSpan - _timeSinceLastAccess.Elapsed;
                var delayTimeSpan = TimeSpan.FromMilliseconds(Math.Max(s_minimumDelay.TotalMilliseconds, timeLeft.TotalMilliseconds));
                if (!await expeditableDelaySource.Delay(delayTimeSpan, CancellationToken).ConfigureAwait(false))
                {
                    // The delay terminated early to accommodate a blocking operation. Make sure to yield so low
                    // priority (on idle) operations get a chance to be triggered.
                    //
                    // 📝 At the time this was discovered, it was not clear exactly why the yield (previously delay)
                    // was needed in order to avoid live-lock scenarios.
                    await Task.Yield().ConfigureAwait(false);
                    return false;
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
                    CancellationToken.ThrowIfCancellationRequested();

                    using (Listener.BeginAsyncOperation("ProcessAsync"))
                    {
                        // we have items but workspace is busy. wait for idle.
                        await WaitForIdleAsync(Listener).ConfigureAwait(false);
                        CancellationToken.ThrowIfCancellationRequested();

                        await ExecuteAsync().ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // ignore cancellation exception
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
                {
                    // In case any error happen during the execution, don't exit the loop and continue to work on the next item.
                }
            }
        }

        public virtual Task AsyncProcessorTask
            => _processorTask ?? Task.CompletedTask;
    }
}
