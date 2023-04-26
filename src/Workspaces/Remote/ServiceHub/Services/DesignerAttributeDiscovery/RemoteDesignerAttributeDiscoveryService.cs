// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DesignerAttribute;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteDesignerAttributeDiscoveryService : BrokeredServiceBase, IRemoteDesignerAttributeDiscoveryService
    {
        private sealed class CallbackWrapper : IDesignerAttributeDiscoveryService.ICallback
        {
            private readonly RemoteCallback<IRemoteDesignerAttributeDiscoveryService.ICallback> _callback;
            private readonly RemoteServiceCallbackId _callbackId;

            public CallbackWrapper(
                RemoteCallback<IRemoteDesignerAttributeDiscoveryService.ICallback> callback,
                RemoteServiceCallbackId callbackId)
            {
                _callback = callback;
                _callbackId = callbackId;
            }

            public ValueTask ReportDesignerAttributeDataAsync(ImmutableArray<DesignerAttributeData> data, CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.ReportDesignerAttributeDataAsync(_callbackId, data, cancellationToken), cancellationToken);
        }

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

        public ValueTask DiscoverDesignerAttributesAsync(
            RemoteServiceCallbackId callbackId,
            Checksum solutionChecksum,
            DocumentId? priorityDocument,
            bool useFrozenSnapshots,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(
                solutionChecksum,
                solution =>
                {
                    var service = solution.Services.GetRequiredService<IDesignerAttributeDiscoveryService>();
                    return service.ProcessSolutionAsync(
                        solution, priorityDocument, useFrozenSnapshots, new CallbackWrapper(_callback, callbackId), cancellationToken);
                },
                cancellationToken);
        }
    }
}
