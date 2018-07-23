// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal interface IRemoteNavigateToSearchService
    {
        Task<IList<SerializableNavigateToSearchResult>> SearchDocumentAsync(DocumentId documentId, string searchPattern, string[] kinds, CancellationToken cancellationToken);
        Task<IList<SerializableNavigateToSearchResult>> SearchProjectAsync(ProjectId projectId, DocumentId[] priorityDocumentIds, string searchPattern, string[] kinds, CancellationToken cancellationToken);
    }
}
