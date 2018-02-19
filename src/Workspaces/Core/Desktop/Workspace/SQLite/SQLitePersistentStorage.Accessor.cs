// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
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
            /// The task responsible for writing out all the batched actions we have for a particular
            /// queue.  When new reads come in for that queue they can 'await' this write-task completing
            /// so that all reads for the queue observe any previously completed writes.
            /// </summary>
            private readonly Dictionary<TWriteQueueKey, Task> _writeQueueKeyToWriteTask =
                new Dictionary<TWriteQueueKey, Task>();

            public Accessor(SQLitePersistentStorage storage)
            {
                Storage = storage;
            }

            protected abstract string DataTableName { get; }

            protected abstract bool TryGetDatabaseId(SqlConnection connection, TKey key, out TDatabaseId dataId);
            protected abstract void BindFirstParameter(SqlStatement statement, TDatabaseId dataId);
            protected abstract TWriteQueueKey GetWriteQueueKey(TKey key);

            public async Task<Stream> ReadStreamAsync(TKey key, CancellationToken cancellationToken)
            {
                // Note: we're technically fully synchronous.  However, we're called from several
                // async methods.  We just return a Task<stream> here so that all our callers don't
                // need to call Task.FromResult on us.

                cancellationToken.ThrowIfCancellationRequested();

                if (!Storage._shutdownTokenSource.IsCancellationRequested)
                {
                    bool haveDataId;
                    TDatabaseId dataId;
                    using (var pooledConnection = Storage.GetPooledConnection())
                    {
                        haveDataId = TryGetDatabaseId(pooledConnection.Connection, key, out dataId);
                    }

                    if (haveDataId)
                    {
                        // Ensure all pending document writes to this name are flushed to the DB so that 
                        // we can find them below.
                        await FlushPendingWritesAsync(key, cancellationToken).ConfigureAwait(false);

                        try
                        {
                            using (var pooledConnection = Storage.GetPooledConnection())
                            {
                                // Lookup the row from the DocumentData table corresponding to our dataId.
                                return ReadBlob(pooledConnection.Connection, dataId);
                            }
                        }
                        catch (Exception ex)
                        {
                            StorageDatabaseLogger.LogException(ex);
                        }
                    }
                }

                return null;
            }

            public async Task<bool> WriteStreamAsync(
                TKey key, Stream stream, CancellationToken cancellationToken)
            {
                // Note: we're technically fully synchronous.  However, we're called from several
                // async methods.  We just return a Task<bool> here so that all our callers don't
                // need to call Task.FromResult on us.

                cancellationToken.ThrowIfCancellationRequested();

                if (!Storage._shutdownTokenSource.IsCancellationRequested)
                {
                    bool haveDataId;
                    TDatabaseId dataId;
                    using (var pooledConnection = Storage.GetPooledConnection())
                    {
                        // Determine the appropriate data-id to store this stream at.
                        haveDataId = TryGetDatabaseId(pooledConnection.Connection, key, out dataId);
                    }

                    if (haveDataId)
                    {
                        var (bytes, length, pooled) = GetBytes(stream);

                        await AddWriteTaskAsync(key, con =>
                        {
                            InsertOrReplaceBlob(con, dataId, bytes, length);
                            if (pooled)
                            {
                                ReturnPooledBytes(bytes);
                            }
                        }, cancellationToken).ConfigureAwait(false);

                        return true;
                    }
                }

                return false;
            }

            private Task FlushPendingWritesAsync(TKey key, CancellationToken cancellationToken)
                => Storage.FlushSpecificWritesAsync(_writeQueueKeyToWrites, _writeQueueKeyToWriteTask, GetWriteQueueKey(key), cancellationToken);

            private Task AddWriteTaskAsync(TKey key, Action<SqlConnection> action, CancellationToken cancellationToken)
                => Storage.AddWriteTaskAsync(_writeQueueKeyToWrites, GetWriteQueueKey(key), action, cancellationToken);

            private Stream ReadBlob(SqlConnection connection, TDatabaseId dataId)
            {
                if (TryGetRowId(connection, dataId, out var rowId))
                {
                    // Note: it's possible that someone may write to this row between when we
                    // get the row ID above and now.  That's fine.  We'll just read the new
                    // bytes that have been written to this location.  Note that only the
                    // data for a row in our system can change, the ID will always stay the
                    // same, and the data will always be valid for our ID.  So there is no
                    // safety issue here.
                    return connection.ReadBlob(DataTableName, DataColumnName, rowId);
                }

                return null;
            }

            protected bool GetAndVerifyRowId(SqlConnection connection, long dataId, out long rowId)
            {
                // For the Document and Project tables, our dataId is our rowId:
                // 
                // https://sqlite.org/lang_createtable.html
                // if a rowid table has a primary key that consists of a single column and the 
                // declared type of that column is "INTEGER" in any mixture of upper and lower 
                // case, then the column becomes an alias for the rowid. Such a column is usually
                // referred to as an "integer primary key". A PRIMARY KEY column only becomes an
                // integer primary key if the declared type name is exactly "INTEGER"
#if DEBUG
                // make sure that if we actually request the rowId from the database that it
                // is equal to our data id.  Only do this in debug as this can be expensive
                // and we definitely do not want to do this in release.
                if (TryGetRowIdWorker(connection, (TDatabaseId)(object)dataId, out rowId))
                {
                    Debug.Assert(dataId == rowId);
                }
#endif

                // Can just return out dataId as the rowId without actually having to hit the 
                // database at all.
                rowId = dataId;
                return true;
            }

            protected virtual bool TryGetRowId(SqlConnection connection, TDatabaseId dataId, out long rowId)
                => TryGetRowIdWorker(connection, dataId, out rowId);

            private bool TryGetRowIdWorker(SqlConnection connection, TDatabaseId dataId, out long rowId)
            {
                // See https://sqlite.org/autoinc.html
                // > In SQLite, table rows normally have a 64-bit signed integer ROWID which is 
                // unique among all rows in the same table. (WITHOUT ROWID tables are the exception.)
                // 
                // You can access the ROWID of an SQLite table using one of the special column names 
                // ROWID, _ROWID_, or OID. Except if you declare an ordinary table column to use one 
                // of those special names, then the use of that name will refer to the declared column
                // not to the internal ROWID.
                using (var resettableStatement = connection.GetResettableStatement(
                    $@"select rowid from ""{this.DataTableName}"" where ""{DataIdColumnName}"" = ?"))
                {
                    var statement = resettableStatement.Statement;

                    // Binding indices are 1-based.
                    BindFirstParameter(statement, dataId);

                    var stepResult = statement.Step();
                    if (stepResult == Result.ROW)
                    {
                        rowId = statement.GetInt64At(columnIndex: 0);
                        return true;
                    }
                }

                rowId = -1;
                return false;
            }

            private void InsertOrReplaceBlob(
                SqlConnection conection, TDatabaseId dataId, byte[] bytes, int length)
            {
                using (var resettableStatement = conection.GetResettableStatement(
                    $@"insert or replace into ""{this.DataTableName}""(""{DataIdColumnName}"",""{DataColumnName}"") values (?,?)"))
                {
                    var statement = resettableStatement.Statement;

                    // Binding indices are 1 based.
                    BindFirstParameter(statement, dataId);
                    statement.BindBlobParameter(parameterIndex: 2, value: bytes, length: length);

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
