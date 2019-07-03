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

            using (var connection = GetPooledConnection())
            {
                connection.Connection.RunInTransaction(connection =>
                {
                    CopyFromInMemoryToDisk(connection, SolutionDataTableName);
                    CopyFromInMemoryToDisk(connection, ProjectDataTableName);
                    CopyFromInMemoryToDisk(connection, DocumentDataTableName);

                    ClearInMemoryTable(connection, SolutionDataTableName);
                    ClearInMemoryTable(connection, ProjectDataTableName);
                    ClearInMemoryTable(connection, DocumentDataTableName);
                }, connection.Connection);
            }
        }

        private void CopyFromInMemoryToDisk(SqlConnection connection, string tableName)
        {
            if (!connection.IsInTransaction)
            {
                throw new InvalidOperationException("Must clear tables within a transaction to prevent corruption!");
            }

            using var statement = connection.GetResettableStatement($"DELETE FROM '${WriteCacheDBName}.${tableName}';");
            statement.Statement.Step();
        }

        private void ClearInMemoryTable(SqlConnection connection, string tableName)
        {
            if (!connection.IsInTransaction)
            {
                throw new InvalidOperationException("Must clear tables within a transaction to prevent corruption!");
            }

            using var statement = connection.GetResettableStatement($"INSERT INTO '${MainDBName}.${tableName}' SELECT * from '${WriteCacheDBName}.${tableName}';");
            statement.Statement.Step();
        }
    }
}
