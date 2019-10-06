// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        private readonly object _flushGate = new object();

        private Task _flushTask;
        private bool _flushToPrimaryDatabase;

        private void EnqueueFlushTask()
        {
            lock (_flushGate)
            {
                _flushToPrimaryDatabase = true;
            }
        }

        private async Task FlushInMemoryDataToDiskAsync()
        {
            while (!_shutdownTokenSource.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(500, _shutdownTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }

                lock (this._flushGate)
                {
                    if (!_flushToPrimaryDatabase)
                    {
                        continue;
                    }

                    _flushToPrimaryDatabase = false;
                }

                using var connection = GetPooledConnection();

                Console.WriteLine("Flushing");

                // Within a single transaction, bulk flush all the tables from our writecache
                // db to the main on-disk db.  Once that is done, within the same transaction,
                // clear the writecache tables so they can be filled by the next set of writes
                // coming in.
                connection.Connection.RunInTransaction(
                    performsWrites: true,
                    tuple =>
                    {
                        var connection = tuple.Connection;
                        tuple.self._solutionAccessor.FlushInMemoryDataToDisk(connection);
                        tuple.self._projectAccessor.FlushInMemoryDataToDisk(connection);
                        tuple.self._documentAccessor.FlushInMemoryDataToDisk(connection);
                    }, (self: this, connection.Connection));
            }
        }
    }
}
