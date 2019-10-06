// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        private readonly object _flushTaskGate = new object();

        /// <summary>
        /// Task kicked off to actually do the work of flushing all data to the DB.
        /// </summary>
        private Task _flushTask;

        private void EnqueueFlushTask()
        {
            lock (_flushTaskGate)
            {
                if (_flushTask == null)
                {
                    var token = _shutdownTokenSource.Token;
                    _flushTask = Task.Delay(FlushAllDelayMS, token).ContinueWith(
                        _ => FlushInMemoryDataToDisk(),
                        token,
                        TaskContinuationOptions.None,
                        _pair.ExclusiveScheduler);
                }
            }
        }

        private void FlushInMemoryDataToDisk()
        {
            // Indicate that there is no outstanding write task.  The next request to 
            // write will cause one to be kicked off.
            lock (this._flushTaskGate)
            {
                _flushTask = null;
            }

            Console.WriteLine("Flushing");

            using var connection = GetPooledConnection();

            // Within a single transaction, bulk flush all the tables from our writecache
            // db to the main on-disk db.  Once that is done, within the same transaction,
            // clear the writecache tables so they can be filled by the next set of writes
            // coming in.
            connection.Connection.RunInTransaction(tuple =>
            {
                var connection = tuple.Connection;
                tuple.self._solutionAccessor.FlushInMemoryDataToDisk(connection);
                tuple.self._projectAccessor.FlushInMemoryDataToDisk(connection);
                tuple.self._documentAccessor.FlushInMemoryDataToDisk(connection);
            }, (self: this, connection.Connection));
        }
    }
}
