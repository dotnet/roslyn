// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal abstract class IdleProcessor
    {
        private static readonly TimeSpan s_minimumDelay = TimeSpan.FromMilliseconds(50);

        private readonly object _gate = new();

        protected readonly IAsynchronousOperationListener Listener;
        protected readonly CancellationToken CancellationToken;
        protected readonly TimeSpan BackOffTimeSpan;

        // points to processor task
        private Task? _processorTask;

        // there is one thread that writes to it and one thread reads from it
        private SharedStopwatch _timeSinceLastAccess;

        /// <summary>
        /// Whether or not this processor is paused.  As long as it is paused, it will not start executing new work,
        /// even if <see cref="BackOffTimeSpan"/> has been met.
        /// </summary>
        private bool _paused_doNotAccessDirectly;

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

        protected bool Paused
        {
            get
            {
                lock (_gate)
                    return _paused_doNotAccessDirectly;
            }

            set
            {
                lock (_gate)
                {
                    // We should never try to transition from paused state to paused state.  That would indicate we
                    // missed some resume call, or that the pause-notification are not serialized.  Note: we cannot make
                    // the opposite assertion.  We start in the resumed state, and we might then get a call to resume if
                    // we were started while in the *middle* of some global operation.
                    if (value)
                        Contract.ThrowIfTrue(_paused_doNotAccessDirectly);

                    _paused_doNotAccessDirectly = value;
                }

                // Let subclasses know we're paused so they can change what they're doing accordingly.
                if (value)
                    OnPaused();
            }
        }

        protected async Task WaitForIdleAsync(IExpeditableDelaySource expeditableDelaySource)
        {
            while (!CancellationToken.IsCancellationRequested)
            {

                // If we're not paused, and enough time has elapsed, then we're done.  Otherwise, ensure we wait at
                // least s_minimumDelay and check again in the future.
                var diff = _timeSinceLastAccess.Elapsed;
                if (!Paused && diff >= BackOffTimeSpan)
                    return;

                var timeLeft = BackOffTimeSpan - diff;
                var delayTimeSpan = TimeSpan.FromMilliseconds(Math.Max(s_minimumDelay.TotalMilliseconds, timeLeft.TotalMilliseconds));
                if (!await expeditableDelaySource.Delay(delayTimeSpan, CancellationToken).ConfigureAwait(false))
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

                        if (!CancellationToken.IsCancellationRequested)
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
