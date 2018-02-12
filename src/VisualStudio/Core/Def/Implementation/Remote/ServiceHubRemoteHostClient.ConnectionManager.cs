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
            // it is mutable since it will set to false when this pool got shutdown
            private volatile bool _usePool;

            public ConnectionManager(
                HubClient hubClient,
                HostGroup hostGroup,
                bool usePool,
                int maxPoolConnection,
                TimeSpan timeout,
                ReferenceCountedDisposable<RemotableDataJsonRpc> remotableDataRpc)
            {
                _hubClient = hubClient;
                _hostGroup = hostGroup;
                _timeout = timeout;
                _remotableDataRpc = remotableDataRpc;

                _maxPoolConnections = maxPoolConnection;

                // we have 4 services. so start from 4. later if we add more services, it will still work.
                _pools = new ConcurrentDictionary<string, ConcurrentQueue<JsonRpcConnection>>(concurrencyLevel: 4, capacity: 4);

                _usePool = usePool;
                _shutdownLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            }

            public Task<Connection> TryCreateConnectionAsync(string serviceName, object callbackTarget, CancellationToken cancellationToken)
            {
                // pool is turned off either by option or pool has shutdown.
                if (!_usePool)
                {
                    // RemoteHostClient is allowed to be restarted by IRemoteHostClientService.RequestNewRemoteHostAsync
                    // when that happen, existing RemoteHostClient doesn't suddenly go away and start to throw exception. it handles
                    // shutdown gracefully. it will keep serving existing requests or even new requests if someone is holding old remoteHostClient.
                    // all connections made to old RemoteHost will eventually go away and that's when RemoteHost is actually removed.
                    // simplyput, RequestNewRemoteHostAsync is not intrusive to running features. all existing one will keep do what is doing
                    // with old RemoteHost and only new request will go to new remoteHost.
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

                return new PooledConnection(this, serviceName, (JsonRpcConnection)await TryCreateNewConnectionAsync(serviceName, callbackTarget: null, cancellationToken).ConfigureAwait(false));
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
                var serviceStream = await Connections.RequestServiceAsync(_hubClient, serviceName, _hostGroup, _timeout, cancellationToken).ConfigureAwait(false);

                return new JsonRpcConnection(_hubClient.Logger, callbackTarget, serviceStream, dataRpc);
            }

            private void Free(string serviceName, JsonRpcConnection connection)
            {
                using (_shutdownLock.DisposableRead())
                {
                    if (!_usePool)
                    {
                        // pool is not being used.
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
                    // mark not to use pool
                    _usePool = false;

                    // let ref count this one is holding go
                    _remotableDataRpc.Dispose();

                    // let all connections in the pool to go away
                    foreach (var kv in _pools)
                    {
                        while (kv.Value.TryDequeue(out var connection))
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
