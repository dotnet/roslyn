// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A queue where items can be added to to be processed in batches after some delay has passed.
    /// When processing happens, all the items added since the last processing point will be passed
    /// along to be worked on.  Rounds of processing happen serially, only starting up after a
    /// previous round has completed.
    /// </summary>
    internal class AsyncBatchingWorkQueue<TItem, TResult>
    {
        /// <summary>
        /// Delay we wait after finishing the processing of one batch and starting up on then.
        /// </summary>
        private readonly TimeSpan _delay;

        /// <summary>
        /// Equality comparer uses to dedupe items if present.
        /// </summary>
        private readonly IEqualityComparer<TItem>? _equalityComparer;

        /// <summary>
        /// Callback to actually perform the processing of the next batch of work.
        /// </summary>
        private readonly Func<ImmutableSegmentedList<TItem>, CancellationToken, ValueTask<TResult>> _processBatchAsync;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly CancellationToken _cancellationToken;

        #region protected by lock

        /// <summary>
        /// Lock we will use to ensure the remainder of these fields can be accessed in a threadsafe
        /// manner.  When work is added we'll place the data into <see cref="_nextBatch"/>.
        /// We'll then kick of a task to process this in the future if we don't already have an
        /// existing task in flight for that.
        /// </summary>
        private readonly object _gate = new();

        /// <summary>
        /// Data added that we want to process in our next update task.
        /// </summary>
        private readonly ImmutableSegmentedList<TItem>.Builder _nextBatch = ImmutableSegmentedList.CreateBuilder<TItem>();

        /// <summary>
        /// Used if <see cref="_equalityComparer"/> is present to ensure only unique items are added to <see
        /// cref="_nextBatch"/>.
        /// </summary>
        private readonly HashSet<TItem> _uniqueItems;

        /// <summary>
        /// Task kicked off to do the next batch of processing of <see cref="_nextBatch"/>. These
        /// tasks form a chain so that the next task only processes when the previous one completes.
        /// </summary>
        private Task<TResult?> _updateTask = Task.FromResult(default(TResult));

        /// <summary>
        /// Whether or not there is an existing task in flight that will process the current batch
        /// of <see cref="_nextBatch"/>.  If there is an existing in flight task, we don't need to
        /// kick off a new one if we receive more work before it runs.
        /// </summary>
        private bool _taskInFlight = false;

        #endregion

        public AsyncBatchingWorkQueue(
            TimeSpan delay,
            Func<ImmutableSegmentedList<TItem>, CancellationToken, ValueTask<TResult>> processBatchAsync,
            IEqualityComparer<TItem>? equalityComparer,
            IAsynchronousOperationListener asyncListener,
            CancellationToken cancellationToken)
        {
            _delay = delay;
            _processBatchAsync = processBatchAsync;
            _equalityComparer = equalityComparer;
            _asyncListener = asyncListener;
            _cancellationToken = cancellationToken;

            _uniqueItems = new HashSet<TItem>(equalityComparer);
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
                AddItemsToBatch(items);

                if (!_taskInFlight)
                {
                    // No in-flight task.  Kick one off to process these messages a second from now.
                    // We always attach the task to the previous one so that notifications to the ui
                    // follow the same order as the notification the OOP server sent to us.
                    _updateTask = ContinueAfterDelay(_updateTask);
                    _taskInFlight = true;
                }
            }

            return;

            void AddItemsToBatch(IEnumerable<TItem> items)
            {
                // no equality comparer.  We want to process all items.
                if (_equalityComparer == null)
                {
                    _nextBatch.AddRange(items);
                    return;
                }

                // We're deduping items.  Only add the item if it's the first time we've seen it.
                foreach (var item in items)
                {
                    if (_uniqueItems.Add(item))
                        _nextBatch.Add(item);
                }
            }

            async Task<TResult?> ContinueAfterDelay(Task lastTask)
            {
                using var _ = _asyncListener.BeginAsyncOperation(nameof(AddWork));
                await lastTask.ConfigureAwait(false);

                // Ensure that we always yield the current thread this is necessary for correctness as we are called
                // inside a lock that _taskInFlight to true.  We must ensure that the work to process the next batch
                // must be on another thread that runs afterwards, can only grab the thread once we release it and will
                // then reset that bool back to false
                await Task.Yield().ConfigureAwait(false);
                await _asyncListener.Delay(_delay, _cancellationToken).ConfigureAwait(false);
                return await ProcessNextBatchAsync(_cancellationToken).ConfigureAwait(false);
            }
        }

        public Task<TResult?> WaitUntilCurrentBatchCompletesAsync()
        {
            lock (_gate)
                return _updateTask;
        }

        private ValueTask<TResult> ProcessNextBatchAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _processBatchAsync(GetNextBatchAndResetQueue(), _cancellationToken);
        }

        private ImmutableSegmentedList<TItem> GetNextBatchAndResetQueue()
        {
            lock (_gate)
            {
                var result = _nextBatch.ToImmutable();

                // mark there being no existing update task so that the next OOP notification will
                // kick one off.
                _nextBatch.Clear();
                _uniqueItems.Clear();
                _taskInFlight = false;

                return result;
            }
        }
    }
}
