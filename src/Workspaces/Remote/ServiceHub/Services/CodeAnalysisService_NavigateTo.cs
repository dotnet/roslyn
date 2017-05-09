// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService : IRemoteNavigateToSearchService
    {
        public async Task<SerializableNavigateToSearchResult[]> SearchDocumentAsync(
            DocumentId documentId, string searchPattern)
        {
            using (UserOperationBooster.Boost())
            {
                var solution = await GetSolutionAsync().ConfigureAwait(false);

                var project = solution.GetDocument(documentId);
                var result = await AbstractNavigateToSearchService.SearchDocumentInCurrentProcessAsync(
                    project, searchPattern, CancellationToken).ConfigureAwait(false);

                return Convert(result);
            }
        }

        public async Task<SerializableNavigateToSearchResult[]> SearchProjectAsync(
            ProjectId projectId, string searchPattern)
        {
            using (UserOperationBooster.Boost())
            {
                var start = DateTime.Now;
                var solution = await GetSolutionAsync().ConfigureAwait(false);
                var getSolutionEnd = DateTime.Now;

                var project = solution.GetProject(projectId);
                var result = await AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync(
                    project, searchPattern, CancellationToken).ConfigureAwait(false);

                var resultEnd = DateTime.Now;

                var converted = Convert(result);

                var text = "Searching: " + projectId.DebugName +
                    "\r\nGet-Solution: " + (getSolutionEnd - start) +
                    "\r\nSearch: " + (resultEnd - getSolutionEnd) +
                    "\r\n";
                AbstractNavigateToSearchService.Log(text);

                return converted;
            }
        }

        private SerializableNavigateToSearchResult[] Convert(
            ImmutableArray<INavigateToSearchResult> result)
        {
            return result.Select(SerializableNavigateToSearchResult.Dehydrate).ToArray();
        }
    }
}