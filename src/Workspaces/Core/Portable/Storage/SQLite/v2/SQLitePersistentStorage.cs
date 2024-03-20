// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SQLite.Interop;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite.v2;

using static SQLitePersistentStorageConstants;

/// <summary>
/// Implementation of an <see cref="IPersistentStorage"/> backed by SQLite.
/// </summary>
internal sealed partial class SQLitePersistentStorage : AbstractPersistentStorage
{
    private readonly CancellationTokenSource _shutdownTokenSource = new();

    private readonly SolutionKey _solutionKey;
    private readonly string _solutionDirectory;

    private readonly SQLiteConnectionPoolService _connectionPoolService;
    private readonly ReferenceCountedDisposable<SQLiteConnectionPool> _connectionPool;
    private readonly Action _flushInMemoryDataToDisk;

    // Accessors that allow us to retrieve/store data into specific DB tables.  The
    // core Accessor type has logic that we to share across all reading/writing, while
    // the derived types contain only enough logic to specify how to read/write from
    // their respective tables.

    private readonly SolutionAccessor _solutionAccessor;
    private readonly ProjectAccessor _projectAccessor;
    private readonly DocumentAccessor _documentAccessor;

    // cached query strings

    private readonly string _insert_into_string_table_values_0 = $"insert into {StringInfoTableName}({DataColumnName}) values (?)";
    private readonly string _select_star_from_string_table_where_0_limit_one = $"select * from {StringInfoTableName} where ({DataColumnName} = ?) limit 1";
    private readonly string _select_star_from_string_table = $"select * from {StringInfoTableName}";

    private SQLitePersistentStorage(
        SQLiteConnectionPoolService connectionPoolService,
        SolutionKey solutionKey,
        string workingFolderPath,
        string databaseFile,
        IAsynchronousOperationListener asyncListener,
        IPersistentStorageFaultInjector? faultInjector)
        : base(workingFolderPath, solutionKey.FilePath!, databaseFile)
    {
        Contract.ThrowIfNull(solutionKey.FilePath);
        _solutionKey = solutionKey;
        _solutionDirectory = PathUtilities.GetDirectoryName(solutionKey.FilePath);
        _connectionPoolService = connectionPoolService;

        _solutionAccessor = new SolutionAccessor(this);
        _projectAccessor = new ProjectAccessor(this);
        _documentAccessor = new DocumentAccessor(this);

        // This assignment violates the declared non-nullability of _connectionPool, but the caller ensures that
        // the constructed object is only used if the nullability post-conditions are met.
        _connectionPool = connectionPoolService.TryOpenDatabase(
            databaseFile,
            faultInjector,
            Initialize,
            CancellationToken.None)!;

        // Create a delay to batch up requests to flush.  We'll won't flush more than every FlushAllDelayMS.
        _flushInMemoryDataToDisk = FlushInMemoryDataToDisk;
        _flushQueue = new AsyncBatchingWorkQueue(
            TimeSpan.FromMilliseconds(FlushAllDelayMS),
            FlushInMemoryDataToDiskIfNotShutdownAsync,
            asyncListener,
            _shutdownTokenSource.Token);
    }

    public static SQLitePersistentStorage? TryCreate(
        SQLiteConnectionPoolService connectionPoolService,
        SolutionKey solutionKey,
        string workingFolderPath,
        string databaseFile,
        IAsynchronousOperationListener asyncListener,
        IPersistentStorageFaultInjector? faultInjector)
    {
        var sqlStorage = new SQLitePersistentStorage(
            connectionPoolService, solutionKey, workingFolderPath, databaseFile, asyncListener, faultInjector);
        if (sqlStorage._connectionPool is null)
        {
            // The connection pool failed to initialize
            return null;
        }

        return sqlStorage;
    }

    public override void Dispose()
    {
        var task = DisposeAsync().AsTask();
        task.Wait();
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            // Flush all pending writes so that all data our features wanted written are definitely
            // persisted to the DB.
            try
            {
                await FlushWritesOnCloseAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // Flushing may fail.  We still have to close all our connections.
                StorageDatabaseLogger.LogException(e);
            }
        }
        finally
        {
            _connectionPool.Dispose();
        }
    }

    private void DisableStorage(SqlException exception)
    {
        Logger.Log(FunctionId.SQLite_StorageDisabled, GetLogMessage(exception));
        base.DisableStorage();
    }

    public static KeyValueLogMessage GetLogMessage(SqlException exception)
        => KeyValueLogMessage.Create(d =>
        {
            d["Result"] = exception.Result.ToString();
            d["Message"] = exception.Message;
        });

    private void Initialize(SqlConnection connection, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            // Someone tried to get a connection *after* a call to Dispose the storage system
            // happened.  That should never happen.  We only Dispose when the last ref to the
            // storage system goes away.  Once that happens, it's an error for there to be any
            // future or existing consumers of the storage service.  So nothing should be doing
            // anything that wants to get an connection.
            throw new InvalidOperationException();
        }

        // Ensure the database has tables for the types we care about.

        // Enable write-ahead logging to increase write performance by reducing amount of disk writes,
        // by combining writes at checkpoint, salong with using sequential-only writes to populate the log.
        // Also, WAL allows for relaxed ("normal") "synchronous" mode, see below.
        connection.ExecuteCommand("pragma journal_mode=wal", throwOnError: false);

        // Set "synchronous" mode to "normal" instead of default "full" to reduce the amount of buffer flushing syscalls,
        // significantly reducing both the blocked time and the amount of context switches.
        // When coupled with WAL, this (according to https://sqlite.org/pragma.html#pragma_synchronous and
        // https://www.sqlite.org/wal.html#performance_considerations) is unlikely to significantly affect durability,
        // while significantly increasing performance, because buffer flushing is done for each checkpoint, instead of each
        // transaction. While some writes can be lost, they are never reordered, and higher layers will recover from that.
        connection.ExecuteCommand("pragma synchronous=normal", throwOnError: false);

        // First, create all string tables in the main on-disk db.  These tables
        // don't need to be in the write-cache as all string looks go to/from the
        // main db.  This isn't a perf problem as we write the strings in bulk,
        // so there's no need for a write caching layer.  This also keeps consistency
        // totally clear as there's only one source of truth.
        connection.ExecuteCommand(
$@"create table if not exists {StringInfoTableName}(
""{StringDataIdColumnName}"" integer primary key autoincrement not null,
""{DataColumnName}"" varchar)");

        // Ensure that the string-info table's 'Value' column is defined to be 'unique'.
        // We don't allow duplicate strings in this table.
        connection.ExecuteCommand(
$@"create unique index if not exists ""{StringInfoTableName}_{DataColumnName}"" on {StringInfoTableName}(""{DataColumnName}"")");

        // Now make sure we have the individual tables for the solution/project/document info.
        // We put this in both our persistent table and our in-memory table so that they have
        // the same shape.
        EnsureTables(connection, Database.Main);
        EnsureTables(connection, Database.WriteCache);

        // Bulk load all the existing string/id pairs in the DB at once.  In a solution like roslyn, there are
        // roughly 20k of these strings.  Doing it as 20k individual reads adds more than a second of work time
        // reading in all the data.  This allows for a single query that can efficiently have the DB just stream the
        // pages from disk and bulk read those in the cursor the query uses.
        LoadExistingStringIds(connection);

        return;

        void EnsureTables(SqlConnection connection, Database database)
        {
            _solutionAccessor.CreateTable(connection, database);
            _projectAccessor.CreateTable(connection, database);
            _documentAccessor.CreateTable(connection, database);
        }
    }
}
