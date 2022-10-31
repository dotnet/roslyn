// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService
    {
        public async IAsyncEnumerable<INavigateToSearchResult> SearchDocumentAsync(
            Document document,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;

            var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                // Don't need to sync the full solution when searching a particular project.
                var result = client.TryInvokeStreamAsync<IRemoteNavigateToSearchService, RoslynNavigateToItem>(
                    document.Project,
                    (service, solutionInfo, cancellationToken) =>
                        service.SearchDocumentAsync(solutionInfo, document.Id, searchPattern, kinds.ToImmutableArray(), cancellationToken),
                    cancellationToken);

                await foreach (var item in ConvertItemsAsync(solution, activeDocument, result, cancellationToken).ConfigureAwait(false))
                    yield return item;
            }
            else
            {
                var result = SearchDocumentInCurrentProcessAsync(document, searchPattern, kinds, cancellationToken);

                await foreach (var item in ConvertItemsAsync(solution, activeDocument, result, cancellationToken).ConfigureAwait(false))
                    yield return item;
            }
        }

        public static IAsyncEnumerable<RoslynNavigateToItem> SearchDocumentInCurrentProcessAsync(
            Document document, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
        {
            return SearchProjectInCurrentProcessAsync(
                document.Project, priorityDocuments: ImmutableArray<Document>.Empty, document,
                searchPattern, kinds, cancellationToken);
        }

        public async IAsyncEnumerable<INavigateToSearchResult> SearchProjectAsync(
            Project project,
            ImmutableArray<Document> priorityDocuments,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var solution = project.Solution;

            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var priorityDocumentIds = priorityDocuments.SelectAsArray(d => d.Id);

                var result = client.TryInvokeStreamAsync<IRemoteNavigateToSearchService, RoslynNavigateToItem>(
                    solution,
                    (service, solutionInfo, cancellationToken) =>
                        service.SearchProjectAsync(solutionInfo, project.Id, priorityDocumentIds, searchPattern, kinds.ToImmutableArray(), cancellationToken),
                    cancellationToken);

                await foreach (var item in ConvertItemsAsync(solution, activeDocument, result, cancellationToken).ConfigureAwait(false))
                    yield return item;
            }
            else
            {
                var result = SearchProjectInCurrentProcessAsync(project, priorityDocuments, searchPattern, kinds, cancellationToken);

                await foreach (var item in ConvertItemsAsync(solution, activeDocument, result, cancellationToken).ConfigureAwait(false))
                    yield return item;
            }
        }

        public static IAsyncEnumerable<RoslynNavigateToItem> SearchProjectInCurrentProcessAsync(
            Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
        {
            return SearchProjectInCurrentProcessAsync(
                project, priorityDocuments, searchDocument: null,
                pattern: searchPattern, kinds, cancellationToken);
        }
    }
}
