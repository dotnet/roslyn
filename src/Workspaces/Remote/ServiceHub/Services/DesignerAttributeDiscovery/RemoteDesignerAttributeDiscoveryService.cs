// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteDesignerAttributeDiscoveryService : BrokeredServiceBase, IRemoteDesignerAttributeDiscoveryService
    {
        internal sealed class Factory : FactoryBase<IRemoteDesignerAttributeDiscoveryService, IRemoteDesignerAttributeDiscoveryService.ICallback>
        {
            protected override IRemoteDesignerAttributeDiscoveryService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteDesignerAttributeDiscoveryService.ICallback> callback)
                => new RemoteDesignerAttributeDiscoveryService(arguments, callback);
        }

        private readonly RemoteCallback<IRemoteDesignerAttributeDiscoveryService.ICallback> _callback;

        public RemoteDesignerAttributeDiscoveryService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteDesignerAttributeDiscoveryService.ICallback> callback)
            : base(arguments)
        {
            _callback = callback;
        }

        public ValueTask StartScanningForDesignerAttributesAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                var registrationService = GetWorkspace().Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
                var analyzerProvider = new RemoteDesignerAttributeIncrementalAnalyzerProvider(_callback, callbackId);

                registrationService.AddAnalyzerProvider(
                    analyzerProvider,
                    new IncrementalAnalyzerProviderMetadata(
                        nameof(RemoteDesignerAttributeIncrementalAnalyzerProvider),
                        highPriorityForActiveFile: true,
                        workspaceKinds: WorkspaceKind.RemoteWorkspace));

                return default;
            }, cancellationToken);
        }
    }
}
