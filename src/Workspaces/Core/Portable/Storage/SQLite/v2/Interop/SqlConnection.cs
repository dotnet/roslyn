// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.SQLite.Interop;
using Roslyn.Utilities;
using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.v2.Interop;

using static SQLitePersistentStorageConstants;

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
    // Cached UTF-8 (and null terminated) versions of the common strings we need to pass to sqlite.  Used to prevent
    // having to convert these names to/from utf16 to UTF-8 on every call.  Sqlite requires these be null terminated.

    private static readonly byte[] s_mainNameWithTrailingZero = GetUtf8BytesWithTrailingZero(Database.Main.GetName());
    private static readonly byte[] s_writeCacheNameWithTrailingZero = GetUtf8BytesWithTrailingZero(Database.WriteCache.GetName());

    private static readonly byte[] s_solutionTableNameWithTrailingZero = GetUtf8BytesWithTrailingZero(SolutionDataTableName);
    private static readonly byte[] s_projectTableNameWithTrailingZero = GetUtf8BytesWithTrailingZero(ProjectDataTableName);
    private static readonly byte[] s_documentTableNameWithTrailingZero = GetUtf8BytesWithTrailingZero(DocumentDataTableName);

    private static readonly byte[] s_checksumColumnNameWithTrailingZero = GetUtf8BytesWithTrailingZero(ChecksumColumnName);
    private static readonly byte[] s_dataColumnNameWithTrailingZero = GetUtf8BytesWithTrailingZero(DataColumnName);

    private static byte[] GetUtf8BytesWithTrailingZero(string value)
    {
        var length = Encoding.UTF8.GetByteCount(value);

        // Add one for the trailing zero.
        var byteArray = new byte[length + 1];
        var wrote = Encoding.UTF8.GetBytes(value, 0, value.Length, byteArray, 0);
        Contract.ThrowIfFalse(wrote == length);

        // Paranoia, but write in the trailing zero no matter what.
        byteArray[^1] = 0;
        return byteArray;
    }

    /// <summary>
    /// The raw handle to the underlying DB.
    /// </summary>
    private readonly SafeSqliteHandle _handle;

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

    public static SqlConnection Create(IPersistentStorageFaultInjector? faultInjector, string databasePath)
    {
        faultInjector?.OnNewConnection();

        // Allocate dictionary before doing any sqlite work.  That way if it throws
        // we don't have to do any additional cleanup.
        var queryToStatement = new Dictionary<string, SqlStatement>();

        // Use SQLITE_OPEN_NOMUTEX to enable multi-thread mode, where multiple connections can
        // be used provided each one is only used from a single thread at a time.
        //
        // Use SHAREDCACHE so that we can have an in-memory DB that we dump our writes into.  We
        // need SHAREDCACHE so that all connections see that same in-memory DB.  This also
        // requires OPEN_URI since we need a `file::memory:` uri for them all to refer to.
        //
        // see https://sqlite.org/threadsafe.html for more detail
        var flags = OpenFlags.SQLITE_OPEN_CREATE |
            OpenFlags.SQLITE_OPEN_READWRITE |
            OpenFlags.SQLITE_OPEN_NOMUTEX |
            OpenFlags.SQLITE_OPEN_SHAREDCACHE |
            OpenFlags.SQLITE_OPEN_URI;
        var handle = NativeMethods.sqlite3_open_v2(databasePath, (int)flags, vfs: null, out var result);

        if (result != Result.OK)
        {
            handle.Dispose();
            throw new SqlException(result, $"Could not open database file: {databasePath} ({result})");
        }

        try
        {
            NativeMethods.sqlite3_busy_timeout(handle, (int)TimeSpan.FromMinutes(1).TotalMilliseconds);
            var connection = new SqlConnection(handle, queryToStatement);

            // Attach (creating if necessary) a singleton in-memory write cache to this connection.
            //
            // From: https://www.sqlite.org/sharedcache.html Enabling shared-cache for an in-memory database allows
            // two or more database connections in the same process to have access to the same in-memory database.
            // An in-memory database in shared cache is automatically deleted and memory is reclaimed when the last
            // connection to that database closes.

            // Using `?mode=memory&cache=shared as writecache` at the end ensures all connections (to the on-disk
            // db) see the same db (https://sqlite.org/inmemorydb.html) and the same data when reading and writing.
            // i.e. if one connection writes data to this, another connection will see that data when reading.
            // Without this, each connection would get their own private memory db independent of all other
            // connections.

            // Workaround https://github.com/ericsink/SQLitePCL.raw/issues/407.  On non-windows do not pass in the
            // uri of the DB on disk we're associating this in-memory cache with.  This throws on at least OSX for
            // reasons that aren't fully understood yet.  If more details/fixes emerge in that linked issue, we can
            // ideally remove this and perform the attachment uniformly on all platforms.

            // From: https://www.sqlite.org/lang_expr.html
            //
            // A string constant is formed by enclosing the string in single quotes ('). A single quote within the
            // string can be encoded by putting two single quotes in a row - as in Pascal. C-style escapes using the
            // backslash character are not supported because they are not standard SQL.
            var attachString = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? $"attach database '{new Uri(databasePath.Replace("'", "''")).AbsoluteUri}?mode=memory&cache=shared' as {Database.WriteCache.GetName()};"
                : $"attach database 'file::memory:?cache=shared' as {Database.WriteCache.GetName()};";

            connection.ExecuteCommand(attachString);

            return connection;
        }
        catch
        {
            // If we failed to create connection, ensure that we still release the sqlite
            // handle.
            handle.Dispose();
            throw;
        }
    }

    private SqlConnection(SafeSqliteHandle handle, Dictionary<string, SqlStatement> queryToStatement)
    {
        _handle = handle;
        _queryToStatement = queryToStatement;
    }

    internal void Close_OnlyForUseBySQLiteConnectionPool()
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

    /// <inheritdoc cref="RunInTransaction{TState, TResult}(Func{TState, TResult}, TState, bool)"/>
    public SqlException? RunInTransaction<TState>(Action<TState> action, TState state, bool throwOnSqlException)
    {
        var (_, exception) = RunInTransaction(
            static state =>
            {
                state.action(state.state);
                return (object?)null;
            },
            (action, state),
            throwOnSqlException);

        return exception;
    }

    /// <param name="throwOnSqlException">If a <see cref="SqlException"/> that happens during excution of <paramref
    /// name="action"/> should bubble out of this method or not.  If <see langword="false"/>, then the exception
    /// will be returned in the result value instead</param>
    public (TResult? result, SqlException? exception) RunInTransaction<TState, TResult>(
        Func<TState, TResult> action, TState state, bool throwOnSqlException)
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
            return (result, null);
        }
        catch (SqlException ex)
        {
            Logger.Log(FunctionId.SQLite_SqlException, SQLitePersistentStorage.GetLogMessage(ex));

            // See documentation here: https://sqlite.org/lang_transaction.html
            //
            // If certain kinds of errors occur within a transaction, the transaction may or may not be rolled back
            // automatically.
            //
            // ...
            //
            // It is recommended that applications respond to the errors listed above by explicitly issuing a
            // ROLLBACK command. If the transaction has already been rolled back automatically by the error
            // response, then the ROLLBACK command will fail with an error, but no harm is caused by this.
            //
            // End of sqlite documentation.

            // Because of the above, we know we may be in an incomplete state, so we always do a rollback to get us
            // back to a clean state.  We ignore errors here as it's know that this can fail, but will cause no
            // harm.
            Rollback(throwOnError: false);

            if (throwOnSqlException)
                throw;

            return (default, ex);
        }
        catch (Exception)
        {
            // Some other exception occurred outside of sqlite entirely (like a null-ref exception in our own code).
            // Rollback (throwing if that rollback failed for some reason), then continue the exception higher up
            // to tear down the callers as well.

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
    public Optional<Stream> ReadDataBlob_MustRunInTransaction(Database database, Table table, long rowId)
    {
        return ReadBlob_MustRunInTransaction(
            database, table, Column.Data, rowId,
            static (self, blobHandle) => new Optional<Stream>(self.ReadBlob(blobHandle)));
    }

    [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/36114", AllowCaptures = false)]
    public Optional<Checksum> ReadChecksum_MustRunInTransaction(Database database, Table table, long rowId)
    {
        return ReadBlob_MustRunInTransaction(
            database, table, Column.Checksum, rowId,
            static (self, blobHandle) =>
            {
                // If the length of the blob isn't correct, then we can't read a checksum out of this.
                var length = NativeMethods.sqlite3_blob_bytes(blobHandle);
                if (length != Checksum.HashSize)
                    return new Optional<Checksum>();

                Span<byte> bytes = stackalloc byte[Checksum.HashSize];
                self.ThrowIfNotOk(NativeMethods.sqlite3_blob_read(blobHandle, bytes, offset: 0));

                Contract.ThrowIfFalse(MemoryMarshal.TryRead(bytes, out Checksum result));
                return new Optional<Checksum>(result);
            });
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
            ThrowIfNotOk(NativeMethods.sqlite3_blob_read(blob, bytes.AsSpan(), offset: 0));
            return new MemoryStream(bytes);
        }
    }

    private Stream ReadBlobIntoPooledStream(SafeSqliteBlobHandle blob, int length)
    {
        var bytes = SQLitePersistentStorage.GetPooledBytes();
        try
        {

            ThrowIfNotOk(NativeMethods.sqlite3_blob_read(blob, new Span<byte>(bytes, start: 0, length), offset: 0));

            // Copy those bytes into a pooled stream
            return SerializableBytes.CreateReadableStream(bytes, length);
        }
        finally
        {
            // Return our small array back to the pool.
            SQLitePersistentStorage.ReturnPooledBytes(bytes);
        }
    }

    [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/36114", AllowCaptures = false)]
    public Optional<T> ReadBlob_MustRunInTransaction<T>(
        Database database, Table table, Column column, long rowId,
        Func<SqlConnection, SafeSqliteBlobHandle, Optional<T>> readBlob)
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

        var databaseNameBytes = database switch
        {
            Database.Main => s_mainNameWithTrailingZero,
            Database.WriteCache => s_writeCacheNameWithTrailingZero,
            _ => throw ExceptionUtilities.UnexpectedValue(database),
        };

        var tableNameBytes = table switch
        {
            Table.Solution => s_solutionTableNameWithTrailingZero,
            Table.Project => s_projectTableNameWithTrailingZero,
            Table.Document => s_documentTableNameWithTrailingZero,
            _ => throw ExceptionUtilities.UnexpectedValue(table),
        };

        var columnNameBytes = column switch
        {
            Column.Data => s_dataColumnNameWithTrailingZero,
            Column.Checksum => s_checksumColumnNameWithTrailingZero,
            _ => throw ExceptionUtilities.UnexpectedValue(column),
        };

        unsafe
        {
            fixed (byte* databaseNamePtr = databaseNameBytes)
            fixed (byte* tableNamePtr = tableNameBytes)
            fixed (byte* columnNamePtr = columnNameBytes)
            {
                // sqlite requires a byte* and a length *not* including the trailing zero.  So subtract one from all
                // the array lengths to get the length they expect.

                const int ReadOnlyFlags = 0;
                using var blob = NativeMethods.sqlite3_blob_open(
                    _handle,
                    utf8z.FromPtrLen(databaseNamePtr, databaseNameBytes.Length - 1),
                    utf8z.FromPtrLen(tableNamePtr, tableNameBytes.Length - 1),
                    utf8z.FromPtrLen(columnNamePtr, columnNameBytes.Length - 1),
                    rowId,
                    ReadOnlyFlags,
                    out var result);

                if (result == Result.ERROR)
                {
                    // can happen when rowId points to a row that hasn't been written to yet.
                    return default;
                }

                ThrowIfNotOk(result);
                return readBlob(this, blob);
            }
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
        => throw new SqlException(result,
            NativeMethods.sqlite3_errmsg(handle) + Environment.NewLine +
            NativeMethods.sqlite3_errstr(NativeMethods.sqlite3_extended_errcode(handle)));
}
