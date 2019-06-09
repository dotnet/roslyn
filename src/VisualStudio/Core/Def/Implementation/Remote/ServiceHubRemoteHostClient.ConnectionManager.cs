// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Client;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed partial class ServiceHubRemoteHostClient : RemoteHostClient
    {
        private partial class ConnectionManager
        {
            private readonly HubClient _hubClient;
            private readonly HostGroup _hostGroup;
            private readonly TimeSpan _timeout;

            private readonly ReaderWriterLockSlim _shutdownLock;
            private readonly ReferenceCountedDisposable<RemotableDataJsonRpc> _remotableDataRpc;

            private readonly int _maxPoolConnections;

            // keyed to serviceName. each connection is for specific service such as CodeAnalysisService
            private readonly ConcurrentDictionary<string, ConcurrentQueue<JsonRpcConnection>> _pools;

            // indicate whether pool should be used.
            private readonly bool _enableConnectionPool;

            // indicate whether connection manager has shutdown
            private bool _shutdown;

            public ConnectionManager(
                HubClient hubClient,
                HostGroup hostGroup,
                bool enableConnectionPool,
                int maxPoolConnection,
                TimeSpan timeout,
                ReferenceCountedDisposable<RemotableDataJsonRpc> remotableDataRpc)
            {
                _shutdown = false;

                _hubClient = hubClient;
                _hostGroup = hostGroup;
                _timeout = timeout;

                _remotableDataRpc = remotableDataRpc;
                _maxPoolConnections = maxPoolConnection;

                // initial value 4 is chosen to stop concurrent dictionary creating too many locks.
                // and big enough for all our services such as codeanalysis, remotehost, snapshot and etc services
                _pools = new ConcurrentDictionary<string, ConcurrentQueue<JsonRpcConnection>>(concurrencyLevel: 4, capacity: 4);

                _enableConnectionPool = enableConnectionPool;
                _shutdownLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            }

            public HostGroup HostGroup => _hostGroup;

            public Task<Connection> TryCreateConnectionAsync(string serviceName, object callbackTarget, CancellationToken cancellationToken)
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
                    return TryCreateNewConnectionAsync(serviceName, callbackTarget, cancellationToken);
                }

                // when callbackTarget is given, we can't share/pool connection since callbackTarget attaches a state to connection.
                // so connection is only valid for that specific callbackTarget. it is up to the caller to keep connection open
                // if he wants to reuse same connection
                if (callbackTarget != null)
                {
                    return TryCreateNewConnectionAsync(serviceName, callbackTarget, cancellationToken);
                }

                return TryGetConnectionFromPoolAsync(serviceName, cancellationToken);
            }

            private async Task<Connection> TryGetConnectionFromPoolAsync(string serviceName, CancellationToken cancellationToken)
            {
                var queue = _pools.GetOrAdd(serviceName, _ => new ConcurrentQueue<JsonRpcConnection>());
                if (queue.TryDequeue(out var connection))
                {
                    return new PooledConnection(this, serviceName, connection);
                }

                var newConnection = (JsonRpcConnection)await TryCreateNewConnectionAsync(serviceName, callbackTarget: null, cancellationToken).ConfigureAwait(false);
                if (newConnection == null)
                {
                    // we might not get new connection if we are either shutdown explicitly or due to OOP terminated
                    return null;
                }

                return new PooledConnection(this, serviceName, newConnection);
            }

            private async Task<Connection> TryCreateNewConnectionAsync(string serviceName, object callbackTarget, CancellationToken cancellationToken)
            {
                var dataRpc = _remotableDataRpc.TryAddReference();
                if (dataRpc == null)
                {
                    // dataRpc is disposed. this can happen if someone killed remote host process while there is
                    // no other one holding the data connection.
                    // in those error case, don't crash but return null. this method is TryCreate since caller expects it to return null
                    // on such error situation.
                    return null;
                }

                // get stream from service hub to communicate service specific information
                // this is what consumer actually use to communicate information
                var serviceStream = await Connections.RequestServiceAsync(dataRpc.Target.Workspace, _hubClient, serviceName, _hostGroup, _timeout, cancellationToken).ConfigureAwait(false);

                return new JsonRpcConnection(_hubClient.Logger, callbackTarget, serviceStream, dataRpc);
            }

            private void Free(string serviceName, JsonRpcConnection connection)
            {
                using (_shutdownLock.DisposableRead())
                {
                    if (!_enableConnectionPool || _shutdown)
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

            public void Shutdown()
            {
                using (_shutdownLock.DisposableWrite())
                {
                    _shutdown = true;

                    // let ref count this one is holding go
                    _remotableDataRpc.Dispose();

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
