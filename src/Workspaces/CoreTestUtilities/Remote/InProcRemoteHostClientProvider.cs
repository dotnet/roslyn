// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests.Remote;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Testing
{
    internal sealed class InProcRemoteHostClientProvider : IRemoteHostClientProvider, IDisposable
    {
        [ExportWorkspaceServiceFactory(typeof(IRemoteHostClientProvider), ServiceLayer.Test), Shared, PartNotDiscoverable]
        internal sealed class Factory : IWorkspaceServiceFactory
        {
            private readonly RemoteServiceCallbackDispatcherRegistry _callbackDispatchers;

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory([ImportMany] IEnumerable<Lazy<IRemoteServiceCallbackDispatcher, RemoteServiceCallbackDispatcherRegistry.ExportMetadata>> callbackDispatchers)
                => _callbackDispatchers = new RemoteServiceCallbackDispatcherRegistry(callbackDispatchers);

            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                => new InProcRemoteHostClientProvider(workspaceServices, _callbackDispatchers);
        }

        private sealed class WorkspaceManager : RemoteWorkspaceManager
        {
            public WorkspaceManager(SolutionAssetCache assetStorage, ConcurrentDictionary<Guid, TestGeneratorReference> sharedTestGeneratorReferences, Type[]? additionalRemoteParts)
                : base(assetStorage)
            {
                LazyWorkspace = new Lazy<RemoteWorkspace>(
                    () =>
                    {
                        var hostServices = FeaturesTestCompositions.RemoteHost.AddParts(additionalRemoteParts).GetHostServices();

                        // We want to allow references to source generators to be shared between the "in proc" and "remote" workspaces and
                        // MEF compositions, so tell the serializer service to use the same map for this "remote" workspace as the in-proc one.
                        ((IMefHostExportProvider)hostServices).GetExportedValue<TestSerializerService.Factory>().SharedTestGeneratorReferences = sharedTestGeneratorReferences;
                        return new RemoteWorkspace(hostServices, WorkspaceKind.RemoteWorkspace);
                    });
            }

            public Lazy<RemoteWorkspace> LazyWorkspace { get; }

            public override RemoteWorkspace GetWorkspace()
                => LazyWorkspace.Value;
        }

        private readonly HostWorkspaceServices _services;
        private readonly Lazy<WorkspaceManager> _lazyManager;
        private readonly AsyncLazy<RemoteHostClient> _lazyClient;

        public SolutionAssetCache? RemoteAssetStorage { get; set; }
        public Type[]? AdditionalRemoteParts { get; set; }
        public TraceListener? TraceListener { get; set; }

        public InProcRemoteHostClientProvider(HostWorkspaceServices services, RemoteServiceCallbackDispatcherRegistry callbackDispatchers)
        {
            _services = services;

            var testSerializerServiceFactory = ((IMefHostExportProvider)services.HostServices).GetExportedValue<TestSerializerService.Factory>();

            _lazyManager = new Lazy<WorkspaceManager>(
                () => new WorkspaceManager(
                    RemoteAssetStorage ?? new SolutionAssetCache(),
                    testSerializerServiceFactory.SharedTestGeneratorReferences,
                    AdditionalRemoteParts));
            _lazyClient = new AsyncLazy<RemoteHostClient>(
                cancellationToken => InProcRemoteHostClient.CreateAsync(
                    _services,
                    callbackDispatchers,
                    TraceListener,
                    new RemoteHostTestData(_lazyManager.Value, isInProc: true)),
                cacheResult: true);
        }

        public void Dispose()
        {
            // Dispose the remote workspace when the owning host workspace is disposed
            if (_lazyManager.IsValueCreated)
            {
                var manager = _lazyManager.Value;
                if (manager.LazyWorkspace.IsValueCreated)
                {
                    manager.LazyWorkspace.Value.Dispose();
                }
            }
        }

        public Task<RemoteHostClient?> TryGetRemoteHostClientAsync(CancellationToken cancellationToken)
            => _lazyClient.GetValueAsync(cancellationToken).AsNullable();
    }
}
