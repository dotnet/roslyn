// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

/// <summary>
/// A class similar to a <see cref="AsyncBatchingWorkQueue{TItem}"/> but with a few key differences:
/// 
/// <list type="bullet">
/// <item>Each item has a priority, higher priorities (defined as a larger number away from zero) are processed first.</item>
/// <item>Each item has a single priority and only appears in the queue once; unlike <see cref="AsyncBatchingWorkQueue{TItem}"/> where deduplication is optional.</item>
/// <item>When a batch is started, it is given an enumerator of the items in the batch, rather than a list. This allows items to be reprioritized or added while a batch is underway.</item>
/// <item>We don't support cancelling currently queued work, since the users of this don't have a need. If that becomes a need, that can be added.</item>
/// </list>
/// 
/// The assumption here is this type is helpful for situations when batches are long-running, so being able to reprioritize items or "sneak things in" to a batch already running
/// is more helpful.
/// </summary>
internal sealed class AsyncPriorityWorkQueue<TItem> : IDisposable where TItem : notnull
{
    private readonly Func<Enumerator, CancellationToken, ValueTask> _processBatchAsync;
    private readonly IAsynchronousOperationListener _asyncListener;

    /// <summary>
    /// Delay we wait after finishing the processing of one batch and starting up another.
    /// </summary>
    private readonly TimeSpan _delay;

    /// <summary>
    /// The gate for all mutable fields of this type that are listed below this.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// Cancellation token controlling the entire queue.  Once this is triggered, we don't want to do any more work
    /// at all. This is cancelled by a call to <see cref="Dispose()"/>; the IsCancellationRequested flag of this token
    /// can be used as the "is disposed" flag for this object.
    /// </summary>
    private readonly CancellationTokenSource _entireQueueCancellationTokenSource;

    /// <summary>
    /// The items that have been added to the queue by priority; the array index is the priority and the end of the array is the highest priority.
    /// Unfortunately .NET doesn't have a PriorityQueue type that allows priorities to be changed, so this is the simplest implementation.
    /// The assumption is the number of priorities is fixed -- if that is not the case then a different implementation is needed here.
    /// </summary>
    private readonly HashSet<TItem>[] _itemsByPriority;

    /// <summary>
    /// Task kicked off to do the next batch of processing of <see cref="_itemsByPriority"/>. These
    /// tasks form a chain so that the next task only processes when the previous one completes.
    /// </summary>
    private Task _updateTask = Task.CompletedTask;

    /// <summary>
    /// Whether or not there is an existing task in flight that will process the current batch
    /// of items in <see cref="_itemsByPriority"/> If there is an existing in flight task, we don't need to
    /// kick off a new one if we receive more work before it runs.
    /// </summary>
    private bool _taskInFlight = false;

    public AsyncPriorityWorkQueue(
        int maximumPriority,
        TimeSpan delay,
        Func<Enumerator, CancellationToken, ValueTask> processBatchAsync,
        IEqualityComparer<TItem> equalityComparer,
        IAsynchronousOperationListener asyncListener)
    {
        if (maximumPriority < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumPriority), maximumPriority, "Maximum priority must be non-negative.");

        _itemsByPriority = new HashSet<TItem>[maximumPriority + 1];
        _delay = delay;
        _processBatchAsync = processBatchAsync;
        _asyncListener = asyncListener;

        for (var i = 0; i < _itemsByPriority.Length; i++)
            _itemsByPriority[i] = new HashSet<TItem>(equalityComparer);

