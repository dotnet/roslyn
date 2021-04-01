// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService : INavigateToSearchService
    {
        public IImmutableSet<string> KindsProvided { get; } = ImmutableHashSet.Create(
            NavigateToItemKind.Class,
            NavigateToItemKind.Record,
            NavigateToItemKind.Constant,
            NavigateToItemKind.Delegate,
            NavigateToItemKind.Enum,
            NavigateToItemKind.EnumItem,
            NavigateToItemKind.Event,
            NavigateToItemKind.Field,
            NavigateToItemKind.Interface,
            NavigateToItemKind.Method,
            NavigateToItemKind.Module,
            NavigateToItemKind.Property,
            NavigateToItemKind.Structure);

        public bool CanFilter => true;

        private static Func<RoslynNavigateToItem, Task> GetOnItemFoundCallback(
            Solution solution, Func<INavigateToSearchResult, Task> onResultFound, CancellationToken cancellationToken)
        {
            return async item =>
            {
                var result = await item.TryCreateSearchResultAsync(solution, cancellationToken).ConfigureAwait(false);
                if (result != null)
                    await onResultFound(result).ConfigureAwait(false);
            };
        }

        public async Task SearchDocumentAsync(
            Document document,
            string searchPattern,
            IImmutableSet<string> kinds,
            Func<INavigateToSearchResult, Task> onResultFound,
            bool isFullyLoaded,
            CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var onItemFound = GetOnItemFoundCallback(solution, onResultFound, cancellationToken);
            var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var callback = new NavigateToSearchServiceCallback(onItemFound);
                await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                    solution,
                    (service, solutionInfo, callbackId, cancellationToken) =>
                    service.SearchDocumentAsync(solutionInfo, document.Id, searchPattern, kinds.ToImmutableArray(), callbackId, isFullyLoaded, cancellationToken),
                    callback, cancellationToken).ConfigureAwait(false);

                return;
            }

            await SearchDocumentInCurrentProcessAsync(
                document, searchPattern, kinds, onItemFound, isFullyLoaded, cancellationToken).ConfigureAwait(false);
        }

        public Task SearchProjectAsync(Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, Func<INavigateToSearchResult, Task> onResultFound, bool isFullyLoaded, CancellationToken cancellationToken)
        {
            return isFullyLoaded
                ? SearchFullyLoadedProjectAsync(project, priorityDocuments, searchPattern, kinds, onResultFound, cancellationToken)
                : SearchCachedProjectAsync(project, priorityDocuments, searchPattern, kinds, onResultFound, cancellationToken);
        }

        private async Task SearchFullyLoadedProjectAsync(
            Project project,
            ImmutableArray<Document> priorityDocuments,
            string searchPattern,
            IImmutableSet<string> kinds,
            Func<INavigateToSearchResult, Task> onResultFound,
            CancellationToken cancellationToken)
        {
            var solution = project.Solution;
            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            var onItemFound = GetOnItemFoundCallback(solution, onResultFound, cancellationToken);

            if (client != null)
            {
                var priorityDocumentIds = priorityDocuments.SelectAsArray(d => d.Id);
                var callback = new NavigateToSearchServiceCallback(onItemFound);
                await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                    solution,
                    (service, solutionInfo, callbackId, cancellationToken) =>
                        service.SearchProjectAsync(solutionInfo, project.Id, priorityDocumentIds, searchPattern, kinds.ToImmutableArray(), callbackId, isFullyLoaded, cancellationToken),
                    callback, cancellationToken).ConfigureAwait(false);

                return;
            }

            await SearchFullyLoadedProjectInCurrentProcessAsync(
                project, priorityDocuments, searchPattern, kinds, onItemFound, cancellationToken).ConfigureAwait(false);
        }

        private async Task SearchCachedProjectAsync(
            Project project,
            ImmutableArray<Document> priorityDocuments,
            string searchPattern,
            IImmutableSet<string> kinds,
            Func<INavigateToSearchResult, Task> onResultFound,
            CancellationToken cancellationToken)
        {
            var solution = project.Solution;
            var solutionKey = SolutionKey.ToSolutionKey(solution);
            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            var onItemFound = GetOnItemFoundCallback(solution, onResultFound, cancellationToken);

            var documentIds = project.DocumentIds.ToImmutableArray();
            var priorityDocumentIds = priorityDocuments.SelectAsArray(d => d.Id);
            if (client != null)
            {
                var callback = new NavigateToSearchServiceCallback(onItemFound);
                await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                    (service, callbackId, cancellationToken) =>
                        service.SearchCachedProjectAsync(solutionKey, documentIds, priorityDocumentIds, searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                    callback, cancellationToken).ConfigureAwait(false);

                return;
            }

            await SearchCachedProjectInCurrentProcessAsync(
                workspace, solutionKey, documentIds, priorityDocumentIds, searchPattern, kinds, onItemFound, cancellationToken).ConfigureAwait(false);
        }
    }
}
