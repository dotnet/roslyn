// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SQLite.Interop;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite.v2
{
    using static SQLitePersistentStorageConstants;

    internal partial class SQLitePersistentStorage
    {
        /// <summary>
        /// Abstracts out access to specific tables in the DB.  This allows us to share overall
        /// logic around cancellation/pooling/error-handling/etc, while still hitting different
        /// db tables.
        /// </summary>
        private abstract class Accessor<TKey, TWriteQueueKey, TDatabaseId> 
            where TDatabaseId : struct
        {
            protected readonly SQLitePersistentStorage Storage;

            private readonly ImmutableArray<(string name, string type)> _primaryKeys;

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

            public Accessor(
                SQLitePersistentStorage storage,
                ImmutableArray<(string name, string type)> primaryKeys)
            {
                Storage = storage;
                _primaryKeys = primaryKeys;

                var main = Database.Main.GetName();
                var writeCache = Database.WriteCache.GetName();

                // "X" = ? and "Y" = ? and "Z" = ?
                var whereClause = string.Join(" and ", primaryKeys.Select(k => $@"{k.name} = ?"));

                var dataTableName = this.TableName;
                _select_rowid_from_main_table_where_0 = $"select rowid from {main}.{dataTableName} where {whereClause}";
                _select_rowid_from_writecache_table_where_0 = $"select rowid from {writeCache}.{dataTableName} where {whereClause}";

                var allColumnNames = primaryKeys.Select(t => t.name).Concat(ChecksumColumnName).Concat(DataColumnName);

                _insert_or_replace_into_writecache_table_values_0_1_2 = $@"insert or replace into {writeCache}.{dataTableName}
                    ({string.Join(",", allColumnNames)})
                    values
                    ({string.Join(",", allColumnNames.Select(n => "?"))})";

                _delete_from_writecache_table = $"delete from {writeCache}.{dataTableName};";
                _insert_or_replace_into_main_table_select_star_from_writecache_table = $"insert or replace into {main}.{dataTableName} select * from {writeCache}.{dataTableName};";
            }

            protected abstract Table Table { get; }

            private string TableName
                => this.Table switch
                {
                    Table.Solution => SolutionDataTableName,
                    Table.Project => ProjectDataTableName,
                    Table.Document => DocumentDataTableName,
                    _ => throw ExceptionUtilities.UnexpectedValue(this.Table),
                };

            /// <summary>
            /// Gets the internal sqlite db-id (effectively the row-id for the doc or proj table, or just the string-id
            /// for the solution table) for the provided caller key.  This db-id will be looked up and returned if a
            /// mapping already exists for it in the db.  Otherwise, a guaranteed unique id will be created for it and
            /// stored in the db for the future.  This allows all associated data to be cheaply associated with the 
            /// simple ID, avoiding lots of db bloat if we used the full <paramref name="key"/> in numerous places.
            /// </summary>
            /// <param name="allowWrite">Whether or not the caller owns the write lock and thus is ok with the DB id
            /// being generated and stored for this component key when it currently does not exist.  If <see
            /// langword="false"/> then failing to find the key will result in <see langword="false"/> being returned.
            /// </param>
            protected abstract TDatabaseId? TryGetDatabaseId(SqlConnection connection, TKey key, bool allowWrite);
            protected abstract void BindPrimaryKeyParameters(SqlStatement statement, TDatabaseId dataId);
            protected abstract TWriteQueueKey GetWriteQueueKey(TKey key);

            public void CreateTable(SqlConnection connection, Database database)
            {
                connection.ExecuteCommand($"""
                    create table if not exists {database.GetName()}.{this.TableName}(
                        {string.Join(", ", _primaryKeys.Select(k => $@"{k.name} {k.type} not null"))},
                        "{ChecksumColumnName}" blob,
                        "{DataColumnName}" blob,
                        primary key({string.Join(", ", _primaryKeys.Select(k => k.name))})
                    )
                    """);
            }

            [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/36114", AllowCaptures = false)]
            public Task<bool> ChecksumMatchesAsync(TKey key, Checksum checksum, CancellationToken cancellationToken)
                => Storage.PerformReadAsync(
                    static t => t.self.ChecksumMatches(t.key, t.checksum, t.cancellationToken),
                    (self: this, key, checksum, cancellationToken), cancellationToken);

            private bool ChecksumMatches(TKey key, Checksum checksum, CancellationToken cancellationToken)
            {
                var optional = ReadColumn(
                    key,
                    static (self, connection, database, rowId) => self.ReadChecksum(connection, database, rowId),
                    this,
                    cancellationToken);
                return optional.HasValue && checksum == optional.Value;
            }

            [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/36114", AllowCaptures = false)]
            public Task<Stream?> ReadStreamAsync(TKey key, Checksum? checksum, CancellationToken cancellationToken)
                => Storage.PerformReadAsync(
                    static t => t.self.ReadStream(t.key, t.checksum, t.cancellationToken),
                    (self: this, key, checksum, cancellationToken), cancellationToken);

            [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/36114", AllowCaptures = false)]
            private Stream? ReadStream(TKey key, Checksum? checksum, CancellationToken cancellationToken)
            {
                var optional = ReadColumn(
                    key,
                    static (t, connection, database, rowId) => t.self.ReadDataBlob(connection, database, rowId, t.checksum),
                    (self: this, checksum),
                    cancellationToken);

                Contract.ThrowIfTrue(optional.HasValue && optional.Value == null);
                return optional.HasValue ? optional.Value : null;
            }

            private Optional<T> ReadColumn<T, TData>(
                TKey key,
                Func<TData, SqlConnection, Database, long, Optional<T>> readColumn,
                TData data,
                CancellationToken cancellationToken)
            {
                // We're reading.  All current scenarios have this happening under the concurrent/read-only scheduler.
                // If this assert fires either a bug has been introduced, or there is a valid scenario for a writing
                // codepath to read a column and this assert should be adjusted.
                Contract.ThrowIfFalse(TaskScheduler.Current == Storage._connectionPoolService.Scheduler.ConcurrentScheduler);

                cancellationToken.ThrowIfCancellationRequested();

                if (!Storage._shutdownTokenSource.IsCancellationRequested)
                {
                    using var _ = Storage._connectionPool.Target.GetPooledConnection(out var connection);

                    // We're in the reading-only scheduler path, so we can't allow TryGetDatabaseId to write.  Note that
                    // this is ok, and actually provides the semantics we want.  Specifically, we can be trying to read
                    // data that either exists in the DB or not.  If it doesn't exist in the DB, then it's fine to fail
                    // to map from the key to a DB id (since there's nothing to lookup anyways).  And if it does exist
                    // in the db then finding the ID would succeed (without writing) and we could continue.
                    var dataId = TryGetDatabaseId(connection, key, allowWrite: false);
                    if (dataId != null)
                    {
                        try
                        {
                            // First, try to see if there was a write to this key in our in-memory db.
                            // If it wasn't in the in-memory write-cache.  Check the full on-disk file.

                            var optional = ReadColumnHelper(connection, Database.WriteCache, dataId.Value);
                            if (optional.HasValue)
                                return optional;

                            optional = ReadColumnHelper(connection, Database.Main, dataId.Value);
                            if (optional.HasValue)
                                return optional;
                        }
                        catch (Exception ex)
                        {
                            StorageDatabaseLogger.LogException(ex);
                        }
                    }
                }

                return default;

                Optional<T> ReadColumnHelper(SqlConnection connection, Database database, TDatabaseId dataId)
                {
                    // Note: it's possible that someone may write to this row between when we get the row ID
                    // above and now.  That's fine.  We'll just read the new bytes that have been written to
                    // this location.  Note that only the data for a row in our system can change, the ID will
                    // always stay the same, and the data will always be valid for our ID.  So there is no
                    // safety issue here.
                    return TryGetActualRowIdFromDatabase(connection, database, dataId, out var writeCacheRowId)
                        ? readColumn(data, connection, database, writeCacheRowId)
                        : default;
                }
            }

            public Task<bool> WriteStreamAsync(TKey key, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
                => Storage.PerformWriteAsync(
                    static t => t.self.WriteStream(t.key, t.stream, t.checksum, t.cancellationToken),
                    (self: this, key, stream, checksum, cancellationToken), cancellationToken);

            private bool WriteStream(TKey key, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
            {
                // We're writing.  This better always be under the exclusive scheduler.
                Contract.ThrowIfFalse(TaskScheduler.Current == Storage._connectionPoolService.Scheduler.ExclusiveScheduler);

                cancellationToken.ThrowIfCancellationRequested();

                if (!Storage._shutdownTokenSource.IsCancellationRequested)
                {
                    using var _ = Storage._connectionPool.Target.GetPooledConnection(out var connection);

                    // Determine the appropriate data-id to store this stream at.  We already are running
                    // with an exclusive write lock on the DB, so it's safe for us to write the data id to 
                    // the db on this connection if we need to.
                    var dataId = TryGetDatabaseId(connection, key, allowWrite: true);
                    if (dataId != null)
                    {
                        checksum ??= Checksum.Null;
                        Span<byte> checksumBytes = stackalloc byte[Checksum.HashSize];
                        checksum.WriteTo(checksumBytes);

                        var (dataBytes, dataLength, dataPooled) = GetBytes(stream);

                        // Write the information into the in-memory write-cache.  Later on a background task
                        // will move it from the in-memory cache to the on-disk db in a bulk transaction.
                        InsertOrReplaceBlobIntoWriteCache(
                            connection, dataId.Value,
                            checksumBytes,
                            new ReadOnlySpan<byte>(dataBytes, 0, dataLength));

                        if (dataPooled)
                            ReturnPooledBytes(dataBytes);

                        return true;
                    }
                }

                return false;
            }

            private Optional<Stream> ReadDataBlob(
                SqlConnection connection, Database database, long rowId, Checksum? checksum)
            {
                // Have to run the blob reading in a transaction.  This is necessary
                // for two reasons.  First, blob reading outside a transaction is not
                // safe to do with the sqlite API.  It may produce corrupt bits if
                // another thread is writing to the blob.  Second, if a checksum was
                // passed in, we need to validate that the checksums match.  This is
                // only safe if we are in a transaction and no-one else can race with
                // us.
                var (stream, exception) = connection.RunInTransaction(
                    static t =>
                    {
                        // If we were passed a checksum, make sure it matches what we have
                        // stored in the table already.  If they don't match, don't read
                        // out the data value at all.
                        if (t.checksum != null &&
                            !t.self.ChecksumsMatch_MustRunInTransaction(t.connection, t.database, t.rowId, t.checksum))
                        {
                            return default;
                        }

                        return t.connection.ReadDataBlob_MustRunInTransaction(t.database, t.self.Table, t.rowId);
                    },
                    (self: this, connection, database, checksum, rowId),
                    throwOnSqlException: true);

                // we should never have gotten a SqlException while reading since we passed throwOnSqlException: true above.
                Contract.ThrowIfTrue(exception != null);

                return stream;
            }

            private Optional<Checksum.HashData> ReadChecksum(
                SqlConnection connection, Database database, long rowId)
            {
                // Have to run the checksum reading in a transaction.  This is necessary as blob reading outside a
                // transaction is not safe to do with the sqlite API.  It may produce corrupt bits if another thread is
                // writing to the blob.
                var (stream, exception) = connection.RunInTransaction(
                    static t => t.connection.ReadChecksum_MustRunInTransaction(t.database, t.self.Table, t.rowId),
                    (self: this, connection, database, rowId),
                    throwOnSqlException: true);

                // we should never have gotten a SqlException while reading since we passed throwOnSqlException: true above.
                Contract.ThrowIfTrue(exception != null);

                return stream;
            }

            private bool ChecksumsMatch_MustRunInTransaction(SqlConnection connection, Database database, long rowId, Checksum checksum)
            {
                var storedChecksum = connection.ReadChecksum_MustRunInTransaction(database, Table, rowId);
                return storedChecksum.HasValue && checksum == storedChecksum.Value;
            }

            private bool TryGetActualRowIdFromDatabase(SqlConnection connection, Database database, TDatabaseId dataId, out long rowId)
            {
                // See https://sqlite.org/autoinc.html
                // > In SQLite, table rows normally have a 64-bit signed integer ROWID which is
                // unique among all rows in the same table. (WITHOUT ROWID tables are the exception.)
                //
                // You can access the ROWID of an SQLite table using one of the special column names
                // ROWID, _ROWID_, or OID. Except if you declare an ordinary table column to use one
                // of those special names, then the use of that name will refer to the declared column
                // not to the internal ROWID.
                using var resettableStatement = connection.GetResettableStatement(database == Database.WriteCache
                    ? _select_rowid_from_writecache_table_where_0
                    : _select_rowid_from_main_table_where_0);

                var statement = resettableStatement.Statement;

                BindPrimaryKeyParameters(statement, dataId);

                var stepResult = statement.Step();
                if (stepResult == Result.ROW)
                {
                    rowId = statement.GetInt64At(columnIndex: 0);
                    return true;
                }

                rowId = -1;
                return false;
            }

            private void InsertOrReplaceBlobIntoWriteCache(
                SqlConnection connection, TDatabaseId dataId,
                ReadOnlySpan<byte> checksumBytes,
                ReadOnlySpan<byte> dataBytes)
            {
                // We're writing.  This better always be under the exclusive scheduler.
                Contract.ThrowIfFalse(TaskScheduler.Current == Storage._connectionPoolService.Scheduler.ExclusiveScheduler);

                using (var resettableStatement = connection.GetResettableStatement(_insert_or_replace_into_writecache_table_values_0_1_2))
                {
                    var statement = resettableStatement.Statement;

                    BindPrimaryKeyParameters(statement, dataId);

                    // Binding indices are 1 based.
                    statement.BindBlobParameter(parameterIndex: _primaryKeys.Length + 1, checksumBytes);
                    statement.BindBlobParameter(parameterIndex: _primaryKeys.Length + 2, dataBytes);

                    statement.Step();
                }

                // Let the storage system know it should flush this information
                // to disk in the future.
                Storage.EnqueueFlushTask();
            }

            public void FlushInMemoryDataToDisk_MustRunInTransaction(SqlConnection connection)
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
