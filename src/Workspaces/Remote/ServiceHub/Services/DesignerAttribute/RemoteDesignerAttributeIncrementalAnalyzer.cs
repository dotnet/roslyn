﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DesignerAttribute;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteDesignerAttributeIncrementalAnalyzer : AbstractDesignerAttributeIncrementalAnalyzer
    {
        /// <summary>
        /// Channel back to VS to inform it of the designer attributes we discover.
        /// </summary>
        private readonly RemoteCallback<IDesignerAttributeListener> _callback;

        public RemoteDesignerAttributeIncrementalAnalyzer(Workspace workspace, RemoteCallback<IDesignerAttributeListener> callback)
            : base(workspace)
        {
            _callback = callback;
        }

        protected override async ValueTask ReportProjectRemovedAsync(ProjectId projectId, CancellationToken cancellationToken)
        {
            // cancel whenever the analyzer runner cancels or the client disconnects and the request is canceled:
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _callback.ClientDisconnectedSource.Token);

            await _callback.InvokeAsync(
                (callback, cancellationToken) => callback.OnProjectRemovedAsync(projectId, cancellationToken),
                linkedSource.Token).ConfigureAwait(false);
        }

        protected override async ValueTask ReportDesignerAttributeDataAsync(List<DesignerAttributeData> data, CancellationToken cancellationToken)
        {
            // cancel whenever the analyzer runner cancels or the client disconnects and the request is canceled:
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _callback.ClientDisconnectedSource.Token);

            await _callback.InvokeAsync(
               (callback, cancellationToken) => callback.ReportDesignerAttributeDataAsync(data.ToImmutableArray(), cancellationToken),
               linkedSource.Token).ConfigureAwait(false);
        }
    }
}
