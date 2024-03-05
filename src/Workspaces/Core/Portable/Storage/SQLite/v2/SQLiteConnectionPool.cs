// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;

namespace Microsoft.CodeAnalysis.SQLite.v2;

internal sealed partial class SQLiteConnectionPool(SQLiteConnectionPoolService connectionPoolService, IPersistentStorageFaultInjector? faultInjector, string databasePath, IDisposable ownershipLock) : IDisposable
{
    // We pool connections to the DB so that we don't have to take the hit of 
    // reconnecting.  The connections also cache the prepared statements used
    // to get/set data from the db.  A connection is safe to use by one thread
    // at a time, but is not safe for simultaneous use by multiple threads.
    private readonly object _connectionGate = new();
    private readonly Stack<SqlConnection> _connectionsPool = new();

    private readonly CancellationTokenSource _shutdownTokenSource = new();

    internal void Initialize(
        Action<SqlConnection, CancellationToken> initializer,
        CancellationToken cancellationToken)
    {
        // This is our startup path.  No other code can be running.  So it's safe for us to access a connection that
        // can talk to the db without having to be on the reader/writer scheduler queue.
        using var _ = GetPooledConnection(checkScheduler: false, out var connection);

        initializer(connection, cancellationToken);
    }

    public void Dispose()
    {
        // Flush all pending writes so that all data our features wanted written
        // are definitely persisted to the DB.
        try
        {
            _shutdownTokenSource.Cancel();
            CloseWorker();
        }
        finally
        {
            // let the lock go
            ownershipLock.Dispose();
        }
    }

    private void CloseWorker()
    {
        lock (_connectionGate)
        {
            // Go through all our pooled connections and close them.
            while (_connectionsPool.Count > 0)
            {
                var connection = _connectionsPool.Pop();
                connection.Close_OnlyForUseBySQLiteConnectionPool();
            }
        }
    }

    /// <summary>
    /// Gets a <see cref="SqlConnection"/> from the connection pool, or creates one if none are available.
    /// </summary>
    /// <remarks>
    /// Database connections have a large amount of overhead, and should be returned to the pool when they are no
    /// longer in use. In particular, make sure to avoid letting a connection lease cross an <see langword="await"/>
    /// boundary, as it will prevent code in the asynchronous operation from using the existing connection.
    /// </remarks>
    internal PooledConnection GetPooledConnection(out SqlConnection connection)
        => GetPooledConnection(checkScheduler: true, out connection);

    /// <summary>
    /// <inheritdoc cref="GetPooledConnection(out SqlConnection)"/>
    /// Only use this overload if it is safe to bypass the normal scheduler check.  Only startup code (which runs
    /// before any reads/writes/flushes happen) should use this.
    /// </summary>
    private PooledConnection GetPooledConnection(bool checkScheduler, out SqlConnection connection)
    {
        if (checkScheduler)
        {
            var scheduler = TaskScheduler.Current;
            if (scheduler != connectionPoolService.Scheduler.ConcurrentScheduler && scheduler != connectionPoolService.Scheduler.ExclusiveScheduler)
                throw new InvalidOperationException($"Cannot get a connection to the DB unless running on one of {nameof(SQLiteConnectionPoolService)}'s schedulers");
        }

        var result = new PooledConnection(this, GetConnection());
        connection = result.Connection;
        return result;
    }

    private SqlConnection GetConnection()
    {
        lock (_connectionGate)
        {
            // If we have an available connection, just return that.
            if (_connectionsPool.Count > 0)
            {
                return _connectionsPool.Pop();
            }
        }

        // Otherwise create a new connection.
        return SqlConnection.Create(faultInjector, databasePath);
    }

    private void ReleaseConnection(SqlConnection connection)
    {
        lock (_connectionGate)
        {
            // If we've been asked to shutdown, then don't actually add the connection back to 
            // the pool.  Instead, just close it as we no longer need it.
            if (_shutdownTokenSource.IsCancellationRequested)
            {
                connection.Close_OnlyForUseBySQLiteConnectionPool();
                return;
            }

            try
            {
                _connectionsPool.Push(connection);
            }
            catch
            {
                // An exception (likely OutOfMemoryException) occurred while returning the connection to the pool.
                // The connection will be discarded, so make sure to close it so the finalizer doesn't crash the
                // process later.
                connection.Close_OnlyForUseBySQLiteConnectionPool();
                throw;
            }
        }
    }
}
