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
        public async Task<IList<SerializableNavigateToSearchResult>> SearchDocumentAsync(
            DocumentId documentId, string searchPattern, CancellationToken cancellationToken)
        {
            return await RunServiceAsync(async token =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(token).ConfigureAwait(false);

                    var project = solution.GetDocument(documentId);
                    var result = await AbstractNavigateToSearchService.SearchDocumentInCurrentProcessAsync(
                        project, searchPattern, token).ConfigureAwait(false);

                    return Convert(result);
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IList<SerializableNavigateToSearchResult>> SearchProjectAsync(
            ProjectId projectId, string searchPattern, CancellationToken cancellationToken)
        {
            return await RunServiceAsync(async token =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(token).ConfigureAwait(false);

                    var project = solution.GetProject(projectId);
                    var result = await AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync(
                        project, searchPattern, token).ConfigureAwait(false);

                    return Convert(result);
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private ImmutableArray<SerializableNavigateToSearchResult> Convert(
            ImmutableArray<INavigateToSearchResult> result)
        {
            return result.SelectAsArray(SerializableNavigateToSearchResult.Dehydrate);
        }
    }
}
