// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.DesignerAttribute
{
    internal interface IDesignerAttributeDiscoveryService : IWorkspaceService
    {
        IAsyncEnumerable<DesignerAttributeData> ProcessProjectAsync(Project project, DocumentId? priorityDocumentId, CancellationToken cancellationToken);
    }
}
