// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// This will tie <see cref="Solution"/> and <see cref="RemoteHostClient.Connection"/>'s lifetime together
    /// so that one can handle those more easily
    /// </summary>
    internal sealed class SessionWithSolution : IDisposable
    {
        private readonly RemoteHostClient.Connection _connection;
        private readonly PinnedRemotableDataScope _scope;

        public static async Task<SessionWithSolution> CreateAsync(RemoteHostClient.Connection connection, Solution solution, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(connection);
            Contract.ThrowIfNull(solution);

            PinnedRemotableDataScope scope = null;
            try
            {
                scope = await solution.GetPinnedScopeAsync(cancellationToken).ConfigureAwait(false);

                // set connection state for this session.
                // we might remove this in future. see https://github.com/dotnet/roslyn/issues/24836
                await connection.InvokeAsync(
                    WellKnownServiceHubServices.ServiceHubServiceBase_Initialize,
                    new object[] { scope.SolutionInfo },
                    cancellationToken).ConfigureAwait(false);

                return new SessionWithSolution(connection, scope);
            }
            catch
            {
                // Make sure disposable objects are disposed when exceptions are thrown. The try/finally is used to
                // ensure 'scope' is disposed even if an exception is thrown while disposing 'connection'.
                try
                {
                    connection.Dispose();
                }
                finally
                {
                    scope?.Dispose();
                }

                // we only expect this to happen on cancellation. otherwise, rethrow
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        private SessionWithSolution(RemoteHostClient.Connection connection, PinnedRemotableDataScope scope)
        {
            _connection = connection;
            _scope = scope;
        }

        public void AddAdditionalAssets(CustomAsset asset)
        {
            _scope.AddAdditionalAsset(asset);
        }

        public void Dispose()
        {
            _scope.Dispose();
            _connection.Dispose();
        }

        public Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => _connection.InvokeAsync(targetName, arguments, cancellationToken);
        public Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => _connection.InvokeAsync<T>(targetName, arguments, cancellationToken);
        public Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
            => _connection.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync, cancellationToken);
        public Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
            => _connection.InvokeAsync<T>(targetName, arguments, funcWithDirectStreamAsync, cancellationToken);
    }

    /// <summary>
    /// This will let one to hold onto <see cref="RemoteHostClient.Connection"/> for a while.
    /// this helper will let you not care about remote host being gone while you hold onto the connection if that ever happen
    /// 
    /// when this is used, solution must be explicitly passed around between client (VS) and remote host (OOP)
    /// </summary>
    internal sealed class KeepAliveSession
    {
        private readonly object _gate;
        private readonly IRemoteHostClientService _remoteHostClientService;

        private readonly string _serviceName;
        private readonly object _callbackTarget;

        private RemoteHostClient _client;
        private ReferenceCountedDisposable<RemoteHostClient.Connection> _connectionDoNotAccessDirectly;

        public KeepAliveSession(RemoteHostClient client, RemoteHostClient.Connection connection, string serviceName, object callbackTarget)
        {
            _gate = new object();

            Initialize(client, connection);

            _remoteHostClientService = client.Workspace.Services.GetService<IRemoteHostClientService>();
            _serviceName = serviceName;
            _callbackTarget = callbackTarget;
        }

        public void Shutdown()
        {
            ReferenceCountedDisposable<RemoteHostClient.Connection> connection;

            lock (_gate)
            {
                if (_client != null)
                {
                    _client.StatusChanged -= OnStatusChanged;
                }

                connection = _connectionDoNotAccessDirectly;

                _client = null;
                _connectionDoNotAccessDirectly = null;
            }

            connection?.Dispose();
        }

        public async Task<bool> TryInvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            using var connection = await TryGetConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return false;
            }

            await connection.Target.InvokeAsync(targetName, arguments, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public async Task<T> TryInvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            using var connection = await TryGetConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return default;
            }

            return await connection.Target.InvokeAsync<T>(targetName, arguments, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TryInvokeAsync(string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            using var connection = await TryGetConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return false;
            }

            await connection.Target.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public async Task<T> TryInvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            using var connection = await TryGetConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return default;
            }

            return await connection.Target.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TryInvokeAsync(string targetName, Solution solution, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            using var pooledObject = SharedPools.Default<List<object>>().GetPooledObject();
            using var scope = await solution.GetPinnedScopeAsync(cancellationToken).ConfigureAwait(false);
            using var connection = await TryGetConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return false;
            }

            pooledObject.Object.Add(scope.SolutionInfo);
            pooledObject.Object.AddRange(arguments);

            await connection.Target.InvokeAsync(targetName, pooledObject.Object, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public async Task<T> TryInvokeAsync<T>(string targetName, Solution solution, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            using var pooledObject = SharedPools.Default<List<object>>().GetPooledObject();
            using var scope = await solution.GetPinnedScopeAsync(cancellationToken).ConfigureAwait(false);
            using var connection = await TryGetConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return default;
            }

            pooledObject.Object.Add(scope.SolutionInfo);
            pooledObject.Object.AddRange(arguments);

            return await connection.Target.InvokeAsync<T>(targetName, pooledObject.Object, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TryInvokeAsync(
            string targetName, Solution solution, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            using var pooledObject = SharedPools.Default<List<object>>().GetPooledObject();
            using var scope = await solution.GetPinnedScopeAsync(cancellationToken).ConfigureAwait(false);
            using var connection = await TryGetConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return false;
            }

            pooledObject.Object.Add(scope.SolutionInfo);
            pooledObject.Object.AddRange(arguments);

            await connection.Target.InvokeAsync(targetName, pooledObject.Object, funcWithDirectStreamAsync, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public async Task<T> TryInvokeAsync<T>(
            string targetName, Solution solution, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            using var pooledObject = SharedPools.Default<List<object>>().GetPooledObject();
            using var scope = await solution.GetPinnedScopeAsync(cancellationToken).ConfigureAwait(false);
            using var connection = await TryGetConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return default;
            }

            pooledObject.Object.Add(scope.SolutionInfo);
            pooledObject.Object.AddRange(arguments);

            return await connection.Target.InvokeAsync(targetName, pooledObject.Object, funcWithDirectStreamAsync, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ReferenceCountedDisposable<RemoteHostClient.Connection>> TryGetConnectionAsync(CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (_connectionDoNotAccessDirectly != null)
                {
                    return _connectionDoNotAccessDirectly.TryAddReference();
                }
            }

            var client = await _remoteHostClientService.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return null;
            }

            var connection = await client.TryCreateConnectionAsync(_serviceName, _callbackTarget, cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return null;
            }

            Initialize(client, connection);

            return await TryGetConnectionAsync(cancellationToken).ConfigureAwait(false);
        }

        private void OnStatusChanged(object sender, bool connection)
        {
            if (connection)
            {
                return;
            }

            Shutdown();
        }

        private void Initialize(RemoteHostClient client, RemoteHostClient.Connection connection)
        {
            Contract.ThrowIfNull(client);
            Contract.ThrowIfNull(connection);

            lock (_gate)
            {
                if (_client != null)
                {
                    Contract.ThrowIfNull(_connectionDoNotAccessDirectly);

                    // someone else beat us and set the connection. 
                    // let this connection closed.
                    connection.Dispose();
                    return;
                }

                _client = client;
                _client.StatusChanged += OnStatusChanged;

                _connectionDoNotAccessDirectly = new ReferenceCountedDisposable<RemoteHostClient.Connection>(connection);
            }
        }
    }
}
