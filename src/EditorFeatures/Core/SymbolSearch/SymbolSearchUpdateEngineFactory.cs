// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

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
                var session = await client.TryCreateKeepAliveSessionAsync(WellKnownServiceHubServices.RemoteSymbolSearchUpdateEngine, logService, cancellationToken).ConfigureAwait(false);
                if (session != null)
                {
                    return new RemoteUpdateEngine(workspace, session);
                }
            }

            // Couldn't go out of proc.  Just do everything inside the current process.
            return new SymbolSearchUpdateEngine(logService);
        }

        private class RemoteUpdateEngine : ISymbolSearchUpdateEngine
        {
            private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);

            private readonly Workspace _workspace;
            private readonly KeepAliveSession _session;

            public RemoteUpdateEngine(Workspace workspace, KeepAliveSession session)
            {
                _workspace = workspace;
                _session = session;
            }

            public async Task<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(
                string source, string name, int arity, CancellationToken cancellationToken)
            {
                var results = await _session.TryInvokeAsync<ImmutableArray<PackageWithTypeResult>>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindPackagesWithTypeAsync),
                    new object[] { source, name, arity }, cancellationToken).ConfigureAwait(false);

                return results.NullToEmpty();
            }

            public async Task<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
                string source, string assemblyName, CancellationToken cancellationToken)
            {
                var results = await _session.TryInvokeAsync<ImmutableArray<PackageWithAssemblyResult>>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindPackagesWithAssemblyAsync),
                    new object[] { source, assemblyName }, cancellationToken).ConfigureAwait(false);

                return results.NullToEmpty();
            }

            public async Task<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
                string name, int arity, CancellationToken cancellationToken)
            {
                var results = await _session.TryInvokeAsync<ImmutableArray<ReferenceAssemblyWithTypeResult>>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindReferenceAssembliesWithTypeAsync),
                    new object[] { name, arity }, cancellationToken).ConfigureAwait(false);

                return results.NullToEmpty();
            }

            public async Task UpdateContinuouslyAsync(
                string sourceName, string localSettingsDirectory)
            {
                await _session.TryInvokeAsync(
                    nameof(IRemoteSymbolSearchUpdateEngine.UpdateContinuouslyAsync),
                    new object[] { sourceName, localSettingsDirectory }, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
