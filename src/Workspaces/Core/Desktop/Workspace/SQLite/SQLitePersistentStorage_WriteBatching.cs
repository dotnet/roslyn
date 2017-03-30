// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Storage;
using SQLite;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        // Batch up writes for performance.  Clients requesting data to be written will have
        // their requests added to _writeQueue.

        /// <summary>
        /// Lock protecting <see cref="_writeQueue"/> and <see cref="_writeQueueTask"/>.
        /// </summary>
        private readonly object _writeQueueGate = new object();

        /// <summary>
        /// Queue of actions we want to perform all at once against the DB in a single transaction.
        /// </summary>
        private readonly List<Action<SQLiteConnection>> _writeQueue = new List<Action<SQLiteConnection>>();

        /// <summary>
        /// Task kicked off to actually do the writing.
        /// </summary>
        private Task _writeQueueTask;

        private void AddWriteTask(Action<SQLiteConnection> action)
        {
            lock (_writeQueueGate)
            {
                // Add this action to the list of work to do.
                _writeQueue.Add(action);

                // If we don't have an outstanding request to write the queue to the DB
                // then create one to run a short while from now.  If there is an outstanding
                // request, then it will see this write request when it runs.
                if (_writeQueueTask == null)
                {
                    _writeQueueTask =
                        Task.Delay(500, _shutdownTokenSource.Token)
                            .ContinueWith(
                                _ => ProcessWriteQueue(),
                                _shutdownTokenSource.Token,
                                TaskContinuationOptions.None,
                                TaskScheduler.Default);
                }
            }
        }

        private void ProcessWriteQueue()
        {
            // Copy the work from _writeQueue to a local list that we can process.
            var tempQueue = ArrayBuilder<Action<SQLiteConnection>>.GetInstance();
            try
            {
                ProcessWriteQueue(tempQueue);
            }
            finally
            {
                tempQueue.Free();
            }
        }

        private void ProcessWriteQueue(
            ArrayBuilder<Action<SQLiteConnection>> tempQueue)
        {
            lock (_writeQueueGate)
            {
                // Copy the work from the shared list to the local copy.
                tempQueue.AddRange(_writeQueue);

                // clear the shared list so we don't process things multiple times.
                _writeQueue.Clear();

                // Indicate that there is no outstanding write task.  The next request to 
                // write will cause one to be kicked off.
                _writeQueueTask = null;
            }

            if (_shutdownTokenSource.Token.IsCancellationRequested)
            {
                // Don't actually try to perform any writes if we've been asked to shutdown.
                // Note: We still clear the queue and set the writetask to null to leave things
                // in a tidy state.
                return;
            }

            // Create a single connection we'll perform all our writes against.  Do all the
            // writes in a single transaction for large perf increase.
            try
            {
                var connection = CreateConnection();
                connection.RunInTransaction(() =>
                {
                    foreach (var action in tempQueue)
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