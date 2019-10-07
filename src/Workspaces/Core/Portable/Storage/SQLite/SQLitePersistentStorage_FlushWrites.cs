// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SQLite.Interop;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        private readonly object _flushGate = new object();

        /// <summary>
        /// Task kicked off to actually do the work of flushing all data to the DB. If we hear about
        /// new writes to the storage system we don't have to kick off another flush task if one is
        /// active.
        /// </summary>
        private Task _flushTask;

        private void EnqueueFlushTask()
        {
            lock (_flushGate)
            {
                // Check if we already have a flush task in flight.  If so, no need to make another.
                if (_flushTask == null)
                {
                    // We're performing writes, so we need to enqueue this work to the exclusive
                    // scheduler.  Because this happens in the future, we may end up getting run
                    // after the storage system has shutdown.  So we need to check to make sure we
                    // should still continue once we wake up.
                    var token = _shutdownTokenSource.Token;
                    _flushTask = Task.Delay(FlushAllDelayMS, token).ContinueWith(
                        s_flushInMemoryDataToDiskIfNotShutdown, this, token, TaskContinuationOptions.None, _readerWriterLock.ExclusiveScheduler);
                }
            }
        }

        private static readonly Action<Task, object> s_flushInMemoryDataToDiskIfNotShutdown = (Task _, object self) =>
        {
            var storage = (SQLitePersistentStorage)self;

            lock (storage._flushGate)
            {
                // Indicate that there is no outstanding write task.  The next request to 
                // write will cause one to be kicked off.
                storage._flushTask = null;

                if (storage._shutdownTokenSource.IsCancellationRequested)
                {
                    // Don't flush from a bg task if we've been asked to shutdown.  The shutdown
                    // logic in the storage service will take care of the final writes to the main
                    // db.
                    return;
                }

                // Haven't been shutdown.  Actually go and move any outstanding data to the real DB.
                using var connection = storage.GetPooledConnection();
                storage.FlushInMemoryDataToDisk_MustRunUnderLock(connection.Connection);
            }
        };

        private void FlushInMemoryDataToDisk_MustRunUnderLock(SqlConnection connection)
        {
            if (!Monitor.IsEntered(_flushGate))
            {
                throw new InvalidOperationException();
            }

            // Within a single transaction, bulk flush all the tables from our writecache
            // db to the main on-disk db.  Once that is done, within the same transaction,
            // clear the writecache tables so they can be filled by the next set of writes
            // coming in.
            connection.Connection.RunInTransaction(s_flushInMemoryDataToDisk, (self: this, connection.Connection));
        }

        private static readonly Action<(SQLitePersistentStorage self, SqlConnection connection)> s_flushInMemoryDataToDisk =
            t =>
            {
                var (self, connection) = t;
                self._solutionAccessor.FlushInMemoryDataToDisk(connection);
                self._projectAccessor.FlushInMemoryDataToDisk(connection);
                self._documentAccessor.FlushInMemoryDataToDisk(connection);
            };
    }
}
