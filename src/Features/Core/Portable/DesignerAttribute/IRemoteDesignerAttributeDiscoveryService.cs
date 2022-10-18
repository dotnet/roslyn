// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.DesignerAttribute
{
    /// <summary>
    /// Interface to allow host (VS) to inform the OOP service to start incrementally analyzing and
    /// reporting results back to the host.
    /// </summary>
    internal interface IRemoteDesignerAttributeDiscoveryService
    {
        IAsyncEnumerable<DesignerAttributeData> DiscoverDesignerAttributesAsync(Checksum solutionChecksum, ProjectId project, DocumentId? priorityDocument, CancellationToken cancellationToken);
    }
}
