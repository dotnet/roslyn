// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;
using SQLite;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        // Batch up writes for performance.  Clients requesting data to be written will have
        // their requests added to _writeQueue.

        /// <summary>
        /// Lock protecting the write queues and <see cref="_writeQueueTask"/>.
        /// </summary>
        private readonly object _writeQueueGate = new object();

        /// <summary>
        /// Queue of actions we want to perform all at once against the DB in a single transaction.
        /// </summary>
        private readonly MultiDictionary<string, Action<SQLiteConnection>> _solutionWriteQueue = new MultiDictionary<string, Action<SQLiteConnection>>();
        private readonly MultiDictionary<(ProjectId, string), Action<SQLiteConnection>> _projectWriteQueue = new MultiDictionary<(ProjectId, string), Action<SQLiteConnection>>();
        private readonly MultiDictionary<(DocumentId, string), Action<SQLiteConnection>> _documentWriteQueue = new MultiDictionary<(DocumentId, string), Action<SQLiteConnection>>();

        /// <summary>
        /// Task kicked off to actually do the writing.
        /// </summary>
        private Task _writeQueueTask;

        private void AddSolutionWriteTask(string name, Action<SQLiteConnection> action)
        {
            lock (_writeQueueGate)
            {
                _solutionWriteQueue.Add(name, action);
                CreateWriteTaskIfNecessary();
            }
        }

        private void AddProjectWriteTask(ProjectId projectId, string name, Action<SQLiteConnection> action)
        {
            lock (_writeQueueGate)
            {
                _projectWriteQueue.Add((projectId, name), action);
                CreateWriteTaskIfNecessary();
            }
        }

        private void AddDocumentWriteTask(DocumentId documentId, string name, Action<SQLiteConnection> action)
        {
            lock (_writeQueueGate)
            {
                _documentWriteQueue.Add((documentId, name), action);
                CreateWriteTaskIfNecessary();
            }
        }

        private void CreateWriteTaskIfNecessary()
        {
            // If we don't have an outstanding request to write the queue to the DB
            // then create one to run a short while from now.  If there is an outstanding
            // request, then it will see this write request when it runs.
            if (_writeQueueTask == null)
            {
                _writeQueueTask =
                    Task.Delay(500, _shutdownTokenSource.Token)
                        .ContinueWith(
                            _ => FlushAllPendingWrites(),
                            _shutdownTokenSource.Token,
                            TaskContinuationOptions.None,
                            TaskScheduler.Default);
            }
        }

        private void FlushPendingSolutionWrites(string name, SQLiteConnection connection)
        {
            var writesToProcess = ArrayBuilder<Action<SQLiteConnection>>.GetInstance();
            try
            {
                lock (_writeQueueGate)
                {
                    writesToProcess.AddRange(_solutionWriteQueue[name]);
                    _solutionWriteQueue.Remove(name);
                }

                ProcessWriteQueue(connection, writesToProcess);
            }
            finally
            {
                writesToProcess.Free();
            }
        }

        private void FlushPendingProjectWrites(SQLiteConnection connection, ProjectId projectId, string name)
        {
            var writesToProcess = ArrayBuilder<Action<SQLiteConnection>>.GetInstance();
            try
            {
                lock (_writeQueueGate)
                {
                    var tuple = (projectId, name);
                    writesToProcess.AddRange(_projectWriteQueue[tuple]);
                    _projectWriteQueue.Remove(tuple);
                }

                ProcessWriteQueue(connection, writesToProcess);
            }
            finally
            {
                writesToProcess.Free();
            }
        }

        private void FlushPendingDocumentWrites(SQLiteConnection connection, DocumentId documentId, string name)
        {
            var writesToProcess = ArrayBuilder<Action<SQLiteConnection>>.GetInstance();
            try
            {
                lock (_writeQueueGate)
                {
                    var tuple = (documentId, name);
                    writesToProcess.AddRange(_documentWriteQueue[tuple]);
                    _documentWriteQueue.Remove(tuple);
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
            var writesToProcess = ArrayBuilder<Action<SQLiteConnection>>.GetInstance();
            try
            {
                lock (_writeQueueGate)
                {
                    // Copy the work from the shared collections to the local copy.
                    writesToProcess.AddRange(_solutionWriteQueue.SelectMany(kvp => kvp.Value));
                    writesToProcess.AddRange(_projectWriteQueue.SelectMany(kvp => kvp.Value));
                    writesToProcess.AddRange(_documentWriteQueue.SelectMany(kvp => kvp.Value));

                    // clear the shared collections so we don't process things multiple times.
                    _solutionWriteQueue.Clear();
                    _projectWriteQueue.Clear();
                    _documentWriteQueue.Clear();

                    // Indicate that there is no outstanding write task.  The next request to 
                    // write will cause one to be kicked off.
                    _writeQueueTask = null;
                }

                ProcessWriteQueue(CreateConnection(), writesToProcess);
            }
            finally
            {
                writesToProcess.Free();
            }
        }

        private void ProcessWriteQueue(
            SQLiteConnection connection,
            ArrayBuilder<Action<SQLiteConnection>> writesToProcess)
        { 
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