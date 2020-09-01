// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DesignerAttribute;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteDesignerAttributeIncrementalAnalyzer : AbstractDesignerAttributeIncrementalAnalyzer
    {
        /// <summary>
        /// Channel back to VS to inform it of the designer attributes we discover.
        /// </summary>
        private readonly IDesignerAttributeListener _callback;

        public RemoteDesignerAttributeIncrementalAnalyzer(Workspace workspace, IDesignerAttributeListener callback)
            : base(workspace)
        {
            _callback = callback;
        }

        protected override async Task ReportProjectRemovedAsync(ProjectId projectId, CancellationToken cancellationToken)
        {
            try
            {
                await _callback.OnProjectRemovedAsync(projectId, cancellationToken).ConfigureAwait(false);
            }
            catch (ConnectionLostException)
            {
                // The client might have terminated without signalling the cancellation token.
                // Ignore this failure to avoid reporting Watson from the solution crawler.
                // Same effect as if cancellation had been requested.
            }
        }

        protected override async Task ReportDesignerAttributeDataAsync(List<DesignerAttributeData> data, CancellationToken cancellationToken)
        {
            try
            {
                await _callback.ReportDesignerAttributeDataAsync(data.ToImmutableArray(), cancellationToken).ConfigureAwait(false);
            }
            catch (ConnectionLostException)
            {
                // The client might have terminated without signalling the cancellation token.
                // Ignore this failure to avoid reporting Watson from the solution crawler.
                // Same effect as if cancellation had been requested.
            }
        }
    }
}
