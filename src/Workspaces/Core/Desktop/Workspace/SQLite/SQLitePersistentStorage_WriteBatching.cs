// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
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
        private readonly object _writeQueueGate = new object();

        /// <summary>
        /// Task kicked off to actually do the work of flushing all data to the DB.
        /// </summary>
        private Task _flushAllTask;

        private void AddWriteTask<TKey>(MultiDictionary<TKey, Action<SqlConnection>> queue, TKey key, Action<SqlConnection> action)
        {
            lock (_writeQueueGate)
            {
                queue.Add(key, action);

                // If we don't have an outstanding request to write the queue to the DB
                // then create one to run a short while from now.  If there is an outstanding
                // request, then it will see this write request when it runs.
                if (_flushAllTask == null)
                {
                    _flushAllTask =
                        Task.Delay(500, _shutdownTokenSource.Token)
                            .ContinueWith(
                                _ => FlushAllPendingWrites(),
                                _shutdownTokenSource.Token,
                                TaskContinuationOptions.None,
                                TaskScheduler.Default);
                }
            }
        }

        private void FlushSpecificWrites<TKey>(
            SqlConnection connection, MultiDictionary<TKey, Action<SqlConnection>> queue, TKey key)
        {
            var writesToProcess = ArrayBuilder<Action<SqlConnection>>.GetInstance();
            try
            {
                lock (_writeQueueGate)
                {
                    // Get the writes we need to process.
                    writesToProcess.AddRange(queue[key]);

                    // and clear them from the queues so we don't process things multiple times.
                    queue.Remove(key);
                }

                ProcessWriteQueue(connection, writesToProcess);
            }
            finally
            {
                writesToProcess.Free();
            }
        }

        private void FlushAllPendingWrites()
        {
            // Copy the work from _writeQueue to a local list that we can process.
            var writesToProcess = ArrayBuilder<Action<SqlConnection>>.GetInstance();
            try
            {
                lock (_writeQueueGate)
                {
                    // Copy the pending work the accessors have to the local copy.
                    _solutionAccessor.AddAndClearAllPendingWrites(writesToProcess);
                    _projectAccessor.AddAndClearAllPendingWrites(writesToProcess);
                    _documentAccessor.AddAndClearAllPendingWrites(writesToProcess);

                    // Indicate that there is no outstanding write task.  The next request to 
                    // write will cause one to be kicked off.
                    _flushAllTask = null;
                }

                using (var pooledConnection = GetPooledConnection())
                {
                    ProcessWriteQueue(pooledConnection.Connection, writesToProcess);
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