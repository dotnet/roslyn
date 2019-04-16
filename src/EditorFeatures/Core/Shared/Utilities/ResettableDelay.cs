// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal class ResettableDelay
    {
        public static readonly ResettableDelay CompletedDelay = new ResettableDelay();

        private readonly int _delayInMilliseconds;
        private readonly IExpeditableDelaySource _expeditableDelaySource;
        private readonly TaskCompletionSource<object> _taskCompletionSource;

        private int _lastSetTime;

        /// <summary>
        /// Create a ResettableDelay that will complete a task after a certain duration.  The delay
        /// can be reset at any point before it elapses in which case completion is postponed.  The
        /// delay can be reset multiple times.
        /// </summary>
        /// <param name="delayInMilliseconds">The time to delay before completing the task</param>
        /// <param name="foregroundTaskScheduler">Optional.  If used, the delay won't start until the supplied TaskScheduler schedules the delay to begin.</param>
        public ResettableDelay(int delayInMilliseconds, IExpeditableDelaySource expeditableDelaySource, TaskScheduler foregroundTaskScheduler = null)
        {
            Contract.ThrowIfFalse(delayInMilliseconds >= 50, "Perf, only use delays >= 50ms");
            _delayInMilliseconds = delayInMilliseconds;
            _expeditableDelaySource = expeditableDelaySource;

            _taskCompletionSource = new TaskCompletionSource<object>();
            Reset();

            if (foregroundTaskScheduler != null)
            {
                Task.Factory.SafeStartNew(() => StartTimerAsync(continueOnCapturedContext: true), CancellationToken.None, foregroundTaskScheduler);
            }
            else
            {
                _ = StartTimerAsync(continueOnCapturedContext: false);
            }
        }

        private ResettableDelay()
        {
            // create resettableDelay with completed state
            _delayInMilliseconds = 0;
            _taskCompletionSource = new TaskCompletionSource<object>();
            _taskCompletionSource.SetResult(null);

            Reset();
        }

        public Task Task => _taskCompletionSource.Task;

        public void Reset()
        {
            // Note: Environment.TickCount - this.lastSetTime is safe in the presence of overflow, but most
            // other operations are not.
            _lastSetTime = Environment.TickCount;
        }

        private async Task StartTimerAsync(bool continueOnCapturedContext)
        {
            do
            {
                // Keep delaying until at least delayInMilliseconds has elapsed since lastSetTime
                if (!await _expeditableDelaySource.Delay(TimeSpan.FromMilliseconds(_delayInMilliseconds), CancellationToken.None).ConfigureAwait(continueOnCapturedContext))
                {
                    // The operation is being expedited.
                    break;
                }
            }
            while (Environment.TickCount - _lastSetTime < _delayInMilliseconds);

            _taskCompletionSource.SetResult(null);
        }
    }
}
