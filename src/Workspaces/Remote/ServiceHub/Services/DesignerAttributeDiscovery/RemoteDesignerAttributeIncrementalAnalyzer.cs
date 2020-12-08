// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DesignerAttribute;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed partial class RemoteDesignerAttributeIncrementalAnalyzer : AbstractDesignerAttributeIncrementalAnalyzer
    {
        /// <summary>
        /// Channel back to VS to inform it of the designer attributes we discover.
        /// </summary>
        private readonly RemoteCallback<IRemoteDesignerAttributeDiscoveryService.ICallback> _callback;
        private readonly RemoteServiceCallbackId _callbackId;

        public RemoteDesignerAttributeIncrementalAnalyzer(RemoteCallback<IRemoteDesignerAttributeDiscoveryService.ICallback> callback, RemoteServiceCallbackId callbackId)
        {
            _callback = callback;
            _callbackId = callbackId;
        }

        protected override async ValueTask ReportProjectRemovedAsync(ProjectId projectId, CancellationToken cancellationToken)
        {
            await _callback.InvokeAsync(
                (callback, cancellationToken) => callback.OnProjectRemovedAsync(_callbackId, projectId, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        protected override async ValueTask ReportDesignerAttributeDataAsync(ImmutableArray<DesignerAttributeData> data, CancellationToken cancellationToken)
        {
            await _callback.InvokeAsync(
               (callback, cancellationToken) => callback.ReportDesignerAttributeDataAsync(_callbackId, data, cancellationToken),
               cancellationToken).ConfigureAwait(false);
        }
    }
}
