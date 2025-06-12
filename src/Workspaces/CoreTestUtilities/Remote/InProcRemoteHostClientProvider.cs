// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests.Remote;
using Microsoft.VisualStudio.Threading;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Testing;

#pragma warning disable CA1416 // Validate platform compatibility
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
            => new InProcRemoteHostClientProvider(workspaceServices.SolutionServices, _callbackDispatchers);
    }

    private sealed class WorkspaceManager : RemoteWorkspaceManager
    {
        public WorkspaceManager(
            Func<RemoteWorkspace, SolutionAssetCache> createAssetStorage,
            ConcurrentDictionary<Guid, TestGeneratorReference> sharedTestGeneratorReferences,
            Type[]? additionalRemoteParts,
            Type[]? excludedRemoteParts)
            : base(createAssetStorage, CreateRemoteWorkspace(sharedTestGeneratorReferences, additionalRemoteParts, excludedRemoteParts))
        {
        }
    }

    private static RemoteWorkspace CreateRemoteWorkspace(
        ConcurrentDictionary<Guid, TestGeneratorReference> sharedTestGeneratorReferences,
        Type[]? additionalRemoteParts,
        Type[]? excludedRemoteParts)
    {
        var hostServices = FeaturesTestCompositions.RemoteHost.AddParts(additionalRemoteParts).AddExcludedPartTypes(excludedRemoteParts).GetHostServices();

        // We want to allow references to source generators to be shared between the "in proc" and "remote" workspaces and
        // MEF compositions, so tell the serializer service to use the same map for this "remote" workspace as the in-proc one.
        ((IMefHostExportProvider)hostServices).GetExportedValue<TestSerializerService.Factory>().SharedTestGeneratorReferences = sharedTestGeneratorReferences;
        return new RemoteWorkspace(hostServices);
    }

    private readonly SolutionServices _services;
    private readonly Lazy<WorkspaceManager> _lazyManager;
    private readonly Lazy<RemoteHostClient> _lazyClient;
    private readonly TaskCompletionSource<bool> _clientCreationSource = new();

    public Type[]? AdditionalRemoteParts { get; set; }
    public Type[]? ExcludedRemoteParts { get; set; }
    public TraceListener? TraceListener { get; set; }

    public InProcRemoteHostClientProvider(SolutionServices services, RemoteServiceCallbackDispatcherRegistry callbackDispatchers)
    {
        _services = services;

        var testSerializerServiceFactory = services.ExportProvider.GetExportedValue<TestSerializerService.Factory>();

        _lazyManager = new Lazy<WorkspaceManager>(
            () => new WorkspaceManager(
                _ => new SolutionAssetCache(),
                testSerializerServiceFactory.SharedTestGeneratorReferences,
                AdditionalRemoteParts,
                ExcludedRemoteParts));
        _lazyClient = new Lazy<RemoteHostClient>(
            () =>
            {
                try
                {
                    return InProcRemoteHostClient.Create(
                        _services,
                        callbackDispatchers,
                        TraceListener,
                        new RemoteHostTestData(_lazyManager.Value, isInProc: true));
                }
                finally
                {
                    _clientCreationSource.SetResult(true);
                }
            });
    }

    public void Dispose()
    {
        // Dispose the remote workspace when the owning host workspace is disposed
        if (_lazyManager.IsValueCreated)
        {
            var manager = _lazyManager.Value;
            manager.GetWorkspace().Dispose();
        }
    }

    public Task<RemoteHostClient?> TryGetRemoteHostClientAsync(CancellationToken cancellationToken)
        => Task.FromResult<RemoteHostClient?>(_lazyClient.Value);

    public Task WaitForClientCreationAsync(CancellationToken cancellationToken)
        => _clientCreationSource.Task.WithCancellation(cancellationToken);
}
#pragma warning restore CA1416 // Validate platform compatibility

