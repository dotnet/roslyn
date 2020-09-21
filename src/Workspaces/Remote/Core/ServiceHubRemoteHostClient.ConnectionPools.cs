// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class ServiceHubRemoteHostClient
    {
        public delegate Task<RemoteServiceConnection> ConnectionFactory(RemoteServiceName serviceName, IPooledConnectionReclamation poolReclamation, CancellationToken cancellationToken);

        internal sealed class ConnectionPools : IDisposable
        {
            private sealed class Pool : IPooledConnectionReclamation
            {
                private readonly ConcurrentQueue<JsonRpcConnection> _queue;
                private readonly ConnectionPools _owner;

                public Pool(ConnectionPools connectionPools)
                {
                    _queue = new ConcurrentQueue<JsonRpcConnection>();
                    _owner = connectionPools;
                }

                public void Return(JsonRpcConnection connection)
                    => _owner.Free(_queue, connection);

                public bool TryAcquire([NotNullWhen(true)] out JsonRpcConnection? connection)
                {
                    if (_queue.TryDequeue(out connection))
                    {
                        connection.SetPoolReclamation(this);
                        return true;
                    }

                    return false;
                }

                internal void DisposeConnections()
                {
                    // Use TryDequeue instead of TryAcquire to ensure disposal doesn't just return the collection to the
                    // pool.
                    while (_queue.TryDequeue(out var connection))
                    {
                        connection.Dispose();
                    }
                }
            }

            private readonly ConnectionFactory _connectionFactory;
            private readonly ReaderWriterLockSlim _shutdownLock;
            private readonly int _capacityPerService;

            // keyed to serviceName. each connection is for specific service such as CodeAnalysisService
            private readonly ConcurrentDictionary<RemoteServiceName, Pool> _pools;

            private bool _isDisposed;

            public ConnectionPools(ConnectionFactory connectionFactory, int capacity)
            {
                _connectionFactory = connectionFactory;
                _capacityPerService = capacity;

                // initial value 4 is chosen to stop concurrent dictionary creating too many locks.
                // and big enough for all our services such as codeanalysis, remotehost, snapshot and etc services
                _pools = new ConcurrentDictionary<RemoteServiceName, Pool>(concurrencyLevel: 4, capacity: 4);

                _shutdownLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            }

            public async Task<RemoteServiceConnection> GetOrCreateConnectionAsync(RemoteServiceName serviceName, CancellationToken cancellationToken)
            {
                var pool = _pools.GetOrAdd(serviceName, _ => new Pool(this));
                if (pool.TryAcquire(out var connection))
                {
                    return connection;
                }

                return await _connectionFactory(serviceName, pool, cancellationToken).ConfigureAwait(false);
            }

            internal void Free(ConcurrentQueue<JsonRpcConnection> pool, JsonRpcConnection connection)
            {
                using (_shutdownLock.DisposableRead())
                {
                    // There is a race between checking the current pool capacity i nthe condition and 
                    // and queueing connections to the pool in the else branch.
                    // The amount of pooled connections may thus exceed the capacity at times,
                    // or some connections might not end up returned into the pool and reused.
                    if (_isDisposed || pool.Count >= _capacityPerService)
                    {
                        connection.Dispose();
                    }
                    else
                    {
                        pool.Enqueue(connection);
                    }
                }
            }

            public void Dispose()
            {
                using (_shutdownLock.DisposableWrite())
                {
                    _isDisposed = true;

                    foreach (var (_, pool) in _pools)
                    {
                        pool.DisposeConnections();
                    }

                    _pools.Clear();
                }
            }
        }
    }
}
