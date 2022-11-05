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
using Microsoft.VisualBasic;
using Roslyn.Utilities;

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

        private Func<RoslynNavigateToItem, ValueTask> GetCallback(
            RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
        {
            return i => _callback.InvokeAsync((callback, cancellationToken) =>
                callback.OnResultFoundAsync(callbackId, i, cancellationToken),
                cancellationToken);
        }

        public ValueTask HydrateAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            // All we need to do is request the solution.  This will ensure that all assets are
            // pulled over from the host side to the remote side.  Once this completes, the next
            // call to SearchFullyLoadedDocumentAsync or SearchFullyLoadedProjectAsync will be
            // quick as very little will need to by sync'ed over.
            return RunServiceAsync(solutionChecksum, solution => ValueTaskFactory.CompletedTask, cancellationToken);
        }

        public ValueTask SearchDocumentAsync(
            Checksum solutionChecksum,
            DocumentId documentId,
            string searchPattern,
            ImmutableArray<string> kinds,
            RemoteServiceCallbackId callbackId,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var document = solution.GetRequiredDocument(documentId);
                var callback = GetCallback(callbackId, cancellationToken);

                await foreach (var item in AbstractNavigateToSearchService.SearchDocumentInCurrentProcessAsync(
                    document, searchPattern, kinds.ToImmutableHashSet(), cancellationToken).ConfigureAwait(false))
                {
                    await callback(item).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public ValueTask SearchProjectAsync(
            Checksum solutionChecksum,
            ProjectId projectId,
            ImmutableArray<DocumentId> priorityDocumentIds,
            string searchPattern,
            ImmutableArray<string> kinds,
            RemoteServiceCallbackId callbackId,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var project = solution.GetRequiredProject(projectId);
                var callback = GetCallback(callbackId, cancellationToken);

                var priorityDocuments = priorityDocumentIds.SelectAsArray(d => solution.GetRequiredDocument(d));

                await foreach (var item in AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync(
                    project, priorityDocuments, searchPattern, kinds.ToImmutableHashSet(), cancellationToken).ConfigureAwait(false))
                {
                    await callback(item).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public ValueTask SearchGeneratedDocumentsAsync(
            Checksum solutionChecksum,
            ProjectId projectId,
            string searchPattern,
            ImmutableArray<string> kinds,
            RemoteServiceCallbackId callbackId,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var project = solution.GetRequiredProject(projectId);
                var callback = GetCallback(callbackId, cancellationToken);

                await foreach (var item in AbstractNavigateToSearchService.SearchGeneratedDocumentsInCurrentProcessAsync(
                    project, searchPattern, kinds.ToImmutableHashSet(), cancellationToken).ConfigureAwait(false))
                {
                    await callback(item).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public ValueTask SearchCachedDocumentsAsync(ImmutableArray<DocumentKey> documentKeys, ImmutableArray<DocumentKey> priorityDocumentKeys, string searchPattern, ImmutableArray<string> kinds, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                // Intentionally do not call GetSolutionAsync here.  We do not want the cost of
                // synchronizing the solution over to the remote side.  Instead, we just directly
                // check whatever cached data we have from the previous vs session.
                var callback = GetCallback(callbackId, cancellationToken);
                var storageService = GetWorkspaceServices().GetPersistentStorageService();
                await foreach (var item in AbstractNavigateToSearchService.SearchCachedDocumentsInCurrentProcessAsync(
                    storageService, documentKeys, priorityDocumentKeys, searchPattern, kinds.ToImmutableHashSet(), cancellationToken).ConfigureAwait(false))
                {
                    await callback(item).ConfigureAwait(false);
                }
            }, cancellationToken);
        }
    }
}
