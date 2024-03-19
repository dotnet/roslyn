// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite.v2;

[Export]
[Shared]
internal sealed class SQLiteConnectionPoolService : IDisposable
{
    private const string LockFile = "db.lock";

    private readonly object _gate = new();

    /// <summary>
    /// Maps from database file path to connection pool.
    /// </summary>
    /// <remarks>
    /// Access to this field is synchronized through <see cref="_gate"/>.
    /// </remarks>
    private readonly Dictionary<string, ReferenceCountedDisposable<SQLiteConnectionPool>> _connectionPools = [];

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SQLiteConnectionPoolService()
    {
    }

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
    public ConcurrentExclusiveSchedulerPair Scheduler { get; } = new();

    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var (_, pool) in _connectionPools)
                pool.Dispose();

            _connectionPools.Clear();
        }
    }

    public ReferenceCountedDisposable<SQLiteConnectionPool>? TryOpenDatabase(
        string databaseFilePath,
        IPersistentStorageFaultInjector? faultInjector,
        Action<SqlConnection, CancellationToken> initializer,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_connectionPools.TryGetValue(databaseFilePath, out var pool))
            {
                return pool.TryAddReference() ?? throw ExceptionUtilities.Unreachable();
            }

            // try to get db ownership lock. if someone else already has the lock. it will throw
            var ownershipLock = TryGetDatabaseOwnership(databaseFilePath);
            if (ownershipLock == null)
            {
                return null;
            }

            try
            {
                pool = new ReferenceCountedDisposable<SQLiteConnectionPool>(
                    new SQLiteConnectionPool(this, faultInjector, databaseFilePath, ownershipLock));

                pool.Target.Initialize(initializer, cancellationToken);

                // Place the initial ownership reference in _connectionPools, and return another
                _connectionPools.Add(databaseFilePath, pool);
                return pool.TryAddReference() ?? throw ExceptionUtilities.Unreachable();
            }
            catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex, cancellationToken))
            {
                if (pool is not null)
                {
                    // Dispose of the connection pool, releasing the ownership lock.
                    pool.Dispose();
                }
                else
                {
                    // The storage was not created so nothing owns the lock.
                    // Dispose the lock to allow reuse.
                    ownershipLock.Dispose();
                }

                throw;
            }
        }
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
    }

    private static void EnsureDirectory(string databaseFilePath)
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
