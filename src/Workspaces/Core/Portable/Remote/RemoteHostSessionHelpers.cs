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

        public static async Task<SessionWithSolution> CreateAsync(RemoteHostClient.Connection connection, PinnedRemotableDataScope scope, CancellationToken cancellationToken)
        {
            var sessionWithSolution = new SessionWithSolution(connection, scope);

            try
            {
                await connection.RegisterPinnedRemotableDataScopeAsync(scope).ConfigureAwait(false);
                return sessionWithSolution;
            }
            catch
            {
                sessionWithSolution.Dispose();

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
    /// and also make sure state is correct even if multiple threads call TryInvokeAsync at the same time. but this 
    /// is not optimized to handle highly concurrent usage. if highly concurrent usage is required, either using
    /// <see cref="RemoteHostClient.Connection"/> direclty or using <see cref="SessionWithSolution"/> would be better choice
    /// </summary>
    internal sealed class KeepAliveSession
    {
        private readonly SemaphoreSlim _gate;
        private readonly IRemoteHostClientService _remoteHostClientService;

        private readonly string _serviceName;
        private readonly object _callbackTarget;

        private RemoteHostClient _client;
        private RemoteHostClient.Connection _connection;

        public KeepAliveSession(RemoteHostClient client, RemoteHostClient.Connection connection, string serviceName, object callbackTarget)
        {
            Initialize_NoLock(client, connection);

            _gate = new SemaphoreSlim(initialCount: 1);
            _remoteHostClientService = client.Workspace.Services.GetService<IRemoteHostClientService>();

            _serviceName = serviceName;
            _callbackTarget = callbackTarget;
        }

        public void Shutdown(CancellationToken cancellationToken)
        {
            using (_gate.DisposableWait(cancellationToken))
            {
                if (_client != null)
                {
                    _client.StatusChanged -= OnStatusChanged;
                }

                _connection?.Dispose();

                _client = null;
                _connection = null;
            }
        }

        public async Task<bool> TryInvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var connection = await TryGetConnection_NoLockAsync(cancellationToken).ConfigureAwait(false);
                if (connection == null)
                {
                    return false;
                }

                await connection.InvokeAsync(targetName, arguments, cancellationToken).ConfigureAwait(false);
                return true;
            }
        }

        public async Task<T> TryInvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var connection = await TryGetConnection_NoLockAsync(cancellationToken).ConfigureAwait(false);
                if (connection == null)
                {
                    return default;
                }

                return await connection.InvokeAsync<T>(targetName, arguments, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<bool> TryInvokeAsync(string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var connection = await TryGetConnection_NoLockAsync(cancellationToken).ConfigureAwait(false);
                if (connection == null)
                {
                    return false;
                }

                await connection.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync, cancellationToken).ConfigureAwait(false);
                return true;
            }
        }

        public async Task<T> TryInvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var connection = await TryGetConnection_NoLockAsync(cancellationToken).ConfigureAwait(false);
                if (connection == null)
                {
                    return default;
                }

                return await connection.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<bool> TryInvokeAsync(string targetName, Solution solution, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            using (var scope = await solution.GetPinnedScopeAsync(cancellationToken).ConfigureAwait(false))
            {
                var connection = await TryGetConnection_NoLockAsync(cancellationToken).ConfigureAwait(false);
                if (connection == null)
                {
                    return false;
                }

                await connection.RegisterPinnedRemotableDataScopeAsync(scope).ConfigureAwait(false);
                await connection.InvokeAsync(targetName, arguments, cancellationToken).ConfigureAwait(false);
                return true;
            }
        }

        public async Task<T> TryInvokeAsync<T>(string targetName, Solution solution, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            using (var scope = await solution.GetPinnedScopeAsync(cancellationToken).ConfigureAwait(false))
            {
                var connection = await TryGetConnection_NoLockAsync(cancellationToken).ConfigureAwait(false);
                if (connection == null)
                {
                    return default;
                }

                await connection.RegisterPinnedRemotableDataScopeAsync(scope).ConfigureAwait(false);
                return await connection.InvokeAsync<T>(targetName, arguments, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<bool> TryInvokeAsync(
            string targetName, Solution solution, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            using (var scope = await solution.GetPinnedScopeAsync(cancellationToken).ConfigureAwait(false))
            {
                var connection = await TryGetConnection_NoLockAsync(cancellationToken).ConfigureAwait(false);
                if (connection == null)
                {
                    return false;
                }

                await connection.RegisterPinnedRemotableDataScopeAsync(scope).ConfigureAwait(false);
                await connection.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync, cancellationToken).ConfigureAwait(false);
                return true;
            }
        }

        public async Task<T> TryInvokeAsync<T>(
            string targetName, Solution solution, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            using (var scope = await solution.GetPinnedScopeAsync(cancellationToken).ConfigureAwait(false))
            {
                var connection = await TryGetConnection_NoLockAsync(cancellationToken).ConfigureAwait(false);
                if (connection == null)
                {
                    return default;
                }

                await connection.RegisterPinnedRemotableDataScopeAsync(scope).ConfigureAwait(false);
                return await connection.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<RemoteHostClient.Connection> TryGetConnection_NoLockAsync(CancellationToken cancellationToken)
        {
            if (_connection != null)
            {
                return _connection;
            }

            var client = await _remoteHostClientService.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return null;
            }

            var session = await client.TryCreateConnectionAsync(_serviceName, _callbackTarget, cancellationToken).ConfigureAwait(false);
            if (session == null)
            {
                return null;
            }

            Initialize_NoLock(client, session);

            return _connection;
        }

        private void OnStatusChanged(object sender, bool connection)
        {
            if (connection)
            {
                return;
            }

            Shutdown(CancellationToken.None);
        }

        private void Initialize_NoLock(RemoteHostClient client, RemoteHostClient.Connection connection)
        {
            Contract.ThrowIfNull(client);
            Contract.ThrowIfNull(connection);

            _client = client;
            _client.StatusChanged += OnStatusChanged;

            _connection = connection;
        }
    }
}
