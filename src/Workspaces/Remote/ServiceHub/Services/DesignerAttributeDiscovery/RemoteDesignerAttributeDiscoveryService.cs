// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DesignerAttribute;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteDesignerAttributeDiscoveryService(
    in BrokeredServiceBase.ServiceConstructionArguments arguments,
    RemoteCallback<IRemoteDesignerAttributeDiscoveryService.ICallback> callback)
    : BrokeredServiceBase(arguments), IRemoteDesignerAttributeDiscoveryService
{
    private sealed class CallbackWrapper(
        RemoteCallback<IRemoteDesignerAttributeDiscoveryService.ICallback> callback,
        RemoteServiceCallbackId callbackId) : IDesignerAttributeDiscoveryService.ICallback
    {
        private readonly RemoteCallback<IRemoteDesignerAttributeDiscoveryService.ICallback> _callback = callback;

        public ValueTask ReportDesignerAttributeDataAsync(ImmutableArray<DesignerAttributeData> data, CancellationToken cancellationToken)
            => _callback.InvokeAsync((callback, cancellationToken) => callback.ReportDesignerAttributeDataAsync(callbackId, data, cancellationToken), cancellationToken);
    }

    internal sealed class Factory : FactoryBase<IRemoteDesignerAttributeDiscoveryService, IRemoteDesignerAttributeDiscoveryService.ICallback>
    {
        protected override IRemoteDesignerAttributeDiscoveryService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteDesignerAttributeDiscoveryService.ICallback> callback)
            => new RemoteDesignerAttributeDiscoveryService(arguments, callback);
    }

    public ValueTask DiscoverDesignerAttributesAsync(
        RemoteServiceCallbackId callbackId,
        Checksum solutionChecksum,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(
            solutionChecksum,
            solution =>
            {
                var service = solution.Services.GetRequiredService<IDesignerAttributeDiscoveryService>();
                return service.ProcessSolutionAsync(
                    solution, new CallbackWrapper(callback, callbackId), cancellationToken);
            },
            cancellationToken);
    }

    public ValueTask DiscoverDesignerAttributesAsync(
        RemoteServiceCallbackId callbackId,
        Checksum solutionChecksum,
        DocumentId priorityDocument,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(
            solutionChecksum,
            solution =>
            {
                var service = solution.Services.GetRequiredService<IDesignerAttributeDiscoveryService>();
                return service.ProcessPriorityDocumentAsync(
                    solution, priorityDocument, new CallbackWrapper(callback, callbackId), cancellationToken);
            },
            cancellationToken);
    }
}
