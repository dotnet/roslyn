// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SQLite.Interop;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        /// <summary>
        /// Lock protecting the write queues and <see cref="_flushAllTask"/>.
        /// </summary>
        private readonly SemaphoreSlim _writeQueueGate = new SemaphoreSlim(initialCount: 1);

        /// <summary>
        /// Task kicked off to actually do the work of flushing all data to the DB.
        /// </summary>
        private Task _flushAllTask;

        private async Task AddWriteTaskAsync<TKey>(
            MultiDictionary<TKey, Action<SqlConnection>> queue,
            TKey key, Action<SqlConnection> action,
            CancellationToken cancellationToken)
        {
            using (await _writeQueueGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                queue.Add(key, action);

                // If we don't have an outstanding request to write the queue to the DB
                // then create one to run a short while from now.  If there is an outstanding
                // request, then it will see this write request when it runs.
                if (_flushAllTask == null)
                {
                    var token = _shutdownTokenSource.Token;
                    _flushAllTask =
                        Task.Delay(FlushAllDelayMS, token)
                            .ContinueWith(
                                async _ => await FlushAllPendingWritesAsync(token).ConfigureAwait(false),
                                token,
                                TaskContinuationOptions.None,
                                TaskScheduler.Default);
                }
            }
        }

        private async Task FlushSpecificWritesAsync<TKey>(
            MultiDictionary<TKey, Action<SqlConnection>> keyToWriteActions,
            Dictionary<TKey, Task> keyToWriteTask,
            TKey key,
            CancellationToken cancellationToken)
        {
            var writesToProcess = ArrayBuilder<Action<SqlConnection>>.GetInstance();
            try
            {
                await FlushSpecificWritesAsync(keyToWriteActions, keyToWriteTask, key, writesToProcess, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                writesToProcess.Free();
            }
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/36114", AllowCaptures = false)]
        private async Task FlushSpecificWritesAsync<TKey>(
            MultiDictionary<TKey, Action<SqlConnection>> keyToWriteActions,
            Dictionary<TKey, Task> keyToWriteTask,
            TKey key,
            ArrayBuilder<Action<SqlConnection>> writesToProcess,
            CancellationToken cancellationToken)
        {
            // Get's the task representing the current writes being performed by another
            // thread for this queue+key, and a TaskCompletionSource we can use to let
            // other threads know about our own progress writing any new writes in this queue.
            var (previousWritesTask, taskCompletionSource) = await GetWriteTaskAsync(keyToWriteActions, keyToWriteTask, key, writesToProcess, cancellationToken).ConfigureAwait(false);
            try
            {
                // Wait for all previous writes to be flushed.
                await previousWritesTask.ConfigureAwait(false);

                if (writesToProcess.Count == 0)
                {
                    // No additional writes for us to flush.  We can immediately bail out.
                    Debug.Assert(taskCompletionSource == null);
                    return;
                }

                // Now, if we have writes of our own, do them on this thread.
                // 
                // Note: this flushing is not cancellable.  We've already removed the
                // writes from the write queue.  If we were not to write them out we
                // would be losing data.
                Debug.Assert(taskCompletionSource != null);

                using var pooledConnection = GetPooledConnection();
                ProcessWriteQueue(pooledConnection.Connection, writesToProcess);
            }
            catch (OperationCanceledException ex)
            {
                taskCompletionSource?.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                taskCompletionSource?.TrySetException(ex);
            }
            finally
            {
                // Mark our TCS as completed.  Any other threads waiting on us will now be able
                // to proceed.
                taskCompletionSource?.TrySetResult(0);
            }
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/36114", OftenCompletesSynchronously = true)]
        private async ValueTask<(Task previousTask, TaskCompletionSource<int> taskCompletionSource)> GetWriteTaskAsync<TKey>(
            MultiDictionary<TKey, Action<SqlConnection>> keyToWriteActions,
            Dictionary<TKey, Task> keyToWriteTask,
            TKey key,
            ArrayBuilder<Action<SqlConnection>> writesToProcess,
            CancellationToken cancellationToken)
        {
            // Have to acquire the semaphore.  We're going to mutate the shared 'keyToWriteActions'
            // and 'keyToWriteTask' collections.
            //
            // Note: by blocking on _writeQueueGate we are guaranteed to see all the writes
            // performed by FlushAllPendingWritesAsync.
            using (await _writeQueueGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // Get the writes we need to process. 
                // Note: explicitly foreach so we operate on the struct enumerator for
                // MultiDictionary.ValueSet.
                var actions = keyToWriteActions[key];
                writesToProcess.EnsureCapacity(writesToProcess.Count + actions.Count);
                foreach (var action in actions)
                {
                    writesToProcess.Add(action);
                }

                // and clear them from the queues so we don't process things multiple times.
                keyToWriteActions.Remove(key);

                // Find the existing task responsible for writing to this queue.
                var existingWriteTask = keyToWriteTask.TryGetValue(key, out var task)
                    ? task
                    : Task.CompletedTask;

                if (writesToProcess.Count == 0)
                {
                    // We have no writes of our own.  But there may be an existing task that
                    // is writing out this queue.   Return this so our caller can wait for
                    // all existing writes to complete.
                    return (previousTask: existingWriteTask, taskCompletionSource: null);
                }

                // Create a TCS that represents our own work writing out "writesToProcess".
                // Store it in keyToWriteTask so that if other threads come along, they'll
                // wait for us to complete before doing their own reads/writes on this queue.
                var localCompletionSource = new TaskCompletionSource<int>();

                keyToWriteTask[key] = localCompletionSource.Task;

                return (previousTask: existingWriteTask, taskCompletionSource: localCompletionSource);
            }
        }

        private async Task FlushAllPendingWritesAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Flushing 2:");

            // Copy the work from _writeQueue to a local list that we can process.
            var writesToProcess = ArrayBuilder<Action<SqlConnection>>.GetInstance();
            try
            {
                using (await _writeQueueGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // Copy the pending work the accessors have to the local copy.
                    _solutionAccessor.AddAndClearAllPendingWrites(writesToProcess);
                    _projectAccessor.AddAndClearAllPendingWrites(writesToProcess);
                    _documentAccessor.AddAndClearAllPendingWrites(writesToProcess);

                    // Indicate that there is no outstanding write task.  The next request to 
                    // write will cause one to be kicked off.
                    _flushAllTask = null;

                    // Note: we keep the lock while we're writing all.  That way if any reads come
                    // in and want to wait for the respective keys to be written, they will see the
                    // results of our writes after the lock is released.  Note: this is slightly
                    // heavyweight.  But as we're only doing these writes in bulk a couple of times
                    // a second max, this should not be an area of contention.
                    if (writesToProcess.Count > 0)
                    {
                        using var pooledConnection = GetPooledConnection();
                        ProcessWriteQueue(pooledConnection.Connection, writesToProcess);
                    }
                }
            }
            finally
            {
                writesToProcess.Free();
            }
        }

        private void ProcessWriteQueue(
            SqlConnection connection,
            ArrayBuilder<Action<SqlConnection>> writesToProcess)
        {
            if (writesToProcess.Count == 0)
            {
                return;
            }

            if (_shutdownTokenSource.Token.IsCancellationRequested)
            {
                // Don't actually try to perform any writes if we've been asked to shutdown.
                return;
            }

            try
            {
                // Create a transaction and perform all writes within it.
                connection.RunInTransaction(
                    state =>
                    {
                        foreach (var action in state.writesToProcess)
                        {
                            action(state.connection);
                        }
                    },
                    (writesToProcess, connection));
            }
            catch (Exception ex)
            {
                StorageDatabaseLogger.LogException(ex);
            }
        }
    }
}
