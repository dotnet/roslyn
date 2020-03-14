// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A queue where items can be added to to be processed in batches after some delay has passed.
    /// When processing happens, all the items added since the last processing point will be passed
    /// along to be worked on.  Rounds of processing happen serially, only starting up after a
    /// previous round has completed.
    /// </summary>
    internal class AsyncBatchingWorkQueue<TItem>
    {
        /// <summary>
        /// Delay we wait after finishing the processing of one batch and starting up on then.
        /// </summary>
        private readonly TimeSpan _delay;

        /// <summary>
        /// Callback to actually perform the processing of the next batch of work.
        /// </summary>
        private readonly Func<ImmutableArray<TItem>, CancellationToken, Task> _processBatchAsync;
        private readonly CancellationToken _cancellationToken;

        #region protected by lock

        /// <summary>
        /// Lock we will use to ensure the remainder of these fields can be accessed in a threadsafe
        /// manner.  When work is added we'll place the data into <see cref="_nextBatch"/>.
        /// We'll then kick of a task to process this in the future if we don't already have an
        /// existing task in flight for that.
        /// </summary>
        private readonly object _gate = new object();

        /// <summary>
        /// Data added that we want to process in our next update task.
        /// </summary>
        private readonly List<TItem> _nextBatch = new List<TItem>();

        /// <summary>
        /// Task kicked off to do the next batch of processing of <see cref="_nextBatch"/>. These
        /// tasks form a chain so that the next task only processes when the previous one completes.
        /// </summary>
        private Task _updateTask = Task.CompletedTask;

        /// <summary>
        /// Whether or not there is an existing task in flight that will process the current batch
        /// of <see cref="_nextBatch"/>.  If there is an existing in flight task, we don't need to
        /// kick off a new one if we receive more work before it runs.
        /// </summary>
        private bool _taskInFlight = false;

        #endregion

        public AsyncBatchingWorkQueue(
            TimeSpan delay,
            Func<ImmutableArray<TItem>, CancellationToken, Task> processBatchAsync,
            CancellationToken cancellationToken)
        {
            _delay = delay;
            _processBatchAsync = processBatchAsync;
            _cancellationToken = cancellationToken;
        }

        public void AddWork(TItem item)
        {
            using var _ = ArrayBuilder<TItem>.GetInstance(out var items);
            items.Add(item);

            AddWork(items);
        }

        public void AddWork(IEnumerable<TItem> items)
        {
            // Don't do any more work if we've been asked to shutdown.
            if (_cancellationToken.IsCancellationRequested)
                return;

            lock (_gate)
            {
                // add our work to the set we'll process in the next batch.
                _nextBatch.AddRange(items);

                if (!_taskInFlight)
                {
                    // No in-flight task.  Kick one off to process these messages a second from now.
                    // We always attach the task to the previous one so that notifications to the ui
                    // follow the same order as the notification the OOP server sent to us.
                    _updateTask = _updateTask.ContinueWithAfterDelayFromAsync(
                        _ => ProcessNextBatchAsync(_cancellationToken),
                        _cancellationToken,
                        (int)_delay.TotalMilliseconds,
                        TaskContinuationOptions.RunContinuationsAsynchronously,
                        TaskScheduler.Default);
                    _taskInFlight = true;
                }
            }
        }

        private Task ProcessNextBatchAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _processBatchAsync(GetNextBatchAndResetQueue(), _cancellationToken);
        }

        private ImmutableArray<TItem> GetNextBatchAndResetQueue()
        {
            lock (_gate)
            {
                var result = ArrayBuilder<TItem>.GetInstance();
                result.AddRange(_nextBatch);

                // mark there being no existing update task so that the next OOP notification will
                // kick one off.
                _nextBatch.Clear();
                _taskInFlight = false;

                return result.ToImmutableAndFree();
            }
        }
    }
}
