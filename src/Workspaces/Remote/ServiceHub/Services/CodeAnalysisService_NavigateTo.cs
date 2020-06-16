// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            PinnedSolutionInfo solutionInfo, DocumentId documentId, string searchPattern, string[] kinds, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                    var document = solution.GetDocument(documentId);
                    var result = await AbstractNavigateToSearchService.SearchDocumentInCurrentProcessAsync(
                        document, searchPattern, kinds.ToImmutableHashSet(), cancellationToken).ConfigureAwait(false);

                    return Convert(result);
                }
            }, cancellationToken);
        }

        public Task<IList<SerializableNavigateToSearchResult>> SearchProjectAsync(
            PinnedSolutionInfo solutionInfo, ProjectId projectId, DocumentId[] priorityDocumentIds, string searchPattern, string[] kinds, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                    var project = solution.GetProject(projectId);
                    var priorityDocuments = priorityDocumentIds.Select(d => solution.GetDocument(d))
                                                               .ToImmutableArray();

                    var result = await AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync(
                        project, priorityDocuments, searchPattern, kinds.ToImmutableHashSet(), cancellationToken).ConfigureAwait(false);

                    return Convert(result);
                }
            }, cancellationToken);
        }

        private static IList<SerializableNavigateToSearchResult> Convert(
            ImmutableArray<INavigateToSearchResult> result)
        {
            return result.SelectAsArray(SerializableNavigateToSearchResult.Dehydrate);
        }
    }
}
