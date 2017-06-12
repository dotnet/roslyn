// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Experiments;
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
                var session = await client.TryCreateServiceKeepAliveSessionAsync(WellKnownServiceHubServices.RemoteSymbolSearchUpdateEngine, logService, cancellationToken).ConfigureAwait(false);
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
            private readonly KeepAliveSessionHolder _session;

            public RemoteUpdateEngine(Workspace workspace, KeepAliveSessionHolder session)
            {
                _workspace = workspace;
                _session = session;
            }

            public async Task<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(
                string source, string name, int arity)
            {
                var (success, results) = await _session.TryInvokeAsync<ImmutableArray<PackageWithTypeResult>>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindPackagesWithTypeAsync),
                    source, name, arity).ConfigureAwait(false);
                if (!success)
                {
                    // keep alive session couldn't run. most likely remote host is gone
                    return ImmutableArray<PackageWithTypeResult>.Empty;
                }

                return results;
            }

            public async Task<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
                string source, string assemblyName)
            {
                var (success, results) = await _session.TryInvokeAsync<ImmutableArray<PackageWithAssemblyResult>>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindPackagesWithAssemblyAsync),
                    source, assemblyName).ConfigureAwait(false);
                if (!success)
                {
                    // keep alive session couldn't run. most likely remote host is gone
                    return ImmutableArray<PackageWithAssemblyResult>.Empty;
                }

                return results;
            }

            public async Task<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
                string name, int arity)
            {
                var (success, results) = await _session.TryInvokeAsync<ImmutableArray<ReferenceAssemblyWithTypeResult>>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindReferenceAssembliesWithTypeAsync),
                    name, arity).ConfigureAwait(false);
                if (!success)
                {
                    // keep alive session couldn't run. most likely remote host is gone
                    return ImmutableArray<ReferenceAssemblyWithTypeResult>.Empty;
                }

                return results;
            }

            public async Task UpdateContinuouslyAsync(
                string sourceName, string localSettingsDirectory)
            {
                await _session.TryInvokeAsync(
                    nameof(IRemoteSymbolSearchUpdateEngine.UpdateContinuouslyAsync),
                    sourceName, localSettingsDirectory).ConfigureAwait(false);
            }
        }
    }
}