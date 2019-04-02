// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService : IRemoteNavigateToSearchService
    {
        public Task<IList<SerializableNavigateToSearchResult>> SearchDocumentAsync(
            DocumentId documentId, string searchPattern, string[] kinds, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);

                    var project = solution.GetDocument(documentId);
                    var result = await AbstractNavigateToSearchService.SearchDocumentInCurrentProcessAsync(
                        project, searchPattern, kinds.ToImmutableHashSet(), cancellationToken).ConfigureAwait(false);

                    return Convert(result);
                }
            }, cancellationToken);
        }

        public Task<IList<SerializableNavigateToSearchResult>> SearchProjectAsync(
            ProjectId projectId, DocumentId[] priorityDocumentIds, string searchPattern, string[] kinds, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);

                    var project = solution.GetProject(projectId);
                    var priorityDocuments = priorityDocumentIds.Select(d => solution.GetDocument(d))
                                                               .ToImmutableArray();

                    var result = await AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync(
                        project, priorityDocuments, searchPattern, kinds.ToImmutableHashSet(), cancellationToken).ConfigureAwait(false);

                    return Convert(result);
                }
            }, cancellationToken);
        }

        private IList<SerializableNavigateToSearchResult> Convert(
            ImmutableArray<INavigateToSearchResult> result)
        {
            return result.SelectAsArray(SerializableNavigateToSearchResult.Dehydrate);
        }
    }
}
