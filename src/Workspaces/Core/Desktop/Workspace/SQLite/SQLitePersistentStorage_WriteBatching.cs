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
        private readonly List<(bool isSolution, ProjectId, DocumentId, Action<SQLiteConnection>)> _writeQueue =
            new List<(bool isSolution, ProjectId, DocumentId, Action<SQLiteConnection>)>();

        /// <summary>
        /// Task kicked off to actually do the writing.
        /// </summary>
        private Task _writeQueueTask;

        private void AddSolutionWriteTask(Action<SQLiteConnection> action)
        {
            AddWriteTask((isSolution: true, projectId: null, documentId: null, action));
        }

        private void AddProjectWriteTask(ProjectId projectId, Action<SQLiteConnection> action)
        {
            AddWriteTask((isSolution: false, projectId, documentId: null, action));
        }

        private void AddDocumentWriteTask(DocumentId documentId, Action<SQLiteConnection> action)
        {
            AddWriteTask((isSolution: false, projectId: null, documentId, action));
        }

        private void AddWriteTask((bool isSolution, ProjectId projectId, DocumentId documentId, Action<SQLiteConnection>) writeAction)
        {
            lock (_writeQueueGate)
            {
                // Add this action to the list of work to do.
                _writeQueue.Add(writeAction);

                // If we don't have an outstanding request to write the queue to the DB
                // then create one to run a short while from now.  If there is an outstanding
                // request, then it will see this write request when it runs.
                if (_writeQueueTask == null)
                {
                    _writeQueueTask =
                        Task.Delay(500, _shutdownTokenSource.Token)
                            .ContinueWith(
                                _ => FlushPendingWrites((_1, _2, _3) => true),
                                _shutdownTokenSource.Token,
                                TaskContinuationOptions.None,
                                TaskScheduler.Default);
                }
            }
        }

        private void FlushPendingWrites(Func<bool, ProjectId, DocumentId, bool> predicate)
        {
            // Copy the work from _writeQueue to a local list that we can process.
            var writesToProcess = ArrayBuilder<Action<SQLiteConnection>>.GetInstance();
            try
            {
                ProcessWriteQueue(predicate, writesToProcess);
            }
            finally
            {
                writesToProcess.Free();
            }
        }

        private void ProcessWriteQueue(
            Func<bool, ProjectId, DocumentId, bool> predicate,
            ArrayBuilder<Action<SQLiteConnection>> writesToProcess)
        {
            lock (_writeQueueGate)
            {
                // Copy the work from the shared list to the local copy.

                var writesToKeep = ArrayBuilder<(bool, ProjectId, DocumentId, Action<SQLiteConnection>)>.GetInstance();

                foreach (var (isSolution, projId, docId, action) in _writeQueue)
                {
                    if (predicate(isSolution, projId, docId))
                    {
                        writesToProcess.Add(action);
                    }
                    else
                    {
                        writesToKeep.Add((isSolution, projId, docId, action));
                    }
                }

                // clear the shared list so we don't process things multiple times.
                _writeQueue.Clear();
                _writeQueue.AddRange(writesToKeep);
                writesToKeep.Free();

                // Indicate that there is no outstanding write task.  The next request to 
                // write will cause one to be kicked off.
                _writeQueueTask = null;
            }

            if (writesToProcess.Count == 0)
            {
                return;
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