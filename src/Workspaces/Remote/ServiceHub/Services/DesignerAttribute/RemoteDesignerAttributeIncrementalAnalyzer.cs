// Licensed to the .NET Foundation under one or more agreements.
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
        private readonly RemoteEndPoint _endPoint;

        public RemoteDesignerAttributeIncrementalAnalyzer(RemoteEndPoint endPoint)
        {
            _endPoint = endPoint;
        }

        protected override Task ReportProjectRemovedAsync(ProjectId projectId, CancellationToken cancellationToken)
        {
            return _endPoint.InvokeAsync(
                nameof(IDesignerAttributeListener.OnProjectRemovedAsync),
                new object[] { projectId },
                cancellationToken);
        }

        protected override Task ReportDesignerAttributeDataAsync(ImmutableArray<DesignerAttributeData> data, CancellationToken cancellationToken)
        {
            return _endPoint.InvokeAsync(
                nameof(IDesignerAttributeListener.ReportDesignerAttributeDataAsync),
                new object[] { data },
                cancellationToken);
        }
    }
}
