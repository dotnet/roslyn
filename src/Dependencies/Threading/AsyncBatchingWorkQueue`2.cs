// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Threading;

/// <summary>
/// A queue where items can be added to to be processed in batches after some delay has passed. When processing
/// happens, all the items added since the last processing point will be passed along to be worked on.  Rounds of
/// processing happen serially, only starting up after a previous round has completed.
/// <para>
/// Failure to complete a particular batch (either due to cancellation or some faulting error) will not prevent
/// further batches from executing. The only thing that will permanently stop this queue from processing items is
/// calling <see cref="Dispose()"/>, or cancelling the <see cref="CancellationToken"/> passed to the constructor,
/// which is equivalent.
/// </para>
/// </summary>
internal class AsyncBatchingWorkQueue<TItem, TResult> : IDisposable
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

    /// <summary>
    /// Cancellation token controlling the entire queue.  Once this is triggered, we don't want to do any more work
    /// at all. This is cancelled by a call to <see cref="Dispose()"/>; the IsCancellationRequested flag of this token
    /// can be used as the "is disposed" flag for this object.
    /// </summary>
    private readonly CancellationTokenSource _entireQueueCancellationTokenSource;

    /// <summary>
    /// Cancellation series we use so we can cancel individual batches of work if requested.  The client of the
    /// queue can cancel existing work by either calling <see cref="CancelExistingWork"/> directly, or passing <see
    /// langword="true"/> to <see cref="AddWork(TItem, bool)"/>.  Work in the queue that has not started will be
    /// immediately discarded. The cancellation token passed to <see cref="_processBatchAsync"/> will be triggered
    /// allowing the client callback to cooperatively cancel the current batch of work it is performing.
    /// </summary>
    private readonly CancellationSeries _cancellationSeries;

    /// <summary>
    /// If our constructor was given a CancellationToken, the registration against that token to call Dispose().
    /// </summary>
    private readonly CancellationTokenRegistration _externalCancellationTokenRegistration;

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
    /// CancellationToken controlling the next batch of items to execute.
    /// </summary>
    private CancellationToken _nextBatchCancellationToken;

    /// <summary>
    /// Used if <see cref="_equalityComparer"/> is present to ensure only unique items are added to <see
    /// cref="_nextBatch"/>.
    /// </summary>
    private readonly SegmentedHashSet<TItem> _uniqueItems;

    /// <summary>
    /// Task kicked off to do the next batch of processing of <see cref="_nextBatch"/>. These
    /// tasks form a chain so that the next task only processes when the previous one completes.
    /// </summary>
    private Task<(bool ranToCompletion, TResult? result)> _updateTask = Task.FromResult((ranToCompletion: true, default(TResult?)));

    /// <summary>
    /// Whether or not there is an existing task in flight that will process the current batch
    /// of <see cref="_nextBatch"/>.  If there is an existing in flight task, we don't need to
    /// kick off a new one if we receive more work before it runs.
    /// </summary>
    private bool _taskInFlight = false;

    #endregion

    /// <param name="processBatchAsync">Callback to process queued work items.  The list of items passed in is
    /// guaranteed to always be non-empty.</param>
    public AsyncBatchingWorkQueue(
        TimeSpan delay,
        Func<ImmutableSegmentedList<TItem>, CancellationToken, ValueTask<TResult>> processBatchAsync,
        IEqualityComparer<TItem>? equalityComparer,
        IAsynchronousOperationListener asyncListener,
        CancellationToken cancellationToken = default)
    {
        _delay = delay;
        _processBatchAsync = processBatchAsync;
        _equalityComparer = equalityComparer;
        _asyncListener = asyncListener;
        _entireQueueCancellationTokenSource = new CancellationTokenSource();

        _uniqueItems = new SegmentedHashSet<TItem>(equalityComparer);

        // Combine with the queue cancellation token so that any batch is controlled by that token as well.
        _cancellationSeries = new CancellationSeries(_entireQueueCancellationTokenSource.Token);
        CancelExistingWork();

        // As a convenience, if we were given a cancellation token, this should be equivalent to calling Dispose().
        // We don't link _entireQueueCancellationTokenSource to this, since we want to ensure the Dispose() also cleans up any
        // queued items that were in our lists.
        _externalCancellationTokenRegistration = cancellationToken.Register(static @this => ((AsyncBatchingWorkQueue<TItem, TResult>)@this!).Dispose(), this);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            // If we've previously disposed, we don't need to do anything further
            if (_entireQueueCancellationTokenSource.IsCancellationRequested)
                return;

            // Cancel all work in the queue; this .Cancel() should stop the work, but we'll call CancelExistingWork() too to ensure
            // we've cleared out all items that haven't ran.
            CancelExistingWork();
            _entireQueueCancellationTokenSource.Cancel();
        }

        // This must be done outside of the lock: disposing a registration blocks if the registered callback is currently running.
        // If we did this inside the lock, the callback might be blocked waiting for a call to Dispose() to release the lock, but the
        // caller of Dispose() would be blocked on that registration. If we could drop netstandard support, we could just call Unregister() instead.
        _externalCancellationTokenRegistration.Dispose();
        _cancellationSeries.Dispose();
    }

    /// <summary>
    /// Cancels any outstanding work in this queue.  Work that has not yet started will never run. Work that is in
    /// progress will request cancellation in a standard best effort fashion.
    /// </summary>
    public void CancelExistingWork()
    {
        lock (_gate)
        {
            // If we've previously disposed, we don't need to do anything further
            if (_entireQueueCancellationTokenSource.IsCancellationRequested)
                return;

            // Cancel out the current executing batch, and create a new token for the next batch.
            _nextBatchCancellationToken = _cancellationSeries.CreateNext();

            // Clear out the existing items that haven't run yet.  There is no point keeping them around now.
            _nextBatch.Clear();
            _uniqueItems.Clear();
        }
    }

    public void AddWork(TItem item, bool cancelExistingWork = false)
    {
        AddWork([item], cancelExistingWork);
    }

    public void AddWork(ReadOnlySpan<TItem> items, bool cancelExistingWork = false)
    {
        lock (_gate)
        {
            // Don't do any more work if we've been asked to shutdown.
            if (_entireQueueCancellationTokenSource.IsCancellationRequested)
                return;

            // if we were asked to cancel the prior set of items, do so now.
            if (cancelExistingWork)
                CancelExistingWork();

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

        void AddItemsToBatch(ReadOnlySpan<TItem> items)
        {
            // no equality comparer.  We want to process all items.
            if (_equalityComparer == null)
            {
                foreach (var item in items)
                    _nextBatch.Add(item);
                return;
            }

            // We're deduping items.  Only add the item if it's the first time we've seen it.
            foreach (var item in items)
            {
                if (_uniqueItems.Add(item))
                    _nextBatch.Add(item);
            }
        }

        async Task<(bool ranToCompletion, TResult? result)> ContinueAfterDelayAsync(Task lastTask)
        {
            using var _ = _asyncListener.BeginAsyncOperation(nameof(AddWork));

            // Await the previous item in the task chain in a non-throwing fashion.  Regardless of whether that last
            // task completed successfully or not, we want to move onto the next batch.
            await lastTask.NoThrowAwaitableInternal(captureContext: false);

            // If we were asked to shutdown, immediately transition to the canceled state without doing any more work.
            if (_entireQueueCancellationTokenSource.IsCancellationRequested)
                return (ranToCompletion: false, default(TResult?));

            // Ensure that we always yield the current thread this is necessary for correctness as we are called
            // inside a lock that _taskInFlight to true.  We must ensure that the work to process the next batch
            // must be on another thread that runs afterwards, can only grab the thread once we release it and will
            // then reset that bool back to false
            await Task.Yield().ConfigureAwait(false);
            await _asyncListener.Delay(_delay, _entireQueueCancellationTokenSource.Token).NoThrowAwaitableInternal(false);

            // If we were asked to shutdown, immediately transition to the canceled state without doing any more work.
            if (_entireQueueCancellationTokenSource.IsCancellationRequested)
                return (ranToCompletion: false, default(TResult?));

            return (ranToCompletion: true, await ProcessNextBatchAsync().ConfigureAwait(false));
        }
    }

    /// <summary>
    /// Waits until the current batch of work completes and returns the last value successfully computed from <see
    /// cref="_processBatchAsync"/>.  If the last <see cref="_processBatchAsync"/> canceled or failed, then a
    /// corresponding canceled or faulted task will be returned that propagates that outwards.
    /// </summary>
    public async Task<TResult?> WaitUntilCurrentBatchCompletesAsync()
    {
        Task<(bool ranToCompletion, TResult? result)> updateTask;
        lock (_gate)
        {
            updateTask = _updateTask;
        }

        var (ranToCompletion, result) = await updateTask.ConfigureAwait(false);
        if (!ranToCompletion)
        {
            Debug.Assert(_entireQueueCancellationTokenSource.IsCancellationRequested);
            _entireQueueCancellationTokenSource.Token.ThrowIfCancellationRequested();
        }

        return result;
    }

    private async ValueTask<TResult?> ProcessNextBatchAsync()
    {
        _entireQueueCancellationTokenSource.Token.ThrowIfCancellationRequested();
        try
        {
            var (nextBatch, batchCancellationToken) = GetNextBatchAndResetQueue();

            // We may have no items if the entire batch was canceled (and no new work was added).
            if (nextBatch.IsEmpty)
                return default;

            var batchResultTask = _processBatchAsync(nextBatch, batchCancellationToken).Preserve();
            await batchResultTask.NoThrowAwaitableInternal(false);
            if (batchResultTask.IsCompletedSuccessfully)
                return batchResultTask.Result;
            else if (batchResultTask.IsCanceled && !_entireQueueCancellationTokenSource.IsCancellationRequested)
            {
                // Don't bubble up cancellation to the queue for the nested batch cancellation.  Just because we decided
                // to cancel this batch isn't something that should stop processing further batches.
                return default;
            }
            else
            {
                Contract.ThrowIfFalse(batchResultTask.IsCompleted);

                // Realize the completed result to force the exception to be thrown.
                batchResultTask.GetAwaiter().GetResult();

                throw ExceptionUtilities.Unreachable();
            }
        }
        catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, ErrorSeverity.Critical))
        {
            // Report an exception if the batch fails for a non-cancellation reason.
            //
            // Note: even though we propagate this exception outwards, we will still continue processing further
            // batches due to the `await NoThrowAwaitableInternal()` above.  The sentiment being that generally
            // failures are recoverable here, and we will have reported the error so we can see in telemetry if we
            // have a problem that needs addressing.
            //
            // Not this code is unreachable because ReportAndPropagateUnlessCanceled returns false along all codepaths.
            throw ExceptionUtilities.Unreachable();
        }
    }

    private (ImmutableSegmentedList<TItem> items, CancellationToken batchCancellationToken) GetNextBatchAndResetQueue()
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
