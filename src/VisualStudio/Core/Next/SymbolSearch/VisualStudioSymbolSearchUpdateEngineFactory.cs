// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Arguments;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.LanguageServices.Remote;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    /// <summary>
    /// Implementation of the <see cref="ISymbolSearchUpdateEngineFactory"/> that attempts to connect
    /// to the ServiceHub service to offload the work to.
    /// </summary>
    [ExportWorkspaceService(typeof(ISymbolSearchUpdateEngineFactory), ServiceLayer.Host), Shared]
    internal class VisualStudioSymbolSearchUpdateEngineFactory : ISymbolSearchUpdateEngineFactory
    {
        public async Task<ISymbolSearchUpdateEngine> CreateEngineAsync(
            CodeAnalysis.Workspace workspace, ISymbolSearchLogService logService, CancellationToken cancellationToken)
        {
            var clientService = workspace.Services.GetService<IRemoteHostClientService>();
            if (clientService == null)
            {
                return new SymbolSearchUpdateEngine(logService);
            }

            var client = await clientService.GetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return new SymbolSearchUpdateEngine(logService);
            }

            var emptySolution = workspace.CreateSolution(workspace.CurrentSolution.Id);

            // We create a single session and use it for the entire lifetime of this process.
            // That single session will be used to do all communication with the remote process.
            // This is because each session will cause a new instance of the RemoteSymbolSearchUpdateEngine
            // to be created on the remote side.  We only want one instance of that type.  The
            // alternative is to make that type static variable on the remote side.  But that's
            // much less clean and would make some of the state management much more complex.
            var session = await client.CreateServiceSessionAsync(
                WellKnownServiceHubServices.RemoteSymbolSearchUpdateEngine,
                emptySolution, logService, cancellationToken).ConfigureAwait(false);

            return new RemoteUpdateEngine(session);
        }

        private class RemoteUpdateEngine : ISymbolSearchUpdateEngine
        {
            private readonly RemoteHostClient.Session _session;

            public RemoteUpdateEngine(RemoteHostClient.Session session)
            {
                _session = session;
            }

            public void Dispose()
            {
                _session.Dispose();
            }

            public async Task<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(
                string source, string name, int arity)
            {
                var results = await _session.InvokeAsync<SerializablePackageWithTypeResult[]>(
                    WellKnownServiceHubServices.RemoteSymbolSearchUpdateEngine_FindPackagesWithTypeAsync,
                    source, name, arity).ConfigureAwait(false);

                return results.Select(r => r.Rehydrate()).ToImmutableArray();
            }

            public async Task<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
                string name, int arity)
            {
                var results = await _session.InvokeAsync<SerializableReferenceAssemblyWithTypeResult[]>(
                    WellKnownServiceHubServices.RemoteSymbolSearchUpdateEngine_FindReferenceAssembliesWithTypeAsync,
                    name, arity).ConfigureAwait(false);

                return results.Select(r => r.Rehydrate()).ToImmutableArray();
            }

            public Task StopUpdatesAsync()
            {
                return _session.InvokeAsync(
                    WellKnownServiceHubServices.RemoteSymbolSearchUpdateEngine_StopUpdatesAsync);
            }

            public Task UpdateContinuouslyAsync(
                string sourceName, string localSettingsDirectory)
            {
                return _session.InvokeAsync(
                    WellKnownServiceHubServices.RemoteSymbolSearchUpdateEngine_UpdateContinuouslyAsync,
                    sourceName, localSettingsDirectory);
            }
        }
    }
}
