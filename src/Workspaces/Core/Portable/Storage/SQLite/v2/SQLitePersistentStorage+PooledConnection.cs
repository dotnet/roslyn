// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;

namespace Microsoft.CodeAnalysis.SQLite.v2;

internal sealed partial class SQLitePersistentStorage
{
    private readonly struct PooledConnection(SQLitePersistentStorage storage, SqlConnection sqlConnection) : IDisposable
    {
        public readonly SqlConnection Connection = sqlConnection;

        public void Dispose()
            => storage.ReleaseConnection(Connection);
    }

    /// <summary>
    /// Gets a <see cref="SqlConnection"/> from the connection pool, or creates one if none are available.
    /// </summary>
    /// <remarks>
    /// Database connections have a large amount of overhead, and should be returned to the pool when they are no
    /// longer in use. In particular, make sure to avoid letting a connection lease cross an <see langword="await"/>
    /// boundary, as it will prevent code in the asynchronous operation from using the existing connection.
    /// </remarks>
    private PooledConnection GetPooledConnection(out SqlConnection connection)
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
            if (scheduler != this.Scheduler.ConcurrentScheduler && scheduler != this.Scheduler.ExclusiveScheduler)
                throw new InvalidOperationException($"Cannot get a connection to the DB unless running on one of {nameof(SQLitePersistentStorage)}'s schedulers");
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
            if (_connectionsPool.TryPop(out var connection))
                return connection;
        }

        // Otherwise create a new connection.
        return SqlConnection.Create(_faultInjector, this.DatabaseFile);
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
