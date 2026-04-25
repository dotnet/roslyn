// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.Threading;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Razor.Utilities;

// NOTE: This code is derived from dotnet/roslyn:
// https://github.com/dotnet/roslyn/blob/98cd097bf122677378692ebe952b71ab6e5bb013/src/Workspaces/Core/Portable/Shared/Utilities/AsyncBatchingWorkQueue%602.cs
//
// The key change to the Roslyn implementation is the addition of 'preferMostRecentItems' which controls which
// version of an item is preferred when deduping is employed.

/// <summary>
/// A queue where items can be added to to be processed in batches after some delay has passed. When processing
/// happens, all the items added since the last processing point will be passed along to be worked on.  Rounds of
/// processing happen serially, only starting up after a previous round has completed.
/// <para>
/// Failure to complete a particular batch (either due to cancellation or some faulting error) will not prevent
/// further batches from executing. The only thing that will permanently stop this queue from processing items is if
/// the <see cref="CancellationToken"/> passed to the constructor switches to <see
/// cref="CancellationToken.IsCancellationRequested"/>.
/// </para>
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
    /// Fired when all batches have finished being processed, and the queue is waiting for an AddWork call.
    /// </summary>
    /// <remarks>
    /// This is a best-effort signal with no guarantee that more work won't be queued, and hence the queue
    /// going non-idle, immediately after (or during!) the event firing.
    /// </remarks>
    private readonly Action? _idleAction;

    /// <summary>
    /// Callback to actually perform the processing of the next batch of work.
    /// </summary>
    private readonly Func<ImmutableArray<TItem>, CancellationToken, ValueTask<TResult>> _processBatchAsync;

    /// <summary>
    /// Cancellation token controlling the entire queue.  Once this is triggered, we don't want to do any more work
    /// at all.
    /// </summary>
    private readonly CancellationToken _entireQueueCancellationToken;

    /// <summary>
    /// Cancellation series we use so we can cancel individual batches of work if requested.  The client of the
    /// queue can cancel existing work by either calling <see cref="CancelExistingWork"/> directly, or passing <see
    /// langword="true"/> to <see cref="AddWork(TItem, bool)"/>.  Work in the queue that has not started will be
    /// immediately discarded. The cancellation token passed to <see cref="_processBatchAsync"/> will be triggered
    /// allowing the client callback to cooperatively cancel the current batch of work it is performing.
    /// </summary>
    private readonly CancellationSeries _cancellationSeries;

    #region protected by lock

    /// <summary>
    /// Lock we will use to ensure the remainder of these fields can be accessed in a thread-safe
    /// manner.  When work is added we'll place the data into <see cref="_nextBatch"/>.
    /// We'll then kick of a task to process this in the future if we don't already have an
    /// existing task in flight for that.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// Data added that we want to process in our next update task.
    /// </summary>
    private readonly ImmutableArray<TItem>.Builder _nextBatch = ImmutableArray.CreateBuilder<TItem>();

    /// <summary>
    /// CancellationToken controlling the next batch of items to execute.
    /// </summary>
    private CancellationToken _nextBatchCancellationToken;

    /// <summary>
    /// Used if <see cref="_equalityComparer"/> is present to ensure only unique items are added to <see
    /// cref="_nextBatch"/>.
    /// </summary>
    private readonly HashSet<TItem> _uniqueItems;

    /// <summary>
    /// Task kicked off to do the next batch of processing of <see cref="_nextBatch"/>. These
    /// tasks form a chain so that the next task only processes when the previous one completes.
    /// </summary>
    private Task<TResult?> _updateTask = SpecializedTasks.Default<TResult>();

    /// <summary>
    /// Whether or not there is an existing task in flight that will process the current batch
    /// of <see cref="_nextBatch"/>.  If there is an existing in flight task, we don't need to
    /// kick off a new one if we receive more work before it runs.
    /// </summary>
    private bool _taskInFlight = false;

    #endregion

    public AsyncBatchingWorkQueue(
        TimeSpan delay,
        Func<ImmutableArray<TItem>, CancellationToken, ValueTask<TResult>> processBatchAsync,
        IEqualityComparer<TItem>? equalityComparer,
        Action? idleAction,
        CancellationToken cancellationToken)
    {
        _delay = delay;
        _processBatchAsync = processBatchAsync;
        _equalityComparer = equalityComparer;
        _idleAction = idleAction;
        _entireQueueCancellationToken = cancellationToken;

        _uniqueItems = new HashSet<TItem>(equalityComparer);

        // Combine with the queue cancellation token so that any batch is controlled by that token as well.
        _cancellationSeries = new CancellationSeries(_entireQueueCancellationToken);
        CancelExistingWork();
    }

    /// <summary>
    /// Cancels any outstanding work in this queue.  Work that has not yet started will never run. Work that is in
    /// progress will request cancellation in a standard best effort fashion.
    /// </summary>
    public void CancelExistingWork()
    {
        lock (_gate)
        {
            // Cancel out the current executing batch, and create a new token for the next batch.
            _nextBatchCancellationToken = _cancellationSeries.CreateNext();

            // Clear out the existing items that haven't run yet.  There is no point keeping them around now.
            _nextBatch.Clear();
            _uniqueItems.Clear();
        }
    }

    public void AddWork(TItem item, bool cancelExistingWork = false)
    {
        using var _ = ArrayBuilderPool<TItem>.GetPooledObject(out var items);
        items.Add(item);

        AddWork(items, cancelExistingWork);
    }

    public void AddWork(IEnumerable<TItem> items, bool cancelExistingWork = false)
    {
        // Don't do any more work if we've been asked to shutdown.
        if (_entireQueueCancellationToken.IsCancellationRequested)
        {
            return;
        }

        lock (_gate)
        {
            // if we were asked to cancel the prior set of items, do so now.
            if (cancelExistingWork)
            {
                CancelExistingWork();
            }

            // add our work to the set we'll process in the next batch.
            AddItemsToBatch(items);

            if (!_taskInFlight)
            {
                // No in-flight task.  Kick one off to process these messages a second from now.
                // We always attach the task to the previous one so that notifications to the ui
                // follow the same order as the notification the OOP server sent to us.
                _updateTask = ContinueAfterDelayAsync(_updateTask);
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

            // Otherwise, we dedupe and prefer the first item that we see.
            foreach (var item in items)
            {
                if (_uniqueItems.Add(item))
                {
                    _nextBatch.Add(item);
                }
            }
        }

        async Task<TResult?> ContinueAfterDelayAsync(Task lastTask)
        {
            // Await the previous item in the task chain in a non-throwing fashion.  Regardless of whether that last
            // task completed successfully or not, we want to move onto the next batch.
            await lastTask.NoThrowAwaitable(captureContext: false);

            // If we were asked to shutdown, immediately transition to the canceled state without doing any more work.
            _entireQueueCancellationToken.ThrowIfCancellationRequested();

            // Ensure that we always yield the current thread this is necessary for correctness as we are called
            // inside a lock that _taskInFlight to true.  We must ensure that the work to process the next batch
            // must be on another thread that runs afterwards, can only grab the thread once we release it and will
            // then reset that bool back to false
            await Task.Yield().ConfigureAwait(false);
            await Task.Delay(_delay, _entireQueueCancellationToken).ConfigureAwait(false);
            var result = await ProcessNextBatchAsync().ConfigureAwait(false);

            // Not worried about the lock here because we don't want to fire the event under the lock, which means
            // there is no effective way to avoid a race. The event doesn't guarantee that there will never be any
            // more work anyway, it's merely a best effort.
            if (_idleAction is { } idleAction && _nextBatch.Count == 0)
            {
                idleAction();
            }

            return result;
        }
    }

    /// <summary>
    /// Waits until the current batch of work completes and returns the last value successfully computed from <see
    /// cref="_processBatchAsync"/>.  If the last <see cref="_processBatchAsync"/> canceled or failed, then a
    /// corresponding canceled or faulted task will be returned that propagates that outwards.
    /// </summary>
    public Task<TResult?> WaitUntilCurrentBatchCompletesAsync()
    {
        lock (_gate)
        {
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
            return _updateTask;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
        }
    }

    private async ValueTask<TResult?> ProcessNextBatchAsync()
    {
        _entireQueueCancellationToken.ThrowIfCancellationRequested();
        try
        {
            var (nextBatch, batchCancellationToken) = GetNextBatchAndResetQueue();

            // We may have no items if the entire batch was canceled (and no new work was added).
            if (nextBatch.IsEmpty)
            {
                return default;
            }

            var batchResultTask = _processBatchAsync(nextBatch, batchCancellationToken).Preserve();
            await batchResultTask.NoThrowAwaitable(false);

            if (batchResultTask.IsCompletedSuccessfully)
            {
                // The VS threading library analyzers warn here because we're accessing Result
                // directly, which can be block. However, this is safe because we've already awaited
                // the task and verified that it completed successfully.
#pragma warning disable VSTHRD103
                return batchResultTask.Result;
#pragma warning restore VSTHRD103
            }
            else if (batchResultTask.IsCanceled && !_entireQueueCancellationToken.IsCancellationRequested)
            {
                // Don't bubble up cancellation to the queue for the nested batch cancellation.  Just because we decided
                // to cancel this batch isn't something that should stop processing further batches.
                return default;
            }
            else
            {
                // Realize the completed result to force the exception to be thrown.
                batchResultTask.VerifyCompleted();

                return Assumed.Unreachable<TResult?>();
            }
        }
        catch (OperationCanceledException)
        {
            // Silently continue to allow the next batch to be processed.
            return default;
        }
    }

    private (ImmutableArray<TItem> items, CancellationToken batchCancellationToken) GetNextBatchAndResetQueue()
    {
        lock (_gate)
        {
            var nextBatch = _nextBatch.ToImmutable();

            // mark there being no existing update task so that the next OOP notification will
            // kick one off.
            _nextBatch.Clear();
            _uniqueItems.Clear();
            _taskInFlight = false;

            return (nextBatch, _nextBatchCancellationToken);
        }
    }
}
