// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteNavigateToSearchService : BrokeredServiceBase, IRemoteNavigateToSearchService
    {
        internal sealed class Factory : FactoryBase<IRemoteNavigateToSearchService, IRemoteNavigateToSearchService.ICallback>
        {
            protected override IRemoteNavigateToSearchService CreateService(
                in ServiceConstructionArguments arguments, RemoteCallback<IRemoteNavigateToSearchService.ICallback> callback)
                => new RemoteNavigateToSearchService(arguments, callback);
        }

        private readonly RemoteCallback<IRemoteNavigateToSearchService.ICallback> _callback;

        public RemoteNavigateToSearchService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteNavigateToSearchService.ICallback> callback)
            : base(arguments)
        {
            _callback = callback;
        }

        private Func<RoslynNavigateToItem, Task> GetCallback(
            RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
        {
            return async i => await _callback.InvokeAsync((callback, c) =>
                callback.OnResultFoundAsync(callbackId, i),
                cancellationToken).ConfigureAwait(false);
        }

        public ValueTask HydrateAsync(PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                // All we need to do is request the solution.  This will ensure that all assets are
                // pulled over from the host side to the remote side.  Once this completes, the next
                // call to SearchFullyLoadedDocumentAsync or SearchFullyLoadedProjectAsync will be
                // quick as very little will need to by sync'ed over.
                await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask SearchFullyLoadedDocumentAsync(
            PinnedSolutionInfo solutionInfo,
            DocumentId documentId,
            string searchPattern,
            ImmutableArray<string> kinds,
            RemoteServiceCallbackId callbackId,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                var document = solution.GetRequiredDocument(documentId);
                var callback = GetCallback(callbackId, cancellationToken);

                await AbstractNavigateToSearchService.SearchFullyLoadedDocumentInCurrentProcessAsync(
                    document, searchPattern, kinds.ToImmutableHashSet(), callback, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask SearchFullyLoadedProjectAsync(
            PinnedSolutionInfo solutionInfo,
            ProjectId projectId,
            ImmutableArray<DocumentId> priorityDocumentIds,
            string searchPattern,
            ImmutableArray<string> kinds,
            RemoteServiceCallbackId callbackId,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                var project = solution.GetRequiredProject(projectId);
                var callback = GetCallback(callbackId, cancellationToken);

                var priorityDocuments = priorityDocumentIds.SelectAsArray(d => solution.GetRequiredDocument(d));

                await AbstractNavigateToSearchService.SearchFullyLoadedProjectInCurrentProcessAsync(
                    project, priorityDocuments, searchPattern, kinds.ToImmutableHashSet(), callback, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask SearchCachedDocumentsAsync(ImmutableArray<DocumentKey> documentKeys, ImmutableArray<DocumentKey> priorityDocumentKeys, StorageDatabase database, string searchPattern, ImmutableArray<string> kinds, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                // Intentionally do not call GetSolutionAsync here.  We do not want the cost of
                // synchronizing the solution over to the remote side.  Instead, we just directly
                // check whatever cached data we have from the previous vs session.
                var callback = GetCallback(callbackId, cancellationToken);
                var workspace = GetWorkspace();

                // We translated a call from the host over to the OOP side.  We need to look up
                // the data in OOP's storage system, not the host's storage system.
                documentKeys = documentKeys.SelectAsArray(d => d.WithWorkspaceKind(workspace.Kind!));
                priorityDocumentKeys = priorityDocumentKeys.SelectAsArray(d => d.WithWorkspaceKind(workspace.Kind!));

                await AbstractNavigateToSearchService.SearchCachedDocumentsInCurrentProcessAsync(
                    workspace.Services, documentKeys, priorityDocumentKeys, database, searchPattern, kinds.ToImmutableHashSet(), callback, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }
    }
}
