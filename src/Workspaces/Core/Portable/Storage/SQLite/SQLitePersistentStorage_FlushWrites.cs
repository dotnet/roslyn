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
                        TaskScheduler.Default);
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

            using var connection = GetPooledConnection();

            // Within a single transaction, bulk flush all the tables from our writecache
            // db to the main on-disk db.  Once that is done, within the same transaction,
            // clear the writecache tables so they can be filled by the next set of writes
            // coming in.
            connection.Connection.RunInTransaction(connection =>
            {
                CopyFromInMemoryToDisk(connection, SolutionDataTableName);
                CopyFromInMemoryToDisk(connection, ProjectDataTableName);
                CopyFromInMemoryToDisk(connection, DocumentDataTableName);

                ClearInMemoryTable(connection, SolutionDataTableName);
                ClearInMemoryTable(connection, ProjectDataTableName);
                ClearInMemoryTable(connection, DocumentDataTableName);
            }, connection.Connection);

            return;

            static void CopyFromInMemoryToDisk(SqlConnection connection, string tableName)
            {
                if (!connection.IsInTransaction)
                {
                    throw new InvalidOperationException("Must clear tables within a transaction to ensure consistency");
                }

                // Efficient call to sqlite to just fully copy all data from one table to the
                // other.  No need to actually do any reading/writing of the data ourselves.
                using var statement = connection.GetResettableStatement($"INSERT INTO '${MainDBName}.${tableName}' SELECT * from '${WriteCacheDBName}.${tableName}';");
                statement.Statement.Step();
            }

            static void ClearInMemoryTable(SqlConnection connection, string tableName)
            {
                if (!connection.IsInTransaction)
                {
                    throw new InvalidOperationException("Must copy tables within a transaction to ensure consistency");
                }

                using var statement = connection.GetResettableStatement($"DELETE FROM '${WriteCacheDBName}.${tableName}';");
                statement.Statement.Step();
            }
        }
    }
}
