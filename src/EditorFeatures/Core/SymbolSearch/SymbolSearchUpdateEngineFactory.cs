// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    /// <summary>
    /// Factory that will produce the <see cref="ISymbolSearchUpdateEngine"/>.  The default
    /// implementation produces an engine that will run in-process.  Implementations at
    /// other layers can behave differently (for example, running the engine out-of-process).
    /// </summary>
    internal static class SymbolSearchUpdateEngineFactory
    {
        public static async Task<ISymbolSearchUpdateEngine> CreateEngineAsync(
            Workspace workspace, ISymbolSearchLogService logService, CancellationToken cancellationToken)
        {
            var client = await workspace.TryGetRemoteHostClientAsync(
                RemoteFeatureOptions.SymbolSearchEnabled, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                return new RemoteUpdateEngine(workspace, client, logService, cancellationToken);
            }

            // Couldn't go out of proc.  Just do everything inside the current process.
            return new SymbolSearchUpdateEngine(logService);
        }

        private class RemoteUpdateEngine : ISymbolSearchUpdateEngine
        {
            private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);

            private readonly Workspace _workspace;
            private readonly ISymbolSearchLogService _logService;
            private readonly CancellationToken _cancellationToken;

            private RemoteHostClient _client;
            private RemoteHostClient.Session _sessionDoNotAccessDirectly;

            public RemoteUpdateEngine(
                Workspace workspace, RemoteHostClient client,
                ISymbolSearchLogService logService, CancellationToken cancellationToken)
            {
                _workspace = workspace;
                _logService = logService;
                _cancellationToken = cancellationToken;

                // this engine is stateful service which maintaining a connection to remote host. so
                // this feature is required to handle remote host recycle situation.
                _client = client;
                _client.ConnectionChanged += OnConnectionChanged;
            }

            private async void OnConnectionChanged(object sender, bool connected)
            {
                if (connected)
                {
                    return;
                }

                // to make things simpler, this is not cancellable. I believe this
                // is okay since this handle rare cases where remote host is recycled or
                // removed
                using (await _gate.DisposableWaitAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    _client.ConnectionChanged -= OnConnectionChanged;

                    _sessionDoNotAccessDirectly?.Dispose();
                    _sessionDoNotAccessDirectly = null;

                    _client = await _workspace.TryGetRemoteHostClientAsync(CancellationToken.None).ConfigureAwait(false);
                    if (_client != null)
                    {
                        // client can be null if host is shutting down
                        _client.ConnectionChanged += OnConnectionChanged;
                    }
                }
            }

            private async Task<RemoteHostClient.Session> TryGetSessionAsync()
            {
                using (await _gate.DisposableWaitAsync(_cancellationToken).ConfigureAwait(false))
                {
                    if (_sessionDoNotAccessDirectly != null)
                    {
                        return _sessionDoNotAccessDirectly;
                    }

                    if (_client == null)
                    {
                        // client can be null if host is shutting down
                        return null;
                    }

                    // We create a single session and use it for the entire lifetime of this process.
                    // That single session will be used to do all communication with the remote process.
                    // This is because each session will cause a new instance of the RemoteSymbolSearchUpdateEngine
                    // to be created on the remote side.  We only want one instance of that type.  The
                    // alternative is to make that type static variable on the remote side.  But that's
                    // much less clean and would make some of the state management much more complex.
                    _sessionDoNotAccessDirectly = await _client.TryCreateServiceSessionAsync(
                        WellKnownServiceHubServices.RemoteSymbolSearchUpdateEngine,
                        _logService,
                        _cancellationToken).ConfigureAwait(false);

                    return _sessionDoNotAccessDirectly;
                }
            }

            public async Task<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(
                string source, string name, int arity)
            {
                var session = await TryGetSessionAsync().ConfigureAwait(false);
                if (session == null)
                {
                    // we couldn't get session. most likely remote host is gone
                    return ImmutableArray<PackageWithTypeResult>.Empty;
                }

                var results = await session.InvokeAsync<IList<PackageWithTypeResult>>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindPackagesWithTypeAsync),
                    source, name, arity).ConfigureAwait(false);

                return results.ToImmutableArrayOrEmpty();
            }

            public async Task<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
                string source, string assemblyName)
            {
                var session = await TryGetSessionAsync().ConfigureAwait(false);
                if (session == null)
                {
                    // we couldn't get session. most likely remote host is gone
                    return ImmutableArray<PackageWithAssemblyResult>.Empty;
                }

                var results = await session.InvokeAsync<IList<PackageWithAssemblyResult>>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindPackagesWithAssemblyAsync),
                    source, assemblyName).ConfigureAwait(false);

                return results.ToImmutableArrayOrEmpty();
            }

            public async Task<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
                string name, int arity)
            {
                var session = await TryGetSessionAsync().ConfigureAwait(false);
                if (session == null)
                {
                    // we couldn't get session. most likely remote host is gone
                    return ImmutableArray<ReferenceAssemblyWithTypeResult>.Empty;
                }

                var results = await session.InvokeAsync<IList<ReferenceAssemblyWithTypeResult>>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindReferenceAssembliesWithTypeAsync),
                    name, arity).ConfigureAwait(false);

                return results.ToImmutableArrayOrEmpty();
            }

            public async Task UpdateContinuouslyAsync(
                string sourceName, string localSettingsDirectory)
            {
                var session = await TryGetSessionAsync().ConfigureAwait(false);
                if (session == null)
                {
                    // we couldn't get session. most likely remote host is gone
                    return;
                }

                await session.InvokeAsync(
                    nameof(IRemoteSymbolSearchUpdateEngine.UpdateContinuouslyAsync),
                    sourceName, localSettingsDirectory).ConfigureAwait(false);
            }
        }
    }
}