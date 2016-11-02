// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal interface IRemoteNavigateToSearchService
    {
        Task<SerializableNavigateToSearchResult[]> SearchDocumentAsync(
            SerializableDocumentId documentId, string searchPattern, byte[] solutionChecksum);

        Task<SerializableNavigateToSearchResult[]> SearchProjectAsync(
             SerializableProjectId projectId, string searchPattern, byte[] solutionChecksum);
    }
}
