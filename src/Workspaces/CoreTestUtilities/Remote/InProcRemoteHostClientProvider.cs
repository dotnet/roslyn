// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Testing
{
    internal sealed class InProcRemoteHostClientProvider : IRemoteHostClientProvider
    {
        [ExportWorkspaceServiceFactory(typeof(IRemoteHostClientProvider), ServiceLayer.Test), Shared, PartNotDiscoverable]
        internal sealed class Factory : IWorkspaceServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                => new InProcRemoteHostClientProvider(workspaceServices);
        }

        private sealed class WorkspaceManager : RemoteWorkspaceManager
        {
            private readonly Lazy<RemoteWorkspace> _lazyWorkspace;

            public WorkspaceManager(SolutionAssetCache assetStorage, Type[]? additionalRemoteParts)
                : base(assetStorage)
            {
                _lazyWorkspace = new Lazy<RemoteWorkspace>(
                    () => new RemoteWorkspace(FeaturesTestCompositions.RemoteHost.AddParts(additionalRemoteParts).GetHostServices(), WorkspaceKind.RemoteWorkspace));
            }

            public override RemoteWorkspace GetWorkspace()
                => _lazyWorkspace.Value;
        }

        private readonly HostWorkspaceServices _services;
        private readonly AsyncLazy<RemoteHostClient> _lazyClient;

        public SolutionAssetCache? RemoteAssetStorage { get; }
        public Type[]? AdditionalRemoteParts { get; }
        public TraceListener? TraceListener { get; set; }

        public InProcRemoteHostClientProvider(HostWorkspaceServices services)
        {
            _services = services;

            _lazyClient = new AsyncLazy<RemoteHostClient>(
                cancellationToken => InProcRemoteHostClient.CreateAsync(
                    _services,
                    TraceListener,
                    new RemoteHostTestData(
                        new WorkspaceManager(RemoteAssetStorage ?? new SolutionAssetCache(), AdditionalRemoteParts),
                        isInProc: true)),
                cacheResult: true);
        }

        public Task<RemoteHostClient?> TryGetRemoteHostClientAsync(CancellationToken cancellationToken)
            => _lazyClient.GetValueAsync(cancellationToken).AsNullable();
    }
}
