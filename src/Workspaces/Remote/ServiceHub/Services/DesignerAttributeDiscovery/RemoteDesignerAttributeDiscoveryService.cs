// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

        private Func<DesignerAttributeData, ValueTask> GetCallback(
            RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
        {
            return data => _callback.InvokeAsync((callback, cancellationToken) =>
                callback.ReportDesignerAttributeDataAsync(callbackId, data, cancellationToken),
                cancellationToken);
        }

        private async ValueTask PushToCallbackAsync(
            RemoteServiceCallbackId callbackId,
            IAsyncEnumerable<DesignerAttributeData> items,
            CancellationToken cancellationToken)
        {
            var callback = GetCallback(callbackId, cancellationToken);
            await foreach (var item in items.ConfigureAwait(false))
                await callback(item).ConfigureAwait(false);
        }

        public ValueTask DiscoverDesignerAttributesAsync(
            Checksum solutionChecksum,
            ProjectId projectId,
            DocumentId? priorityDocument,
            RemoteServiceCallbackId callbackId,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(
                solutionChecksum,
                solution =>
                {
                    var project = solution.GetRequiredProject(projectId);
                    var service = solution.Services.GetRequiredService<IDesignerAttributeDiscoveryService>();
                    return PushToCallbackAsync(
                        callbackId,
                        service.ProcessProjectAsync(project, priorityDocument, cancellationToken),
                        cancellationToken);
                },
                cancellationToken);
        }
    }
}
