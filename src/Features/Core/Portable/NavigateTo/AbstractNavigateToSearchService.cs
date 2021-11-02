// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService : INavigateToSearchService
    {
        public IImmutableSet<string> KindsProvided { get; } = ImmutableHashSet.Create(
            NavigateToItemKind.Class,
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

        public async Task SearchDocumentAsync(Document document, string searchPattern, IImmutableSet<string> kinds, Func<INavigateToSearchResult, Task> onResultFound, bool isFullyLoaded, CancellationToken cancellationToken)
        {
            if (isFullyLoaded)
            {
                await SearchFullyLoadedDocumentAsync(document, searchPattern, kinds, onResultFound, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await SearchCachedDocumentsAsync(ImmutableArray.Create(document), ImmutableArray<Document>.Empty, searchPattern, kinds, onResultFound, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task SearchFullyLoadedDocumentAsync(
            Document document,
            string searchPattern,
            IImmutableSet<string> kinds,
            Func<INavigateToSearchResult, Task> onResultFound,
            CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var onItemFound = GetOnItemFoundCallback(solution, onResultFound, cancellationToken);
            var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var callback = new NavigateToSearchServiceCallback(onItemFound);
                // Don't need to sync the full solution when searching a particular project.
                await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                    document.Project,
                    (service, solutionInfo, callbackId, cancellationToken) =>
                    service.SearchFullyLoadedDocumentAsync(solutionInfo, document.Id, searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                    callback, cancellationToken).ConfigureAwait(false);

                return;
            }

            await SearchFullyLoadedDocumentInCurrentProcessAsync(
                document, searchPattern, kinds, onItemFound, cancellationToken).ConfigureAwait(false);
        }

        public async Task SearchProjectAsync(Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, Func<INavigateToSearchResult, Task> onResultFound, bool isFullyLoaded, CancellationToken cancellationToken)
        {
            if (isFullyLoaded)
            {
                await SearchFullyLoadedProjectAsync(project, priorityDocuments, searchPattern, kinds, onResultFound, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await SearchCachedDocumentsAsync(project.Documents.ToImmutableArray(), priorityDocuments, searchPattern, kinds, onResultFound, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task SearchFullyLoadedProjectAsync(
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

                // don't need to sync the entire solution when searching a particular project.
                await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                    project,
                    (service, solutionInfo, callbackId, cancellationToken) =>
                        service.SearchFullyLoadedProjectAsync(solutionInfo, project.Id, priorityDocumentIds, searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                    callback, cancellationToken).ConfigureAwait(false);

                return;
            }

            await SearchFullyLoadedProjectInCurrentProcessAsync(
                project, priorityDocuments, searchPattern, kinds, onItemFound, cancellationToken).ConfigureAwait(false);
        }

        private static async Task SearchCachedDocumentsAsync(
            ImmutableArray<Document> documents,
            ImmutableArray<Document> priorityDocuments,
            string searchPattern,
            IImmutableSet<string> kinds,
            Func<INavigateToSearchResult, Task> onResultFound,
            CancellationToken cancellationToken)
        {
            var document = documents.FirstOrDefault() ?? priorityDocuments.FirstOrDefault();
            if (document == null)
                return;

            var project = document.Project;
            var solution = project.Solution;
            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            var onItemFound = GetOnItemFoundCallback(solution, onResultFound, cancellationToken);
            var database = solution.Options.GetPersistentStorageDatabase();

            var documentKeys = project.Documents.Select(d => DocumentKey.ToDocumentKey(d)).ToImmutableArray();
            var priorityDocumentKeys = priorityDocuments.SelectAsArray(d => DocumentKey.ToDocumentKey(d));
            if (client != null)
            {
                var callback = new NavigateToSearchServiceCallback(onItemFound);
                await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                    (service, callbackId, cancellationToken) =>
                        service.SearchCachedDocumentsAsync(documentKeys, priorityDocumentKeys, database, searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                    callback, cancellationToken).ConfigureAwait(false);

                return;
            }

            var storageService = solution.Workspace.Services.GetPersistentStorageService(database);
            await SearchCachedDocumentsInCurrentProcessAsync(
                storageService, documentKeys, priorityDocumentKeys, searchPattern, kinds, onItemFound, cancellationToken).ConfigureAwait(false);
        }
    }
}
