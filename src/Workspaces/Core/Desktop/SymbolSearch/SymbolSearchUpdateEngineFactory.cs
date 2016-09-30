// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
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
            var outOfProcessAllowed = workspace.Options.GetOption(SymbolSearchOptions.OutOfProcessAllowed);
            if (outOfProcessAllowed)
            {
                var client = await workspace.GetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
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
            }

            // Couldn't go out of proc.  Just do everything inside the current process.
            return new SymbolSearchUpdateEngine(logService);
        }

        private class RemoteUpdateEngine : ISymbolSearchUpdateEngine
        {
            private readonly RemoteHostClient.Session _session;

            public RemoteUpdateEngine(RemoteHostClient.Session session)
            {
                _session = session;
            }

            public async Task<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(
                string source, string name, int arity)
            {
                var results = await _session.InvokeAsync<SerializablePackageWithTypeResult[]>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindPackagesWithTypeAsync),
                    source, name, arity).ConfigureAwait(false);

                return results.Select(r => r.Rehydrate()).ToImmutableArray();
            }

            public async Task<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
                string name, int arity)
            {
                var results = await _session.InvokeAsync<SerializableReferenceAssemblyWithTypeResult[]>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindReferenceAssembliesWithTypeAsync),
                    name, arity).ConfigureAwait(false);

                return results.Select(r => r.Rehydrate()).ToImmutableArray();
            }

            public Task UpdateContinuouslyAsync(
                string sourceName, string localSettingsDirectory)
            {
                return _session.InvokeAsync(
                    nameof(IRemoteSymbolSearchUpdateEngine.UpdateContinuouslyAsync),
                    sourceName, localSettingsDirectory);
            }
        }
    }
}