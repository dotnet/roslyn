// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Client;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed partial class ServiceHubRemoteHostClient : RemoteHostClient
    {
        private class ConnectionPools
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

            public ConnectionPools(
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

            private class PooledConnection : Connection
            {
                private readonly ConnectionPools _pools;
                private readonly string _serviceName;
                private readonly JsonRpcConnection _connection;

                public PooledConnection(ConnectionPools pools, string serviceName, JsonRpcConnection connection)
                {
                    _pools = pools;
                    _serviceName = serviceName;
                    _connection = connection;
                }

                public override Task SetConnectionStateAsync(PinnedRemotableDataScope scope) =>
                    _connection.SetConnectionStateAsync(scope);

                public override Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken) =>
                    _connection.InvokeAsync(targetName, arguments, cancellationToken);

                public override Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken) =>
                    _connection.InvokeAsync<T>(targetName, arguments, cancellationToken);

                public override Task InvokeAsync(
                    string targetName, IReadOnlyList<object> arguments,
                    Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken) =>
                    _connection.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync, cancellationToken);

                public override Task<T> InvokeAsync<T>(
                    string targetName, IReadOnlyList<object> arguments,
                    Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken) =>
                    _connection.InvokeAsync<T>(targetName, arguments, funcWithDirectStreamAsync, cancellationToken);

                protected override void OnDisposed() =>
                    _pools.Free(_serviceName, _connection);

            }
        }

        private static class Connections
        {
            /// <summary>
            /// call <paramref name="funcAsync"/> and retry up to <paramref name="timeout"/> if the call throws
            /// <typeparamref name="TException"/>. any other exception from the call won't be handled here.
            /// </summary>
            public static async Task<TResult> RetryRemoteCallAsync<TException, TResult>(
                Func<Task<TResult>> funcAsync,
                TimeSpan timeout,
                CancellationToken cancellationToken) where TException : Exception
            {
                const int retry_delayInMS = 50;

                using (var pooledStopwatch = SharedPools.Default<Stopwatch>().GetPooledObject())
                {
                    var watch = pooledStopwatch.Object;
                    watch.Start();

                    while (watch.Elapsed < timeout)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            return await funcAsync().ConfigureAwait(false);
                        }
                        catch (TException)
                        {
                            // throw cancellation token if operation is cancelled
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        // wait for retry_delayInMS before next try
                        await Task.Delay(retry_delayInMS, cancellationToken).ConfigureAwait(false);

                        ReportTimeout(watch);
                    }
                }

                // operation timed out, more than we are willing to wait
                ShowInfoBar();

                // user didn't ask for cancellation, but we can't fullfill this request. so we
                // create our own cancellation token and then throw it. this doesn't guarantee
                // 100% that we won't crash, but this is at least safest way we know until user
                // restart VS (with info bar)
                using (var ownCancellationSource = new CancellationTokenSource())
                {
                    ownCancellationSource.Cancel();
                    ownCancellationSource.Token.ThrowIfCancellationRequested();
                }

                throw ExceptionUtilities.Unreachable;
            }

            public static async Task<Stream> RequestServiceAsync(
                HubClient client,
                string serviceName,
                HostGroup hostGroup,
                TimeSpan timeout,
                CancellationToken cancellationToken)
            {
                const int max_retry = 10;
                const int retry_delayInMS = 50;

                RemoteInvocationException lastException = null;

                var descriptor = new ServiceDescriptor(serviceName) { HostGroup = hostGroup };

                // call to get service can fail due to this bug - devdiv#288961 or more.
                // until root cause is fixed, we decide to have retry rather than fail right away
                for (var i = 0; i < max_retry; i++)
                {
                    try
                    {
                        // we are wrapping HubClient.RequestServiceAsync since we can't control its internal timeout value ourselves.
                        // we have bug opened to track the issue.
                        // https://devdiv.visualstudio.com/DefaultCollection/DevDiv/Editor/_workitems?id=378757&fullScreen=false&_a=edit

                        // retry on cancellation token since HubClient will throw its own cancellation token
                        // when it couldn't connect to service hub service for some reasons
                        // (ex, OOP process GC blocked and not responding to request)
                        return await RetryRemoteCallAsync<OperationCanceledException, Stream>(
                            () => client.RequestServiceAsync(descriptor, cancellationToken),
                            timeout,
                            cancellationToken).ConfigureAwait(false);
                    }
                    catch (RemoteInvocationException ex)
                    {
                        // save info only if it failed with different issue than before.
                        if (lastException?.Message != ex.Message)
                        {
                            // RequestServiceAsync should never fail unless service itself is actually broken.
                            // So far, we catched multiple issues from this NFW. so we will keep this NFW.
                            ex.ReportServiceHubNFW("RequestServiceAsync Failed");

                            lastException = ex;
                        }
                    }

                    // wait for retry_delayInMS before next try
                    await Task.Delay(retry_delayInMS, cancellationToken).ConfigureAwait(false);
                }

                // crash right away to get better dump. otherwise, we will get dump from async exception
                // which most likely lost all valuable data
                FatalError.ReportUnlessCanceled(lastException);
                GC.KeepAlive(lastException);

                // unreachable
                throw ExceptionUtilities.Unreachable;
            }

            #region code related to make diagnosis easier later

            private static readonly TimeSpan s_reportTimeout = TimeSpan.FromMinutes(10);
            private static bool s_timeoutReported = false;

            private static void ReportTimeout(Stopwatch watch)
            {
                // if we tried for 10 min and still couldn't connect. NFW (non fatal watson) some data
                if (!s_timeoutReported && watch.Elapsed > s_reportTimeout)
                {
                    s_timeoutReported = true;

                    // report service hub logs along with dump
                    (new Exception("RequestServiceAsync Timeout")).ReportServiceHubNFW("RequestServiceAsync Timeout");
                }
            }

            private static bool s_infoBarReported = false;

            private static void ShowInfoBar()
            {
                // use info bar to show warning to users
                if (CodeAnalysis.PrimaryWorkspace.Workspace != null && !s_infoBarReported)
                {
                    // do not report it multiple times
                    s_infoBarReported = true;

                    // use info bar to show warning to users
                    CodeAnalysis.PrimaryWorkspace.Workspace.Services.GetService<IErrorReportingService>()?.ShowGlobalErrorInfo(
                        ServicesVSResources.Unfortunately_a_process_used_by_Visual_Studio_has_encountered_an_unrecoverable_error_We_recommend_saving_your_work_and_then_closing_and_restarting_Visual_Studio);
                }
            }
            #endregion
        }
    }
}