        _entireQueueCancellationTokenSource = new CancellationTokenSource();
    }

    public void AddWork(TItem item, int priority)
    {
        if (priority < 0 || priority >= _itemsByPriority.Length)
            throw new ArgumentOutOfRangeException(paramName: nameof(priority), message: $"Priority must be between 0 and {_itemsByPriority.Length - 1}");

        lock (_gate)
        {
            if (_entireQueueCancellationTokenSource.IsCancellationRequested)
                return;

            // If it's already at a higher priority, then nothing further to do
            for (var priorityToCheck = _itemsByPriority.Length - 1; priorityToCheck > priority; priorityToCheck--)
            {
                if (_itemsByPriority[priorityToCheck].Contains(item))
                    return;
            }

            if (!_itemsByPriority[priority].Add(item))
            {
                // Item was already that priority, so return and nothing new must happen
                return;
            }

            // We've definitely added it; if it's at any lower priorities, then remove it
            for (var priorityToCheck = priority - 1; priorityToCheck >= 0; priorityToCheck--)
                _itemsByPriority[priorityToCheck].Remove(item);

            // If we don't already have an active task, start one
            if (!_taskInFlight)
                StartWork();
        }
    }

    private void StartWork()
    {
        lock (_gate)
        {
            // Kick one off to process the items
            // We always attach the task to the previous one so that batches are processed
            // in order.
            _updateTask = ContinueAfterDelayAsync(_updateTask);
            _taskInFlight = true;

            async Task ContinueAfterDelayAsync(Task lastTask)
            {
                using var _ = _asyncListener.BeginAsyncOperation(nameof(AddWork));

                // Await the previous item in the task chain in a non-throwing fashion.  Regardless of whether that last
                // task completed successfully or not, we want to move onto the next batch.
                await lastTask.NoThrowAwaitableInternal(captureContext: false);

                // If we were asked to shutdown, immediately transition to the canceled state without doing any more work.
                if (_entireQueueCancellationTokenSource.IsCancellationRequested)
                    return;

                // Ensure that we always yield the current thread this is necessary for correctness as we are called
                // inside a lock that _taskInFlight to true.  We must ensure that the work to process the next batch
                // must be on another thread that runs afterwards, can only grab the thread once we release it and will
                // then reset that bool back to false
                await Task.Yield().ConfigureAwait(false);
                await _asyncListener.Delay(_delay, _entireQueueCancellationTokenSource.Token).NoThrowAwaitableInternal(false);

                // If we were asked to shutdown, immediately transition to the canceled state without doing any more work.
                if (_entireQueueCancellationTokenSource.IsCancellationRequested)
                    return;

                await ProcessNextBatchAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessNextBatchAsync()
    {
        Enumerator enumerator;

        lock (_gate)
        {
            // If we don't have any items left, then the work was cancelled and we can immediately be done.
            if (_itemsByPriority.All(static s => s.Count == 0))
            {
                _taskInFlight = false;
                return;
            }

            enumerator = new Enumerator(this);
        }

        try
        {
            await _processBatchAsync(enumerator, _entireQueueCancellationTokenSource.Token).ConfigureAwait(false);
        }
        finally
        {
            // We have completed that batch. When enumeration was stopped, _taskInFlight would have been set to false, and a later AddWork would
            // have set it to true again. But, it's also possible the enumeration was stopped prematurely; in that case we still have work queued up, and
            // we should start that again.
            if (!enumerator.EnumeratorStopped)
            {
                Contract.ThrowIfFalse(_taskInFlight);
                StartWork();
            }
        }
    }

    private bool TryGetNextItem([NotNullWhen(true)] out TItem? item)
    {
        lock (_gate)
        {
            for (var priorityToCheck = _itemsByPriority.Length - 1; priorityToCheck >= 0; priorityToCheck--)
            {
                var items = _itemsByPriority[priorityToCheck];
                if (items.Count > 0)
                {
                    item = items.First();
                    items.Remove(item);
                    return true;
                }
            }

            // We have no items left; we'll end the enumeration here; if a new item is added, we need to start a task again
            _taskInFlight = false;
            item = default;
            return false;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            // If we've previously disposed, we don't need to do anything further
            if (_entireQueueCancellationTokenSource.IsCancellationRequested)
                return;

            // Cancel all work in the queue
            for (var i = 0; i < _itemsByPriority.Length; i++)
                _itemsByPriority[i].Clear();

            _entireQueueCancellationTokenSource.Cancel();
        }
    }

    /// <summary>
    /// An enumerator that will return the items of the queue in priority order. This doesn't implement IEnumerable so the interface can be used from
    /// multiple threads.
    /// </summary>
    public sealed class Enumerator(AsyncPriorityWorkQueue<TItem> queue)
    {
        private readonly object _gate = new object();

        public bool EnumeratorStopped { get; private set; } = false;

        public bool TryGetNextItem([NotNullWhen(true)] out TItem? item)
        {
            lock (_gate)
            {
                // Once we have stopped the enumeration this batch is done, even if more get added later.
                if (EnumeratorStopped)
                {
                    item = default;
                    return false;
                }

                if (queue.TryGetNextItem(out item))
                {
                    return true;
                }
                else
                {
                    EnumeratorStopped = true;
                    return false;
                }
            }
        }
    }
}
