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
    /// Helper type for common session case
    /// 
    /// This will tie <see cref="Solution"/> and <see cref="RemoteHostClient.Session"/>'s lifetime together
    /// so that one can handle those more easily
    /// </summary>
    internal sealed class SolutionAndSessionHolder : IDisposable
    {
        private readonly RemoteHostClient.Session _session;
        private readonly PinnedRemotableDataScope _scope;
        private readonly CancellationToken _cancellationToken;

        public static async Task<SolutionAndSessionHolder> CreateAsync(RemoteHostClient.Session session, PinnedRemotableDataScope scope, CancellationToken cancellationToken)
        {
            var sessionWithSolution = new SolutionAndSessionHolder(session, scope, cancellationToken);

            try
            {
                await session.RegisterPinnedRemotableDataScopeAsync(scope).ConfigureAwait(false);
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

        private SolutionAndSessionHolder(RemoteHostClient.Session session, PinnedRemotableDataScope scope, CancellationToken cancellationToken)
        {
            _session = session;
            _scope = scope;
            _cancellationToken = cancellationToken;
        }

        public void AddAdditionalAssets(CustomAsset asset)
        {
            _scope.AddAdditionalAsset(asset, _cancellationToken);
        }

        public void Dispose()
        {
            _scope.Dispose();
            _session.Dispose();
        }

        public Task InvokeAsync(string targetName, params object[] arguments) =>
            _session.InvokeAsync(targetName, arguments);
        public Task<T> InvokeAsync<T>(string targetName, params object[] arguments) =>
            _session.InvokeAsync<T>(targetName, arguments);
        public Task InvokeAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync) =>
            _session.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync);
        public Task<T> InvokeAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync) =>
            _session.InvokeAsync<T>(targetName, arguments, funcWithDirectStreamAsync);
    }

    /// <summary>
    /// Helper type for common session case
    /// 
    /// This will let one to hold onto <see cref="RemoteHostClient.Session"/> for a while.
    /// this helper will let you not care about remote host being gone while you hold onto the session if that ever happen
    /// 
    /// and also make sure state is correct even if multiple threads call TryInvokeAsync at the same time. but this 
    /// is not optimized to handle highly concurrent usage. if highly concurrent usage is required, either using
    /// <see cref="RemoteHostClient.Session"/> direclty or using <see cref="SolutionAndSessionHolder"/> would be better choice
    /// </summary>
    internal sealed class KeepAliveSessionHolder
    {
        private readonly SemaphoreSlim _gate;
        private readonly IRemoteHostClientService _remoteHostClientService;
        private readonly CancellationToken _cancellationToken;

        private readonly string _serviceName;
        private readonly object _callbackTarget;

        private RemoteHostClient _client;
        private RemoteHostClient.Session _session;

        public KeepAliveSessionHolder(RemoteHostClient client, RemoteHostClient.Session session, string serviceName, object callbackTarget, CancellationToken cancellationToken)
        {
            Initialize_NoLock(client, session);

            _gate = new SemaphoreSlim(initialCount: 1);
            _remoteHostClientService = client.Workspace.Services.GetService<IRemoteHostClientService>();
            _cancellationToken = cancellationToken;

            _serviceName = serviceName;
            _callbackTarget = callbackTarget;
        }

        public void Shutdown()
        {
            using (_gate.DisposableWait(_cancellationToken))
            {
                if (_client != null)
                {
                    _client.ConnectionChanged -= OnConnectionChanged;
                }

                _session?.Dispose();

                _client = null;
                _session = null;
            }
        }

        public async Task<bool> TryInvokeAsync(string targetName, params object[] arguments)
        {
            using (await _gate.DisposableWaitAsync(_cancellationToken).ConfigureAwait(false))
            {
                var session = await TryGetSession_NoLockAsync().ConfigureAwait(false);
                if (session == null)
                {
                    return false;
                }

                await session.InvokeAsync(targetName, arguments).ConfigureAwait(false);
                return true;
            }
        }

        public async Task<(bool success, T result)> TryInvokeAsync<T>(string targetName, params object[] arguments)
        {
            using (await _gate.DisposableWaitAsync(_cancellationToken).ConfigureAwait(false))
            {
                var session = await TryGetSession_NoLockAsync().ConfigureAwait(false);
                if (session == null)
                {
                    return (false, default(T));
                }

                return (true, await session.InvokeAsync<T>(targetName, arguments).ConfigureAwait(false));
            }
        }

        public async Task<bool> TryInvokeAsync(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync)
        {
            using (await _gate.DisposableWaitAsync(_cancellationToken).ConfigureAwait(false))
            {
                var session = await TryGetSession_NoLockAsync().ConfigureAwait(false);
                if (session == null)
                {
                    return false;
                }

                await session.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync).ConfigureAwait(false);
                return true;
            }
        }

        public async Task<(bool success, T result)> TryInvokeAsync<T>(string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync)
        {
            using (await _gate.DisposableWaitAsync(_cancellationToken).ConfigureAwait(false))
            {
                var session = await TryGetSession_NoLockAsync().ConfigureAwait(false);
                if (session == null)
                {
                    return (false, default(T));
                }

                return (true, await session.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync).ConfigureAwait(false));
            }
        }

        public async Task<bool> TryInvokeAsync(string targetName, Solution solution, params object[] arguments)
        {
            using (await _gate.DisposableWaitAsync(_cancellationToken).ConfigureAwait(false))
            using (var scope = await solution.GetPinnedScopeAsync(_cancellationToken).ConfigureAwait(false))
            {
                var session = await TryGetSession_NoLockAsync().ConfigureAwait(false);
                if (session == null)
                {
                    return false;
                }

                await session.RegisterPinnedRemotableDataScopeAsync(scope).ConfigureAwait(false);
                await session.InvokeAsync(targetName, arguments).ConfigureAwait(false);
                return true;
            }
        }

        public async Task<(bool success, T result)> TryInvokeAsync<T>(string targetName, Solution solution, params object[] arguments)
        {
            using (await _gate.DisposableWaitAsync(_cancellationToken).ConfigureAwait(false))
            using (var scope = await solution.GetPinnedScopeAsync(_cancellationToken).ConfigureAwait(false))
            {
                var session = await TryGetSession_NoLockAsync().ConfigureAwait(false);
                if (session == null)
                {
                    return (false, default(T));
                }

                await session.RegisterPinnedRemotableDataScopeAsync(scope).ConfigureAwait(false);
                return (true, await session.InvokeAsync<T>(targetName, arguments).ConfigureAwait(false));
            }
        }

        public async Task<bool> TryInvokeAsync(
            string targetName, Solution solution, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync)
        {
            using (await _gate.DisposableWaitAsync(_cancellationToken).ConfigureAwait(false))
            using (var scope = await solution.GetPinnedScopeAsync(_cancellationToken).ConfigureAwait(false))
            {
                var session = await TryGetSession_NoLockAsync().ConfigureAwait(false);
                if (session == null)
                {
                    return false;
                }

                await session.RegisterPinnedRemotableDataScopeAsync(scope).ConfigureAwait(false);
                await session.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync).ConfigureAwait(false);
                return true;
            }
        }

        public async Task<(bool success, T result)> TryInvokeAsync<T>(string targetName, Solution solution, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync)
        {
            using (await _gate.DisposableWaitAsync(_cancellationToken).ConfigureAwait(false))
            using (var scope = await solution.GetPinnedScopeAsync(_cancellationToken).ConfigureAwait(false))
            {
                var session = await TryGetSession_NoLockAsync().ConfigureAwait(false);
                if (session == null)
                {
                    return (false, default(T));
                }

                await session.RegisterPinnedRemotableDataScopeAsync(scope).ConfigureAwait(false);
                return (true, await session.InvokeAsync(targetName, arguments, funcWithDirectStreamAsync).ConfigureAwait(false));
            }
        }

        private async Task<RemoteHostClient.Session> TryGetSession_NoLockAsync()
        {
            if (_session != null)
            {
                return _session;
            }

            var client = await _remoteHostClientService.TryGetRemoteHostClientAsync(_cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return null;
            }

            var session = await client.TryCreateSessionAsync(_serviceName, _callbackTarget, _cancellationToken).ConfigureAwait(false);
            if (session == null)
            {
                return null;
            }

            Initialize_NoLock(client, session);

            return _session;
        }

        private void OnConnectionChanged(object sender, bool connection)
        {
            if (connection)
            {
                return;
            }

            Shutdown();
        }

        private void Initialize_NoLock(RemoteHostClient client, RemoteHostClient.Session session)
        {
            Contract.ThrowIfNull(client);
            Contract.ThrowIfNull(session);

            _client = client;
            _client.ConnectionChanged += OnConnectionChanged;

            _session = session;
        }
    }
}
