// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal interface IRemoteNavigateToSearchService
    {
        Task<IList<SerializableNavigateToSearchResult>> SearchDocumentAsync(
            PinnedSolutionInfo solutionInfo, DocumentId documentId, string searchPattern, string[] kinds, CancellationToken cancellationToken);

        Task<IList<SerializableNavigateToSearchResult>> SearchProjectAsync(
            PinnedSolutionInfo solutionInfo, ProjectId projectId, DocumentId[] priorityDocumentIds, string searchPattern, string[] kinds, CancellationToken cancellationToken);
    }
}
