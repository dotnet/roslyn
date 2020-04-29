// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Client;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed partial class ServiceHubRemoteHostClient
    {
        private partial class ConnectionManager : IDisposable
        {
            private readonly Workspace _workspace;
            private readonly HubClient _hubClient;
            private readonly HostGroup _hostGroup;

            private readonly ReaderWriterLockSlim _shutdownLock;

            private readonly int _maxPoolConnections;

            // keyed to serviceName. each connection is for specific service such as CodeAnalysisService
            private readonly ConcurrentDictionary<string, ConcurrentQueue<JsonRpcConnection>> _pools;

            // indicate whether pool should be used.
            private readonly bool _enableConnectionPool;

            private bool _isDisposed;

            public ConnectionManager(
                Workspace workspace,
                HubClient hubClient,
                HostGroup hostGroup,
                bool enableConnectionPool,
                int maxPoolConnection)
            {
                _workspace = workspace;
                _hubClient = hubClient;
                _hostGroup = hostGroup;

                _maxPoolConnections = maxPoolConnection;

                // initial value 4 is chosen to stop concurrent dictionary creating too many locks.
                // and big enough for all our services such as codeanalysis, remotehost, snapshot and etc services
                _pools = new ConcurrentDictionary<string, ConcurrentQueue<JsonRpcConnection>>(concurrencyLevel: 4, capacity: 4);

                _enableConnectionPool = enableConnectionPool;
                _shutdownLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            }

            public HostGroup HostGroup => _hostGroup;

            public Task<Connection> CreateConnectionAsync(string serviceName, object? callbackTarget, CancellationToken cancellationToken)
            {
                // pool is not enabled by option
                if (!_enableConnectionPool)
                {
                    // RemoteHost is allowed to be restarted by IRemoteHostClientService.RequestNewRemoteHostAsync
                    // when that happens, existing Connection will keep working until they get disposed.
                    //
                    // now question is when someone calls RemoteHostClient.TryGetConnection for the client that got
                    // shutdown, whether it should gracefully handle that request or fail after shutdown.
                    // for current expected usage case where new remoteHost is only created when new solution is added,
                    // we should be fine on failing after shutdown.
                    //
                    // but, at some point, if we want to support RemoteHost being restarted at any random point, 
                    // we need to revisit this to support such case by creating new temporary connections.
                    // for now, I dropped it since it felt over-designing when there is no usage case for that yet.
                    return CreateNewConnectionAsync(serviceName, callbackTarget, cancellationToken);
                }

                // when callbackTarget is given, we can't share/pool connection since callbackTarget attaches a state to connection.
                // so connection is only valid for that specific callbackTarget. it is up to the caller to keep connection open
                // if he wants to reuse same connection
                if (callbackTarget != null)
                {
                    return CreateNewConnectionAsync(serviceName, callbackTarget, cancellationToken);
                }

                return GetConnectionFromPoolAsync(serviceName, cancellationToken);
            }

            private async Task<Connection> GetConnectionFromPoolAsync(string serviceName, CancellationToken cancellationToken)
            {
                var queue = _pools.GetOrAdd(serviceName, _ => new ConcurrentQueue<JsonRpcConnection>());
                if (queue.TryDequeue(out var connection))
                {
                    return new PooledConnection(this, serviceName, connection);
                }

                var newConnection = await CreateNewConnectionAsync(serviceName, callbackTarget: null, cancellationToken).ConfigureAwait(false);
                return new PooledConnection(this, serviceName, (JsonRpcConnection)newConnection);
            }

            private async Task<Connection> CreateNewConnectionAsync(string serviceName, object? callbackTarget, CancellationToken cancellationToken)
            {
                // get stream from service hub to communicate service specific information
                // this is what consumer actually use to communicate information
                var serviceStream = await RequestServiceAsync(_workspace, _hubClient, serviceName, _hostGroup, cancellationToken).ConfigureAwait(false);

                return new JsonRpcConnection(_workspace, _hubClient.Logger, callbackTarget, serviceStream);
            }

            private void Free(string serviceName, JsonRpcConnection connection)
            {
                using (_shutdownLock.DisposableRead())
                {
                    if (!_enableConnectionPool || _isDisposed)
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

                _hubClient.Dispose();
            }
        }
    }
}
