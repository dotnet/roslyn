// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService : IRemoteNavigateToSearchService
    {
        public async Task<IReadOnlyList<SerializableNavigateToSearchResult>> SearchDocumentAsync(
            DocumentId documentId, string searchPattern, CancellationToken cancellationToken)
        {
            using (UserOperationBooster.Boost())
            {
                var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);

                var project = solution.GetDocument(documentId);
                var result = await AbstractNavigateToSearchService.SearchDocumentInCurrentProcessAsync(
                    project, searchPattern, cancellationToken).ConfigureAwait(false);

                return Convert(result);
            }
        }

        public async Task<IReadOnlyList<SerializableNavigateToSearchResult>> SearchProjectAsync(
            ProjectId projectId, string searchPattern, CancellationToken cancellationToken)
        {
            using (UserOperationBooster.Boost())
            {
                var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);

                var project = solution.GetProject(projectId);
                var result = await AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync(
                    project, searchPattern, cancellationToken).ConfigureAwait(false);

                return Convert(result);
            }
        }

        private ImmutableArray<SerializableNavigateToSearchResult> Convert(
            ImmutableArray<INavigateToSearchResult> result)
        {
            return result.SelectAsArray(SerializableNavigateToSearchResult.Dehydrate);
        }
    }
}
