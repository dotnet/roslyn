// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.DesignerAttribute;

internal partial interface IDesignerAttributeDiscoveryService : IWorkspaceService
{
    public interface ICallback
    {
        ValueTask ReportDesignerAttributeDataAsync(ImmutableArray<DesignerAttributeData> data, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Called to process the entire solution.  May take a while.
    /// </summary>
    ValueTask ProcessSolutionAsync(Solution solution, ICallback callback, CancellationToken cancellationToken);

    /// <summary>
    /// Called to process a single document.  Should be used to quickly process the document a user is editing.
    /// </summary>
    ValueTask ProcessPriorityDocumentAsync(Solution solution, DocumentId priorityDocumentId, ICallback callback, CancellationToken cancellationToken);
}
