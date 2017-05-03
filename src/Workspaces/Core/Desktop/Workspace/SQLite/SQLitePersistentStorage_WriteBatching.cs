// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
                    var delay =
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
            SqlConnection connection,
            MultiDictionary<TKey, Action<SqlConnection>> keyToWriteActions,
            Dictionary<TKey, Task> keyToWriteTask,
            TKey key, CancellationToken cancellationToken)
        {
            var writesToProcess = ArrayBuilder<Action<SqlConnection>>.GetInstance();
            try
            {
                await FlushSpecificWritesAsync(
                    connection, keyToWriteActions, keyToWriteTask, key, writesToProcess, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                writesToProcess.Free();
            }
        }

        private async Task FlushSpecificWritesAsync<TKey>(
            SqlConnection connection, MultiDictionary<TKey, Action<SqlConnection>> keyToWriteActions,
            Dictionary<TKey, Task> keyToWriteTask, TKey key,
            ArrayBuilder<Action<SqlConnection>> writesToProcess,
            CancellationToken cancellationToken)
        {
            // Get the task that is responsible for doing the writes for this queue.
            // This task will complete when all previously enqueued writes for this queue
            // complete, and all the currently enqueued writes for this queue complete as well.
            var writeTask = await GetWriteTask().ConfigureAwait(false);
            await writeTask.ConfigureAwait(false);

            return;

            // Local functions
            async Task<Task> GetWriteTask()
            {
                // Have to acquire the semaphore.  We're going to mutate the shared 'keyToWriteActions'
                // and 'keyToWriteTask' collections.
                using (await _writeQueueGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // Get the writes we need to process.
                    writesToProcess.AddRange(keyToWriteActions[key]);

                    // and clear them from the queues so we don't process things multiple times.
                    keyToWriteActions.Remove(key);

                    // Find the existing task responsible for writing to this queue.
                    var existingWriteTask = keyToWriteTask.TryGetValue(key, out var task)
                        ? task
                        : SpecializedTasks.EmptyTask;

                    if (writesToProcess.Count == 0)
                    {
                        // We have no writes of our own.  But there may be an existing task that
                        // is writing out this queue.   Return this so our caller can wait for
                        // all existing writes to complete.
                        return existingWriteTask;
                    }

                    // We have our own writes to process.  Enqueue the task to write 
                    // these out after the existing write-task for this queue completes.
                    // 
                    // We're currently under a lock, so tell the continuation to run 
                    // *asynchronously* so that the TPL does not try to execute it inline
                    // with this thread.
                    var nextTask = existingWriteTask.ContinueWith(
                        _ => ProcessWriteQueue(connection, writesToProcess),
                        cancellationToken,
                        TaskContinuationOptions.RunContinuationsAsynchronously,
                        TaskScheduler.Default);

                    // Store this for the next flush call to see.
                    keyToWriteTask[key] = nextTask;

                    // And return this to our caller so it can 'await' all these writes completing.
                    return nextTask;
                }
            }
        }

        private async Task FlushAllPendingWritesAsync(CancellationToken cancellationToken)
        {
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
                        using (var pooledConnection = GetPooledConnection())
                        {
                            ProcessWriteQueue(pooledConnection.Connection, writesToProcess);
                        }
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
                connection.RunInTransaction(() =>
                {
                    foreach (var action in writesToProcess)
                    {
                        action(connection);
                    }
                });
            }
            catch (Exception ex)
            {
                StorageDatabaseLogger.LogException(ex);
            }
        }
    }
}