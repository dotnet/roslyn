// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SQLite.Interop;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        private readonly object _flushTaskGate = new object();

        /// <summary>
        /// Task kicked off to actually do the work of flushing all data to the DB. If we hear about
        /// new writes to the storage system we don't have to kick off another flush task if one is
        /// active.
        /// </summary>
        private Task _flushTask;

        private void EnqueueFlushTask()
        {
            lock (_flushTaskGate)
            {
                // Check if we already have a flush task in flight.  If so, no need to make another.
                if (_flushTask == null)
                {
                    var token = _shutdownTokenSource.Token;
                    _flushTask = Task.Delay(FlushAllDelayMS, token).ContinueWith(
                        _ => FlushInMemoryDataToDisk(force: false),
                        token,
                        TaskContinuationOptions.None,
                        _readerWriterLock.ExclusiveScheduler);
                }
            }
        }

        private void FlushInMemoryDataToDisk(bool force)
        {
            lock (this._flushTaskGate)
            {
                // Indicate that there is no outstanding write task.  The next request to 
                // write will cause one to be kicked off.
                _flushTask = null;

                if (!force && _shutdownTokenSource.IsCancellationRequested)
                {
                    // Don't flush from a bg task if we've been asked to shutdown.  The shutdown
                    // logic in the storage service will take care of the final writes to the main
                    // db.
                    return;
                }

                using var connection = GetPooledConnection();

                // Within a single transaction, bulk flush all the tables from our writecache
                // db to the main on-disk db.  Once that is done, within the same transaction,
                // clear the writecache tables so they can be filled by the next set of writes
                // coming in.
                connection.Connection.RunInTransaction(
                    FlushInMemoryDataToDisk,
                    (self: this, connection.Connection));
            }

            static void FlushInMemoryDataToDisk((SQLitePersistentStorage self, SqlConnection connection) tuple)
            {
                var (self, connection) = tuple;
                self._solutionAccessor.FlushInMemoryDataToDisk(connection);
                self._projectAccessor.FlushInMemoryDataToDisk(connection);
                self._documentAccessor.FlushInMemoryDataToDisk(connection);
            }
        }
    }
}
