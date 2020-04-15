// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    [Obsolete("Only used by Razor and LUT", error: false)]
    internal sealed class SessionWithSolution : IDisposable
    {
        public readonly RemoteHostClient.Connection Connection;
        private readonly PinnedRemotableDataScope _scope;

        public static async Task<SessionWithSolution> CreateAsync(RemoteHostClient.Connection connection, Solution solution, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(connection);
            Contract.ThrowIfNull(solution);

            var service = solution.Workspace.Services.GetRequiredService<IRemotableDataService>();
            var scope = await service.CreatePinnedRemotableDataScopeAsync(solution, cancellationToken).ConfigureAwait(false);

            SessionWithSolution? session = null;
            try
            {
                // set connection state for this session.
                // we might remove this in future. see https://github.com/dotnet/roslyn/issues/24836
                await connection.InvokeAsync(
                    WellKnownServiceHubServices.ServiceHubServiceBase_Initialize,
                    new object[] { scope.SolutionInfo },
                    cancellationToken).ConfigureAwait(false);

                // transfer ownership of connection and scope to the session object:
                session = new SessionWithSolution(connection, scope);
            }
            finally
            {
                if (session == null)
                {
                    scope.Dispose();
                }
            }

            return session;
        }

        private SessionWithSolution(RemoteHostClient.Connection connection, PinnedRemotableDataScope scope)
        {
            Connection = connection;
            _scope = scope;
        }

        public void AddAdditionalAssets(CustomAsset asset)
            => _scope.AddAdditionalAsset(asset);

        public void Dispose()
        {
            _scope.Dispose();
            Connection.Dispose();
        }
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
        private readonly IRemotableDataService _remotableDataService;

        private readonly string _serviceName;
        private readonly object? _callbackTarget;

        private RemoteHostClient? _client;
        private ReferenceCountedDisposable<RemoteHostClient.Connection>? _lazyConnection;

        public KeepAliveSession(RemoteHostClient client, RemoteHostClient.Connection connection, string serviceName, object? callbackTarget)
        {
            _gate = new object();

            Initialize(client, connection);

            _remoteHostClientService = client.Workspace.Services.GetRequiredService<IRemoteHostClientService>();
            _remotableDataService = client.Workspace.Services.GetRequiredService<IRemotableDataService>();

            _serviceName = serviceName;
            _callbackTarget = callbackTarget;
        }

        public void Shutdown()
        {
            ReferenceCountedDisposable<RemoteHostClient.Connection>? connection;

            lock (_gate)
            {
                if (_client != null)
                {
                    _client.StatusChanged -= OnStatusChanged;
                }

                connection = _lazyConnection;

                _client = null;
                _lazyConnection = null;
            }

            connection?.Dispose();
        }

        public async Task<bool> TryInvokeAsync(string targetName, Solution? solution, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
        {
            using var connection = await TryGetConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return false;
            }

            await RemoteHostClient.RunRemoteAsync(connection.Target, _remotableDataService, targetName, solution, arguments, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public async Task<Optional<T>> TryInvokeAsync<T>(string targetName, Solution? solution, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
        {
            using var connection = await TryGetConnectionAsync(cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return default;
            }

            return await RemoteHostClient.RunRemoteAsync<T>(connection.Target, _remotableDataService, targetName, solution, arguments, dataReader: null, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ReferenceCountedDisposable<RemoteHostClient.Connection>?> TryGetConnectionAsync(CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (_lazyConnection != null)
                {
                    return _lazyConnection.TryAddReference();
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

        private void OnStatusChanged(object? sender, bool started)
        {
            if (started)
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
                    Contract.ThrowIfNull(_lazyConnection);

                    // someone else beat us and set the connection. 
                    // let this connection closed.
                    connection.Dispose();
                    return;
                }

                _client = client;
                _client.StatusChanged += OnStatusChanged;

                _lazyConnection = new ReferenceCountedDisposable<RemoteHostClient.Connection>(connection);
            }
        }
    }
}
