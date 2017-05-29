// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            var outOfProcessAllowed = workspace.Options.GetOption(SymbolSearchOptions.OutOfProcessAllowed);
            if (outOfProcessAllowed)
            {
                var client = await workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    return new RemoteUpdateEngine(workspace, client, logService);
                }
            }

            // Couldn't go out of proc.  Just do everything inside the current process.
            return new SymbolSearchUpdateEngine(logService);
        }

        private class RemoteUpdateEngine : ISymbolSearchUpdateEngine
        {
            private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);

            private readonly Workspace _workspace;
            private readonly ISymbolSearchLogService _logService;

            private RemoteHostClient _client;
            private RemoteHostClient.Session _sessionDoNotAccessDirectly;

            public RemoteUpdateEngine(
                Workspace workspace, RemoteHostClient client,
                ISymbolSearchLogService logService)
            {
                _workspace = workspace;
                _logService = logService;

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

            private async Task<RemoteHostClient.Session> TryGetSessionAsync(CancellationToken cancellationToken)
            {
                using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
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
                        cancellationToken).ConfigureAwait(false);

                    return _sessionDoNotAccessDirectly;
                }
            }

            public async Task<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(
                string source, string name, int arity, CancellationToken cancellationToken)
            {
                var session = await TryGetSessionAsync(cancellationToken).ConfigureAwait(false);
                if (session == null)
                {
                    // we couldn't get session. most likely remote host is gone
                    return ImmutableArray<PackageWithTypeResult>.Empty;
                }

                var results = await session.InvokeWithCancellationAsync<SerializablePackageWithTypeResult[]>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindPackagesWithTypeAsync),
                    new object[] { source, name, arity },
                    cancellationToken).ConfigureAwait(false);

                return results.Select(r => r.Rehydrate()).ToImmutableArray();
            }

            public async Task<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
                string source, string assemblyName, CancellationToken cancellationToken)
            {
                var session = await TryGetSessionAsync(cancellationToken).ConfigureAwait(false);
                if (session == null)
                {
                    // we couldn't get session. most likely remote host is gone
                    return ImmutableArray<PackageWithAssemblyResult>.Empty;
                }

                var results = await session.InvokeWithCancellationAsync<SerializablePackageWithAssemblyResult[]>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindPackagesWithAssemblyAsync),
                    new[] { source, assemblyName },
                    cancellationToken).ConfigureAwait(false);

                return results.Select(r => r.Rehydrate()).ToImmutableArray();
            }

            public async Task<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
                string name, int arity, CancellationToken cancellationToken)
            {
                var session = await TryGetSessionAsync(cancellationToken).ConfigureAwait(false);
                if (session == null)
                {
                    // we couldn't get session. most likely remote host is gone
                    return ImmutableArray<ReferenceAssemblyWithTypeResult>.Empty;
                }

                var results = await session.InvokeWithCancellationAsync<SerializableReferenceAssemblyWithTypeResult[]>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindReferenceAssembliesWithTypeAsync),
                    new object[] { name, arity },
                    cancellationToken).ConfigureAwait(false);

                return results.Select(r => r.Rehydrate()).ToImmutableArray();
            }

            public async Task UpdateContinuouslyAsync(
                string sourceName, string localSettingsDirectory, CancellationToken cancellationToken)
            {
                var session = await TryGetSessionAsync(cancellationToken).ConfigureAwait(false);
                if (session == null)
                {
                    // we couldn't get session. most likely remote host is gone
                    return;
                }

                await session.InvokeWithCancellationAsync(
                    nameof(IRemoteSymbolSearchUpdateEngine.UpdateContinuouslyAsync),
                    new[] { sourceName, localSettingsDirectory },
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}