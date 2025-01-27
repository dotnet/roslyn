// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
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
    private const string LockFile = "db.lock";

    private readonly CancellationTokenSource _shutdownTokenSource = new();

    private readonly string _solutionDirectory;

    // We pool connections to the DB so that we don't have to take the hit of 
    // reconnecting.  The connections also cache the prepared statements used
    // to get/set data from the db.  A connection is safe to use by one thread
    // at a time, but is not safe for simultaneous use by multiple threads.
    private readonly object _connectionGate = new();
    private readonly Stack<SqlConnection> _connectionsPool = new();
    private readonly Action _flushInMemoryDataToDisk;

    /// <summary>
    /// Lock file that ensures only one database is made per process per solution.
    /// </summary>
    public readonly IDisposable DatabaseOwnership;

    /// <summary>
    /// For testing purposes.  Allows us to test what happens when we fail to acquire the db lock file.
    /// </summary>
    private readonly IPersistentStorageFaultInjector? _faultInjector;

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

    /// <summary>
    /// Use a <see cref="ConcurrentExclusiveSchedulerPair"/> to simulate a reader-writer lock.
    /// Read operations are performed on the <see cref="ConcurrentExclusiveSchedulerPair.ConcurrentScheduler"/>
    /// and writes are performed on the <see cref="ConcurrentExclusiveSchedulerPair.ExclusiveScheduler"/>.
    ///
    /// We use this as a condition of using the in-memory shared-cache sqlite DB.  This DB
    /// doesn't busy-wait when attempts are made to lock the tables in it, which can lead to
    /// deadlocks.  Specifically, consider two threads doing the following:
    ///
    /// Thread A starts a transaction that starts as a reader, and later attempts to perform a
    /// write. Thread B is a writer (either started that way, or started as a reader and
    /// promoted to a writer first). B holds a RESERVED lock, waiting for readers to clear so it
    /// can start writing. A holds a SHARED lock (it's a reader) and tries to acquire RESERVED
    /// lock (so it can start writing).  The only way to make progress in this situation is for
    /// one of the transactions to roll back. No amount of waiting will help, so when SQLite
    /// detects this situation, it doesn't honor the busy timeout.
    ///
    /// To prevent this scenario, we control our access to the db explicitly with operations that
    /// can concurrently read, and operations that exclusively write.
    ///
    /// All code that reads or writes from the db should go through this.
    /// </summary>
    private ConcurrentExclusiveSchedulerPair Scheduler { get; } = new();

    private SQLitePersistentStorage(
        SolutionKey solutionKey,
        string workingFolderPath,
        string databaseFile,
        IAsynchronousOperationListener asyncListener,
        IPersistentStorageFaultInjector? faultInjector,
        IDisposable databaseOwnership)
        : base(solutionKey, workingFolderPath, databaseFile)
    {
        Contract.ThrowIfNull(solutionKey.FilePath);
        _solutionDirectory = PathUtilities.GetDirectoryName(solutionKey.FilePath);

        _solutionAccessor = new SolutionAccessor(this);
        _projectAccessor = new ProjectAccessor(this);
        _documentAccessor = new DocumentAccessor(this);

        _faultInjector = faultInjector;
        DatabaseOwnership = databaseOwnership;

        // Create a delay to batch up requests to flush.  We'll won't flush more than every FlushAllDelayMS.
        _flushInMemoryDataToDisk = FlushInMemoryDataToDisk;
        _flushQueue = new AsyncBatchingWorkQueue(
            TimeSpan.FromMilliseconds(FlushAllDelayMS),
            FlushInMemoryDataToDiskIfNotShutdownAsync,
            asyncListener,
            _shutdownTokenSource.Token);
    }

    public static SQLitePersistentStorage? TryCreate(
        SolutionKey solutionKey,
        string workingFolderPath,
        string databaseFile,
        IAsynchronousOperationListener asyncListener,
        IPersistentStorageFaultInjector? faultInjector)
    {
        var databaseOwnership = TryGetDatabaseOwnership(databaseFile);
        if (databaseOwnership is null)
            return null;

        var storage = new SQLitePersistentStorage(
            solutionKey, workingFolderPath, databaseFile, asyncListener, faultInjector, databaseOwnership);
        storage.Initialize();
        return storage;
    }

    /// <summary>
    /// Returns null in the case where an IO exception prevented us from being able to acquire
    /// the db lock file.
    /// </summary>
    private static IDisposable? TryGetDatabaseOwnership(string databaseFilePath)
    {
        return IOUtilities.PerformIO<IDisposable?>(() =>
        {
            // make sure directory exist first.
            EnsureDirectory(databaseFilePath);

            var directoryName = Path.GetDirectoryName(databaseFilePath);
            Contract.ThrowIfNull(directoryName);

            return File.Open(
                Path.Combine(directoryName, LockFile),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }, defaultValue: null);

        static void EnsureDirectory(string databaseFilePath)
        {
            var directory = Path.GetDirectoryName(databaseFilePath);
            Contract.ThrowIfNull(directory);

            if (Directory.Exists(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
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

    private void Initialize()
    {
        // This is our startup path.  No other code can be running.  So it's safe for us to access a connection that can
        // talk to the db without having to be on the reader/writer scheduler queue.
        using var _ = GetPooledConnection(checkScheduler: false, out var connection);

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
