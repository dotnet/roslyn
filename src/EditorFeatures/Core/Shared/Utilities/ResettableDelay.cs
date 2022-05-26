// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal class ResettableDelay
    {
        public static readonly ResettableDelay CompletedDelay = new();

        private readonly int _delayInMilliseconds;
        private readonly TaskCompletionSource<object?> _taskCompletionSource = new();

        private int _lastSetTime;

        /// <summary>
        /// Create a ResettableDelay that will complete a task after a certain duration.  The delay
        /// can be reset at any point before it elapses in which case completion is postponed.  The
        /// delay can be reset multiple times.
        /// </summary>
        /// <param name="delayInMilliseconds">The time to delay before completing the task</param>
        public ResettableDelay(int delayInMilliseconds, IExpeditableDelaySource expeditableDelaySource, CancellationToken cancellationToken = default)
        {
            Contract.ThrowIfFalse(delayInMilliseconds >= 50, "Perf, only use delays >= 50ms");
            _delayInMilliseconds = delayInMilliseconds;

            Reset();

            _ = StartTimerAsync(expeditableDelaySource, cancellationToken);
        }

        private ResettableDelay()
        {
            // create resettableDelay with completed state
            _delayInMilliseconds = 0;
            _taskCompletionSource = new TaskCompletionSource<object?>();
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

        private async Task StartTimerAsync(IExpeditableDelaySource expeditableDelaySource, CancellationToken cancellationToken)
        {
            try
            {
                do
                {
                    // Keep delaying until at least delayInMilliseconds has elapsed since lastSetTime
                    if (!await expeditableDelaySource.Delay(TimeSpan.FromMilliseconds(_delayInMilliseconds), cancellationToken).ConfigureAwait(false))
                    {
                        // The operation is being expedited.
                        break;
                    }
                }
                while (Environment.TickCount - _lastSetTime < _delayInMilliseconds);

                _taskCompletionSource.SetResult(null);
            }
            catch (OperationCanceledException)
            {
                // Calling the "Try" variant because that's the only one that accepts the token to associate with the task
                _taskCompletionSource.TrySetCanceled(cancellationToken);
            }
        }
    }
}
