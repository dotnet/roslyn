// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService
    {
        public static Task<ImmutableArray<INavigateToSearchResult>> SearchProjectInCurrentProcessAsync(
            Project project, string searchPattern, CancellationToken cancellationToken)
        {
            return SearchInCurrentProcessAsync(
                project, searchDocument: null, pattern: searchPattern, cancellationToken: cancellationToken);
        }

        public static Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentInCurrentProcessAsync(
            Document document, string searchPattern, CancellationToken cancellationToken)
        {
            return SearchInCurrentProcessAsync(
                document.Project, document, searchPattern, cancellationToken);
        }

        private static async Task<ImmutableArray<INavigateToSearchResult>> SearchInCurrentProcessAsync(
            Project project, Document searchDocument, string pattern, CancellationToken cancellationToken)
        {
            using (var searcher = new NavigateToProjectSearcher(project, searchDocument, pattern, cancellationToken))
            {
                return await searcher.DoAsync().ConfigureAwait(false);
            }
        }
    }
}