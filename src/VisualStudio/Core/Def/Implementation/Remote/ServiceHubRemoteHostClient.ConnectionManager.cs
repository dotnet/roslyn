// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed partial class ServiceHubRemoteHostClient
    {
        private delegate Task<Connection> ConnectionFactory(string serviceName, CancellationToken cancellationToken);

        private sealed partial class ConnectionPool : IDisposable
        {
            private readonly ConnectionFactory _connectionFactory;
            private readonly ReaderWriterLockSlim _shutdownLock;
            private readonly int _maxPoolConnections;

            // keyed to serviceName. each connection is for specific service such as CodeAnalysisService
            private readonly ConcurrentDictionary<string, ConcurrentQueue<Connection>> _pools;

            private bool _isDisposed;

            public ConnectionPool(ConnectionFactory connectionFactory, int maxPoolConnection)
            {
                _connectionFactory = connectionFactory;
                _maxPoolConnections = maxPoolConnection;

                // initial value 4 is chosen to stop concurrent dictionary creating too many locks.
                // and big enough for all our services such as codeanalysis, remotehost, snapshot and etc services
                _pools = new ConcurrentDictionary<string, ConcurrentQueue<Connection>>(concurrencyLevel: 4, capacity: 4);

                _shutdownLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            }

            public async Task<Connection> GetOrCreateConnectionAsync(string serviceName, CancellationToken cancellationToken)
            {
                var queue = _pools.GetOrAdd(serviceName, _ => new ConcurrentQueue<Connection>());
                if (queue.TryDequeue(out var connection))
                {
                    return new PooledConnection(this, serviceName, connection);
                }

                var newConnection = await _connectionFactory(serviceName, cancellationToken).ConfigureAwait(false);
                return new PooledConnection(this, serviceName, newConnection);
            }

            private void Free(string serviceName, Connection connection)
            {
                using (_shutdownLock.DisposableRead())
                {
                    if (_isDisposed)
                    {
                        // pool is not being used or 
                        // manager is already shutdown
                        connection.Dispose();
                        return;
                    }

                    // queue must exist
                    var queue = _pools[serviceName];
                    if (queue.Count >= _maxPoolConnections)
                    {
                        // let the connection actually go away
                        connection.Dispose();
                        return;
                    }

                    // pool the connection
                    queue.Enqueue(connection);
                }
            }

            public void Dispose()
            {
                using (_shutdownLock.DisposableWrite())
                {
                    _isDisposed = true;

                    // let all connections in the pool to go away
                    foreach (var (_, queue) in _pools)
                    {
                        while (queue.TryDequeue(out var connection))
                        {
                            connection.Dispose();
                        }
                    }

                    _pools.Clear();
                }
            }
        }
    }
}
