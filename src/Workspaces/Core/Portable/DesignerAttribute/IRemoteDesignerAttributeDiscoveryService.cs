// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.DesignerAttribute
{
    /// <summary>
    /// Interface to allow host (VS) to inform the OOP service to start incrementally analyzing and
    /// reporting results back to the host.
    /// </summary>
    internal interface IRemoteDesignerAttributeDiscoveryService
    {
        internal interface ICallback
        {
            ValueTask OnProjectRemovedAsync(RemoteServiceCallbackId callbackId, ProjectId projectId, CancellationToken cancellationToken);
            ValueTask ReportDesignerAttributeDataAsync(RemoteServiceCallbackId callbackId, ImmutableArray<DesignerAttributeData> data, CancellationToken cancellationToken);
        }

        ValueTask StartScanningForDesignerAttributesAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellation);
    }

    [ExportRemoteServiceCallbackDispatcher(typeof(IRemoteDesignerAttributeDiscoveryService)), Shared]
    internal sealed class RemoteDesignerAttributeDiscoveryCallbackDispatcher : RemoteServiceCallbackDispatcher, IRemoteDesignerAttributeDiscoveryService.ICallback
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteDesignerAttributeDiscoveryCallbackDispatcher()
        {
        }

        private IDesignerAttributeListener GetLogService(RemoteServiceCallbackId callbackId)
            => (IDesignerAttributeListener)GetCallback(callbackId);

        public ValueTask OnProjectRemovedAsync(RemoteServiceCallbackId callbackId, ProjectId projectId, CancellationToken cancellationToken)
            => GetLogService(callbackId).OnProjectRemovedAsync(projectId, cancellationToken);

        public ValueTask ReportDesignerAttributeDataAsync(RemoteServiceCallbackId callbackId, ImmutableArray<DesignerAttributeData> data, CancellationToken cancellationToken)
            => GetLogService(callbackId).ReportDesignerAttributeDataAsync(data, cancellationToken);
    }
}
