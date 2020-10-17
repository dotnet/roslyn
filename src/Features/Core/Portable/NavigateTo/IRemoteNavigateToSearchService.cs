﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal interface IRemoteNavigateToSearchService
    {
        ValueTask<ImmutableArray<SerializableNavigateToSearchResult>> SearchDocumentAsync(
            PinnedSolutionInfo solutionInfo, DocumentId documentId, string searchPattern, ImmutableArray<string> kinds, CancellationToken cancellationToken);

        ValueTask<ImmutableArray<SerializableNavigateToSearchResult>> SearchProjectAsync(
            PinnedSolutionInfo solutionInfo, ProjectId projectId, ImmutableArray<DocumentId> priorityDocumentIds, string searchPattern, ImmutableArray<string> kinds, CancellationToken cancellationToken);
    }
}
