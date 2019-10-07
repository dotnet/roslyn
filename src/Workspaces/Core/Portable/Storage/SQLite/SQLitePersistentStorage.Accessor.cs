// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
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

            // Cache the statement strings we want to execute per accessor.  This way we avoid
            // allocating these strings each time we execute a command.  We also cache the prepared
            // statements (at the connection level) we make for each of these strings.  That way we
            // only incur the parsing cost once. After that, we can use the same prepared statements
            // and just bind the appropriate values it needs into it.
            //
            // values like 0, 1, 2 in the name are the `?`s in the sql string that will need to be
            // bound to runtime values appropriately when executed.

            private readonly string _select_rowid_from_main_table_where_0;
            private readonly string _select_rowid_from_writecache_table_where_0;
            private readonly string _insert_or_replace_into_writecache_table_values_0_1_2;
            private readonly string _delete_from_writecache_table;
            private readonly string _insert_or_replace_into_main_table_select_star_from_writecache_table;

            public Accessor(SQLitePersistentStorage storage)
            {
                Storage = storage;
                _select_rowid_from_main_table_where_0 = $@"select rowid from {MainDBName}.{DataTableName} where ""{DataIdColumnName}"" = ?";
                _select_rowid_from_writecache_table_where_0 = $@"select rowid from {WriteCacheDBName}.{DataTableName} where ""{DataIdColumnName}"" = ?";
                _insert_or_replace_into_writecache_table_values_0_1_2 = $@"insert or replace into {WriteCacheDBName}.{DataTableName}(""{DataIdColumnName}"",""{ChecksumColumnName}"",""{DataColumnName}"") values (?,?,?)";
                _delete_from_writecache_table = $"delete from {WriteCacheDBName}.{DataTableName};";
                _insert_or_replace_into_main_table_select_star_from_writecache_table = $"insert or replace into {MainDBName}.{DataTableName} select * from {WriteCacheDBName}.{DataTableName};";
            }

            protected abstract string DataTableName { get; }

            protected abstract bool TryGetDatabaseId(SqlConnection connection, TKey key, out TDatabaseId dataId);
            protected abstract void BindFirstParameter(SqlStatement statement, TDatabaseId dataId);
            protected abstract TWriteQueueKey GetWriteQueueKey(TKey key);

            [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/36114", AllowCaptures = false)]
            public Task<Checksum> ReadChecksumAsync(TKey key, CancellationToken cancellationToken)
                => Storage.PerformReadAsync(
                    t => t.self.ReadChecksum(t.key, t.cancellationToken),
                    (self: this, key, cancellationToken),
                    cancellationToken);

            private Checksum ReadChecksum(TKey key, CancellationToken cancellationToken)
            {
                using (var stream = ReadBlobColumn(key, ChecksumColumnName, checksumOpt: null, cancellationToken))
                using (var reader = ObjectReader.TryGetReader(stream, cancellationToken))
                {
                    if (reader != null)
                    {
                        return Checksum.ReadFrom(reader);
                    }
                }

                return null;
            }

            [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/36114", AllowCaptures = false)]
            public Task<Stream> ReadStreamAsync(TKey key, Checksum checksum, CancellationToken cancellationToken)
                => Storage.PerformReadAsync(
                    t => t.self.ReadStream(t.key, t.checksum, t.cancellationToken),
                    (self: this, key, checksum, cancellationToken),
                    cancellationToken);

            [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/36114", AllowCaptures = false)]
            private Stream ReadStream(TKey key, Checksum checksum, CancellationToken cancellationToken)
                => ReadBlobColumn(key, DataColumnName, checksum, cancellationToken);

            private Stream ReadBlobColumn(
                TKey key, string columnName, Checksum checksumOpt, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Storage._shutdownTokenSource.IsCancellationRequested)
                {
                    using var pooledConnection = Storage.GetPooledConnection();

                    var connection = pooledConnection.Connection;
                    if (TryGetDatabaseId(pooledConnection.Connection, key, out var dataId))
                    {
                        // First, try to see if there was a write to this key in our in-memory db.
                        // If it wasn't in the in-memory write-cache.  Check the full on-disk file.
                        return ReadBlob(connection, writeCacheDB: true, dataId, columnName, checksumOpt, cancellationToken) ??
                               ReadBlob(connection, writeCacheDB: false, dataId, columnName, checksumOpt, cancellationToken);
                    }
                }

                return null;
            }

            public Task<bool> WriteStreamAsync(TKey key, Stream stream, Checksum checksumOpt, CancellationToken cancellationToken)
                => Storage.PerformWriteAsync(
                    t => t.self.WriteStream(t.key, t.stream, t.checksumOpt, t.cancellationToken),
                    (self: this, key, stream, checksumOpt, cancellationToken),
                    cancellationToken);

            private bool WriteStream(TKey key, Stream stream, Checksum checksumOpt, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Storage._shutdownTokenSource.IsCancellationRequested)
                {
                    using var pooledConnection = Storage.GetPooledConnection();
                    var connection = pooledConnection.Connection;

                    // Determine the appropriate data-id to store this stream at.
                    if (TryGetDatabaseId(pooledConnection.Connection, key, out var dataId))
                    {
                        var (checksumBytes, checksumLength, checksumPooled) = GetBytes(checksumOpt, cancellationToken);
                        var (dataBytes, dataLength, dataPooled) = GetBytes(stream);

                        // Write the information into the in-memory write-cache.  Later on a bg task
                        // will move it from the in-memory cache to the on-disk db in a bulk transaction.
                        InsertOrReplaceBlobIntoWriteCache(
                            connection, dataId, checksumBytes, checksumLength, dataBytes, dataLength);

                        if (dataPooled)
                        {
                            ReturnPooledBytes(dataBytes);
                        }

                        if (checksumPooled)
                        {
                            ReturnPooledBytes(checksumBytes);
                        }

                        return true;
                    }
                }

                return false;
            }

            private Stream ReadBlob(
                SqlConnection connection, bool writeCacheDB,
                TDatabaseId dataId, string columnName,
                Checksum checksumOpt, CancellationToken cancellationToken)
            {
                try
                {
                    // Note: it's possible that someone may write to this row between when we
                    // get the row ID above and now.  That's fine.  We'll just read the new
                    // bytes that have been written to this location.  Note that only the
                    // data for a row in our system can change, the ID will always stay the
                    // same, and the data will always be valid for our ID.  So there is no
                    // safety issue here.
                    if (TryGetRowId(connection, writeCacheDB, dataId, out var rowId))
                    {
                        // Have to run the blob reading in a transaction.  This is necessary
                        // for two reasons.  First, blob reading outside a transaction is not
                        // safe to do with the sqlite API.  It may produce corrupt bits if 
                        // another thread is writing to the blob.  Second, if a checksum was
                        // passed in, we need to validate that the checksums match.  This is
                        // only safe if we are in a transaction and no-one else can race with
                        // us.
                        return connection.RunInTransaction(
                            ValidateChecksumAndReadBlob,
                            (self: this, connection, writeCacheDB, columnName, checksumOpt, rowId, cancellationToken));
                    }
                }
                catch (Exception ex)
                {
                    StorageDatabaseLogger.LogException(ex);
                }

                return null;

                static Stream ValidateChecksumAndReadBlob(
                    (Accessor<TKey, TWriteQueueKey, TDatabaseId> self, SqlConnection connection, bool writeCacheDB,
                     string columnName, Checksum checksumOpt, long rowId, CancellationToken cancellationToken) t)
                {
                    // If we were passed a checksum, make sure it matches what we have
                    // stored in the table already.  If they don't match, don't read
                    // out the data value at all.
                    if (t.checksumOpt != null)
                    {
                        if (!t.self.ChecksumsMatch_MustRunInTransaction(
                                t.connection, t.writeCacheDB, t.rowId,
                                t.checksumOpt, t.cancellationToken))
                        {
                            return null;
                        }
                    }

                    return t.connection.ReadBlob_MustRunInTransaction(t.writeCacheDB, t.self.DataTableName, t.columnName, t.rowId);
                }
            }

            private bool ChecksumsMatch_MustRunInTransaction(
                SqlConnection connection, bool writeCacheDB, long rowId, Checksum checksum, CancellationToken cancellationToken)
            {
                using var checksumStream = connection.ReadBlob_MustRunInTransaction(writeCacheDB, DataTableName, ChecksumColumnName, rowId);
                using var reader = ObjectReader.TryGetReader(checksumStream, cancellationToken);

                return reader != null && Checksum.ReadFrom(reader) == checksum;
            }

            protected bool GetAndVerifyRowId(SqlConnection connection, bool writeCacheDB, long dataId, out long rowId)
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
                if (TryGetRowIdWorker(connection, writeCacheDB, (TDatabaseId)(object)dataId, out rowId))
                {
                    Debug.Assert(dataId == rowId);
                }
#endif

                // Can just return out dataId as the rowId without actually having to hit the 
                // database at all.
                rowId = dataId;
                return true;
            }

            protected virtual bool TryGetRowId(SqlConnection connection, bool writeCacheDB, TDatabaseId dataId, out long rowId)
                => TryGetRowIdWorker(connection, writeCacheDB, dataId, out rowId);

            private bool TryGetRowIdWorker(SqlConnection connection, bool writeCacheDB, TDatabaseId dataId, out long rowId)
            {
                // See https://sqlite.org/autoinc.html
                // > In SQLite, table rows normally have a 64-bit signed integer ROWID which is 
                // unique among all rows in the same table. (WITHOUT ROWID tables are the exception.)
                // 
                // You can access the ROWID of an SQLite table using one of the special column names 
                // ROWID, _ROWID_, or OID. Except if you declare an ordinary table column to use one 
                // of those special names, then the use of that name will refer to the declared column
                // not to the internal ROWID.
                using (var resettableStatement = connection.GetResettableStatement(writeCacheDB
                    ? _select_rowid_from_writecache_table_where_0
                    : _select_rowid_from_main_table_where_0))
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

            private void InsertOrReplaceBlobIntoWriteCache(
                SqlConnection connection, TDatabaseId dataId,
                byte[] checksumBytes, int checksumLength,
                byte[] dataBytes, int dataLength)
            {
                using (var resettableStatement = connection.GetResettableStatement(_insert_or_replace_into_writecache_table_values_0_1_2))
                {
                    var statement = resettableStatement.Statement;

                    // Binding indices are 1 based.
                    BindFirstParameter(statement, dataId);
                    statement.BindBlobParameter(parameterIndex: 2, value: checksumBytes, length: checksumLength);
                    statement.BindBlobParameter(parameterIndex: 3, value: dataBytes, length: dataLength);

                    statement.Step();
                }

                // Let the storage system know it should flush this information
                // to disk in the future.
                Storage.EnqueueFlushTask();
            }

            public void FlushInMemoryDataToDisk(SqlConnection connection)
            {
                if (!connection.IsInTransaction)
                {
                    throw new InvalidOperationException("Must flush tables within a transaction to ensure consistency");
                }

                // Efficient call to sqlite to just fully copy all data from one table to the
                // other.  No need to actually do any reading/writing of the data ourselves.
                using (var statement = connection.GetResettableStatement(_insert_or_replace_into_main_table_select_star_from_writecache_table))
                {
                    statement.Statement.Step();
                }

                // Now, just delete all the data from the write cache.
                using (var statement = connection.GetResettableStatement(_delete_from_writecache_table))
                {
                    statement.Statement.Step();
                }
            }
        }
    }
}
