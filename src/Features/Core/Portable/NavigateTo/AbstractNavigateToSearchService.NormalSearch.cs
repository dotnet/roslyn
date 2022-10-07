// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService
    {
        public async Task SearchDocumentAsync(
            Document document,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            Func<INavigateToSearchResult, Task> onResultFound,
            CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var onItemFound = GetOnItemFoundCallback(solution, activeDocument, onResultFound, cancellationToken);

            var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                // Don't need to sync the full solution when searching a particular project.
                var result = client.TryInvokeStreamAsync<IRemoteNavigateToSearchService, RoslynNavigateToItem>(
                    document.Project,
                    (service, solutionInfo, cancellationToken) =>
                        service.SearchDocumentAsync(solutionInfo, document.Id, searchPattern, kinds.ToImmutableArray(), cancellationToken),
                    cancellationToken);

                await foreach (var item in result.WithCancellation(cancellationToken))
                {
                    if (!item.HasValue)
                        return;

                    await onItemFound(item.Value).ConfigureAwait(false);
                }

                return;
            }

            await SearchDocumentInCurrentProcessAsync(document, searchPattern, kinds, onItemFound, cancellationToken).ConfigureAwait(false);
        }

        public static Task SearchDocumentInCurrentProcessAsync(Document document, string searchPattern, IImmutableSet<string> kinds, Func<RoslynNavigateToItem, Task> onItemFound, CancellationToken cancellationToken)
        {
            return SearchProjectInCurrentProcessAsync(
                document.Project, priorityDocuments: ImmutableArray<Document>.Empty, document,
                searchPattern, kinds, onItemFound, cancellationToken);
        }

        public async Task SearchProjectAsync(
            Project project,
            ImmutableArray<Document> priorityDocuments,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            Func<INavigateToSearchResult, Task> onResultFound,
            CancellationToken cancellationToken)
        {
            var solution = project.Solution;
            var onItemFound = GetOnItemFoundCallback(solution, activeDocument, onResultFound, cancellationToken);

            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var priorityDocumentIds = priorityDocuments.SelectAsArray(d => d.Id);

                var result = client.TryInvokeStreamAsync<IRemoteNavigateToSearchService, RoslynNavigateToItem>(
                    solution,
                    (service, solutionInfo, cancellationToken) =>
                        service.SearchProjectAsync(solutionInfo, project.Id, priorityDocumentIds, searchPattern, kinds.ToImmutableArray(), cancellationToken),
                    cancellationToken);

                await foreach (var item in result.WithCancellation(cancellationToken))
                {
                    if (!item.HasValue)
                        return;

                    await onItemFound(item.Value).ConfigureAwait(false);
                }

                return;
            }

            await SearchProjectInCurrentProcessAsync(project, priorityDocuments, searchPattern, kinds, onItemFound, cancellationToken).ConfigureAwait(false);
        }

        public static Task SearchProjectInCurrentProcessAsync(Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, Func<RoslynNavigateToItem, Task> onItemFound, CancellationToken cancellationToken)
        {
            return SearchProjectInCurrentProcessAsync(
                project, priorityDocuments, searchDocument: null,
                pattern: searchPattern, kinds, onItemFound, cancellationToken);
        }
    }
}
