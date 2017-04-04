// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SQLite.Interop;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        /// <summary>
        /// Abstracts out access to specific tables in the DB.  This allows us to share overall
        /// logic around cancellation/pooling/error-handling/etc, while still hitting different
        /// db tables.
        /// </summary>
        private abstract class Accessor<TKey, TWriteQueueKey, TDatabaseId>
        {
            protected readonly SQLitePersistentStorage Storage;

            /// <summary>
            /// Queue of actions we want to perform all at once against the DB in a single transaction.
            /// </summary>
            private readonly MultiDictionary<TWriteQueueKey, Action<SqlConnection>> _writeQueueKeyToWrites =
                new MultiDictionary<TWriteQueueKey, Action<SqlConnection>>();

            /// <summary>
            /// Keep track of how many threads are trying to write out this particular queue.  All threads
            /// trying to write out the queue will wait until all the writes are done.
            /// </summary>
            private readonly Dictionary<TWriteQueueKey, CountdownEvent> _writeQueueKeyToCountdown =
                new Dictionary<TWriteQueueKey, CountdownEvent>();

            public Accessor(SQLitePersistentStorage storage)
            {
                Storage = storage;
            }

            protected abstract string DataTableName { get; }

            protected abstract bool TryGetDatabaseId(SqlConnection connection, TKey key, out TDatabaseId dataId);
            protected abstract void BindFirstParameter(SqlStatement statement, TDatabaseId dataId);
            protected abstract TWriteQueueKey GetWriteQueueKey(TKey key);

            public Task<Stream> ReadStreamAsync(TKey key, CancellationToken cancellationToken)
            {
                // Note: we're technically fully synchronous.  However, we're called from several
                // async methods.  We just return a Task<stream> here so that all our callers don't
                // need to call Task.FromResult on us.

                cancellationToken.ThrowIfCancellationRequested();

                if (!Storage._shutdownTokenSource.IsCancellationRequested)
                {
                    using (var pooledConnection = Storage.GetPooledConnection())
                    {
                        var connection = pooledConnection.Connection;
                        if (TryGetDatabaseId(connection, key, out var dataId))
                        {
                            // Ensure all pending document writes to this name are flushed to the DB so that 
                            // we can find them below.
                            FlushPendingWrites(connection, key);

                            byte[] data = null;
                            try
                            {
                                // Lookup the row from the DocumentData table corresponding to our dataId.
                                data = FindBlob(connection, dataId);
                            }
                            catch (Exception ex)
                            {
                                StorageDatabaseLogger.LogException(ex);
                            }

                            if (data != null)
                            {
                                return Task.FromResult<Stream>(new MemoryStream(data, writable: false));
                            }
                        }
                    }
                }

                return SpecializedTasks.Default<Stream>();
            }

            public Task<bool> WriteStreamAsync(
                TKey key, Stream stream, CancellationToken cancellationToken)
            {
                // Note: we're technically fully synchronous.  However, we're called from several
                // async methods.  We just return a Task<bool> here so that all our callers don't
                // need to call Task.FromResult on us.

                cancellationToken.ThrowIfCancellationRequested();

                if (!Storage._shutdownTokenSource.IsCancellationRequested)
                {
                    using (var pooledConnection = Storage.GetPooledConnection())
                    {
                        // Determine the appropriate data-id to store this stream at.
                        if (TryGetDatabaseId(pooledConnection.Connection, key, out var dataId))
                        {
                            var bytes = GetBytes(stream);

                            AddWriteTask(key, con =>
                            {
                                InsertOrReplaceBlob(con, dataId, bytes);
                            });

                            return SpecializedTasks.True;
                        }
                    }
                }

                return SpecializedTasks.False;
            }

            private void FlushPendingWrites(SqlConnection connection, TKey key)
                => Storage.FlushSpecificWrites(
                    connection, _writeQueueKeyToWrites, _writeQueueKeyToCountdown, GetWriteQueueKey(key));

            private void AddWriteTask(TKey key, Action<SqlConnection> action)
                => Storage.AddWriteTask(_writeQueueKeyToWrites, GetWriteQueueKey(key), action);

            private byte[] FindBlob(
                SqlConnection connection, TDatabaseId dataId)
            {
                using (var resettableStatement = connection.GetResettableStatement(
                    $@"select * from ""{this.DataTableName}"" where ""{IdColumnName}"" = ?"))
                {
                    var statement = resettableStatement.Statement;

                    // Binding indices are 1-based.
                    BindFirstParameter(statement, dataId);

                    var stepResult = statement.Step();
                    if (stepResult == Result.ROW)
                    {
                        // "Id" is column 0, "Data" is column 1.
                        return statement.GetBlobAt(columnIndex: 1);
                    }
                }

                return null;
            }

            private void InsertOrReplaceBlob(
                SqlConnection conection, TDatabaseId dataId, byte[] bytes)
            {
                using (var resettableStatement = conection.GetResettableStatement(
                    $@"insert or replace into ""{this.DataTableName}""(""{IdColumnName}"",""{DataColumnName}"") values (?,?)"))
                {
                    var statement = resettableStatement.Statement;

                    // Binding indices are 1 based.
                    BindFirstParameter(statement, dataId);
                    statement.BindBlobParameter(parameterIndex: 2, value: bytes);

                    statement.Step();
                }
            }

            public void AddAndClearAllPendingWrites(ArrayBuilder<Action<SqlConnection>> result)
            {
                // Copy the pending work we have to the result copy.
                result.AddRange(_writeQueueKeyToWrites.SelectMany(kvp => kvp.Value));

                // Clear out the collection so we don't process things multiple times.
                _writeQueueKeyToWrites.Clear();
            }
        }
    }
}