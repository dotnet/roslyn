// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
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
        ValueTask DiscoverDesignerAttributesAsync(Checksum solutionChecksum, ProjectId project, DocumentId? priorityDocument, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);

        public interface ICallback
        {
            ValueTask ReportDesignerAttributeDataAsync(RemoteServiceCallbackId callbackId, DesignerAttributeData data, CancellationToken cancellationToken);
        }
    }

    [ExportRemoteServiceCallbackDispatcher(typeof(IRemoteDesignerAttributeDiscoveryService)), Shared]
    internal sealed class RemoteDesignerAttributeDiscoveryCallbackDispatcher : RemoteServiceCallbackDispatcher, IRemoteDesignerAttributeDiscoveryService.ICallback
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteDesignerAttributeDiscoveryCallbackDispatcher()
        {
        }

        private new DesignerAttributeDiscoveryCallback GetCallback(RemoteServiceCallbackId callbackId)
            => (DesignerAttributeDiscoveryCallback)base.GetCallback(callbackId);

        public ValueTask ReportDesignerAttributeDataAsync(RemoteServiceCallbackId callbackId, DesignerAttributeData data, CancellationToken cancellationToken)
            => GetCallback(callbackId).ReportDesignerAttributeDataAsync(data, cancellationToken);
    }

    internal sealed class DesignerAttributeDiscoveryCallback
    {
        private readonly Channel<DesignerAttributeData> _channel;

        public DesignerAttributeDiscoveryCallback(
            Channel<DesignerAttributeData> channel)
        {
            _channel = channel;
        }

        public async ValueTask ReportDesignerAttributeDataAsync(DesignerAttributeData data, CancellationToken cancellationToken)
        {
            try
            {
                await _channel.Writer.WriteAsync(data, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex))
            {
            }
        }
    }
}
