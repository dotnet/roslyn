// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.SQLite.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite.v1.Interop
{
    /// <summary>
    /// Encapsulates a connection to a sqlite database.  On construction an attempt will be made
    /// to open the DB if it exists, or create it if it does not.
    /// 
    /// Connections are considered relatively heavyweight and are pooled until the <see cref="SQLitePersistentStorage"/>
    /// is <see cref="SQLitePersistentStorage.Dispose"/>d.  Connections can be used by different threads,
    /// but only as long as they are used by one thread at a time.  They are not safe for concurrent
    /// use by several threads.
    /// 
    /// <see cref="SqlStatement"/>s can be created through the user of <see cref="GetResettableStatement"/>.
    /// These statements are cached for the lifetime of the connection and are only finalized
    /// (i.e. destroyed) when the connection is closed.
    /// </summary>
    internal class SqlConnection
    {
        /// <summary>
        /// The raw handle to the underlying DB.
        /// </summary>
        private readonly SafeSqliteHandle _handle;

#pragma warning disable IDE0052 // Remove unread private members - TODO: Can this field be removed?
        /// <summary>
        /// For testing purposes to simulate failures during testing.
        /// </summary>
        private readonly IPersistentStorageFaultInjector _faultInjector;
#pragma warning restore IDE0052 // Remove unread private members

        /// <summary>
        /// Our cache of prepared statements for given sql strings.
        /// </summary>
        private readonly Dictionary<string, SqlStatement> _queryToStatement;

        /// <summary>
        /// Whether or not we're in a transaction.  We currently don't supported nested transactions.
        /// If we want that, we can achieve it through sqlite "save points".  However, that's adds a 
        /// lot of complexity that is nice to avoid.
        /// </summary>
        public bool IsInTransaction { get; private set; }

        public static SqlConnection Create(IPersistentStorageFaultInjector faultInjector, string databasePath)
        {
            faultInjector?.OnNewConnection();

            // Allocate dictionary before doing any sqlite work.  That way if it throws
            // we don't have to do any additional cleanup.
            var queryToStatement = new Dictionary<string, SqlStatement>();

            // Use SQLITE_OPEN_NOMUTEX to enable multi-thread mode, where multiple connections can be used provided each
            // one is only used from a single thread at a time.
            // see https://sqlite.org/threadsafe.html for more detail
            var flags = OpenFlags.SQLITE_OPEN_CREATE | OpenFlags.SQLITE_OPEN_READWRITE | OpenFlags.SQLITE_OPEN_NOMUTEX;
            var handle = NativeMethods.sqlite3_open_v2(databasePath, (int)flags, vfs: null, out var result);

            if (result != Result.OK)
            {
                handle.Dispose();
                throw new SqlException(result, $"Could not open database file: {databasePath} ({result})");
            }

            try
            {
                NativeMethods.sqlite3_busy_timeout(handle, (int)TimeSpan.FromMinutes(1).TotalMilliseconds);
                return new SqlConnection(handle, faultInjector, queryToStatement);
            }
            catch
            {
                // If we failed to create connection, ensure that we still release the sqlite
                // handle.
                handle.Dispose();
                throw;
            }
        }

        private SqlConnection(SafeSqliteHandle handle, IPersistentStorageFaultInjector faultInjector, Dictionary<string, SqlStatement> queryToStatement)
        {
            _handle = handle;
            _faultInjector = faultInjector;
            _queryToStatement = queryToStatement;
        }

        internal void Close_OnlyForUseBySqlPersistentStorage()
        {
            // Dispose of the underlying handle at the end of cleanup
            using var _ = _handle;

            // release all the cached statements we have.
            //
            // use the struct-enumerator of our dictionary to prevent any allocations here.  We
            // don't want to risk an allocation causing an OOM which prevents executing the
            // following cleanup code.
            foreach (var (_, statement) in _queryToStatement)
            {
                statement.Close_OnlyForUseBySqlConnection();
            }

            _queryToStatement.Clear();
        }

        public void ExecuteCommand(string command, bool throwOnError = true)
        {
            using var resettableStatement = GetResettableStatement(command);
            var statement = resettableStatement.Statement;
            var result = statement.Step(throwOnError);
            if (result != Result.DONE && throwOnError)
            {
                Throw(result);
            }
        }

        public ResettableSqlStatement GetResettableStatement(string query)
        {
            if (!_queryToStatement.TryGetValue(query, out var statement))
            {
                var handle = NativeMethods.sqlite3_prepare_v2(_handle, query, out var result);
                try
                {
                    ThrowIfNotOk(result);

                    statement = new SqlStatement(this, handle);
                    _queryToStatement[query] = statement;
                }
                catch
                {
                    handle.Dispose();
                    throw;
                }
            }

            return new ResettableSqlStatement(statement);
        }

        public void RunInTransaction<TState>(Action<TState> action, TState state)
        {
            RunInTransaction(
                state =>
                {
                    state.action(state.state);
                    return (object)null;
                },
                (action, state));
        }

        public TResult RunInTransaction<TState, TResult>(Func<TState, TResult> action, TState state)
        {
            try
            {
                if (IsInTransaction)
                {
                    throw new InvalidOperationException("Nested transactions not currently supported");
                }

                IsInTransaction = true;

                ExecuteCommand("begin transaction");
                var result = action(state);
                ExecuteCommand("commit transaction");
                return result;
            }
            catch (SqlException ex) when (ex.Result == Result.FULL ||
                                          ex.Result == Result.IOERR ||
                                          ex.Result == Result.BUSY ||
                                          ex.Result == Result.LOCKED ||
                                          ex.Result == Result.NOMEM)
            {
                // See documentation here: https://sqlite.org/lang_transaction.html
                // If certain kinds of errors occur within a transaction, the transaction 
                // may or may not be rolled back automatically. The errors that can cause 
                // an automatic rollback include:

                // SQLITE_FULL: database or disk full
                // SQLITE_IOERR: disk I/ O error
                // SQLITE_BUSY: database in use by another process
                // SQLITE_LOCKED: database in use by another connection in the same process
                // SQLITE_NOMEM: out or memory

                // It is recommended that applications respond to the errors listed above by
                // explicitly issuing a ROLLBACK command. If the transaction has already been
                // rolled back automatically by the error response, then the ROLLBACK command 
                // will fail with an error, but no harm is caused by this.
                Rollback(throwOnError: false);
                throw;
            }
            catch (Exception)
            {
                Rollback(throwOnError: true);
                throw;
            }
            finally
            {
                IsInTransaction = false;
            }
        }

        private void Rollback(bool throwOnError)
            => ExecuteCommand("rollback transaction", throwOnError);

        public int LastInsertRowId()
            => (int)NativeMethods.sqlite3_last_insert_rowid(_handle);

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/36114", AllowCaptures = false)]
        public Stream ReadBlob_MustRunInTransaction(string tableName, string columnName, long rowId)
        {
            // NOTE: we do need to do the blob reading in a transaction because of the
            // following: https://www.sqlite.org/c3ref/blob_open.html
            //
            // If the row that a BLOB handle points to is modified by an UPDATE, DELETE, 
            // or by ON CONFLICT side-effects then the BLOB handle is marked as "expired".
            // This is true if any column of the row is changed, even a column other than
            // the one the BLOB handle is open on. Calls to sqlite3_blob_read() and 
            // sqlite3_blob_write() for an expired BLOB handle fail with a return code of
            // SQLITE_ABORT.
            if (!IsInTransaction)
            {
                throw new InvalidOperationException("Must read blobs within a transaction to prevent corruption!");
            }

            const int ReadOnlyFlags = 0;

            using var blob = NativeMethods.sqlite3_blob_open(_handle, "main", tableName, columnName, rowId, ReadOnlyFlags, out var result);

            if (result == Result.ERROR)
            {
                // can happen when rowId points to a row that hasn't been written to yet.
                return null;
            }

            ThrowIfNotOk(result);

            return ReadBlob(blob);
        }

        private Stream ReadBlob(SafeSqliteBlobHandle blob)
        {
            var length = NativeMethods.sqlite3_blob_bytes(blob);

            // If it's a small blob, just read it into one of our pooled arrays, and then
            // create a PooledStream over it. 
            if (length <= SQLitePersistentStorage.MaxPooledByteArrayLength)
            {
                return ReadBlobIntoPooledStream(blob, length);
            }
            else
            {
                // Otherwise, it's a large stream.  Just take the hit of allocating.
                var bytes = new byte[length];
                ThrowIfNotOk(NativeMethods.sqlite3_blob_read(blob, bytes, length, offset: 0));
                return new MemoryStream(bytes);
            }
        }

        private Stream ReadBlobIntoPooledStream(SafeSqliteBlobHandle blob, int length)
        {
            var bytes = SQLitePersistentStorage.GetPooledBytes();
            try
            {
                ThrowIfNotOk(NativeMethods.sqlite3_blob_read(blob, bytes, length, offset: 0));

                // Copy those bytes into a pooled stream
                return SerializableBytes.CreateReadableStream(bytes, length);
            }
            finally
            {
                // Return our small array back to the pool.
                SQLitePersistentStorage.ReturnPooledBytes(bytes);
            }
        }

        public void ThrowIfNotOk(int result)
            => ThrowIfNotOk((Result)result);

        public void ThrowIfNotOk(Result result)
            => ThrowIfNotOk(_handle, result);

        public static void ThrowIfNotOk(SafeSqliteHandle handle, Result result)
        {
            if (result != Result.OK)
            {
                Throw(handle, result);
            }
        }

        public void Throw(Result result)
            => Throw(_handle, result);

        public static void Throw(SafeSqliteHandle handle, Result result)
        {
            throw new SqlException(result,
                NativeMethods.sqlite3_errmsg(handle) + Environment.NewLine +
                NativeMethods.sqlite3_errstr(NativeMethods.sqlite3_extended_errcode(handle)));
        }
    }
}
