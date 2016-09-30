// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.NavigateTo;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService : IRemoteNavigateToSearchService
    {
        public async Task<SerializableNavigateToSearchResult[]> SearchDocumentAsync(
             SerializableDocumentId documentId, string searchPattern, byte[] solutionChecksum)
        {
            var solution = await RoslynServices.SolutionService.GetSolutionAsync(
                new Checksum(solutionChecksum), CancellationToken).ConfigureAwait(false);

            var project = solution.GetDocument(documentId.Rehydrate());
            var result = await AbstractNavigateToSearchService.SearchDocumentInCurrentProcessAsync(
                project, searchPattern, CancellationToken).ConfigureAwait(false);

            return Convert(result);
        }

        public async Task<SerializableNavigateToSearchResult[]> SearchProjectAsync(
             SerializableProjectId projectId, string searchPattern, byte[] solutionChecksum)
        {
            var solution = await RoslynServices.SolutionService.GetSolutionAsync(
                new Checksum(solutionChecksum), CancellationToken).ConfigureAwait(false);

            var project = solution.GetProject(projectId.Rehydrate());
            var result = await AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync(
                project, searchPattern, CancellationToken).ConfigureAwait(false);

            return Convert(result);
        }

        private SerializableNavigateToSearchResult[] Convert(
            ImmutableArray<INavigateToSearchResult> result)
        {
            return result.Select(SerializableNavigateToSearchResult.Dehydrate).ToArray();
        }
    }
}