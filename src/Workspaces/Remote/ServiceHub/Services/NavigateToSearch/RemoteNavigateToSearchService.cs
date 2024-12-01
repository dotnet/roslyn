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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteNavigateToSearchService(
    in BrokeredServiceBase.ServiceConstructionArguments arguments,
    RemoteCallback<IRemoteNavigateToSearchService.ICallback> callback)
    : BrokeredServiceBase(arguments), IRemoteNavigateToSearchService
{
    internal sealed class Factory : FactoryBase<IRemoteNavigateToSearchService, IRemoteNavigateToSearchService.ICallback>
    {
        protected override IRemoteNavigateToSearchService CreateService(
            in ServiceConstructionArguments arguments, RemoteCallback<IRemoteNavigateToSearchService.ICallback> callback)
            => new RemoteNavigateToSearchService(arguments, callback);
    }

    private readonly RemoteCallback<IRemoteNavigateToSearchService.ICallback> _callback = callback;

    private (Func<ImmutableArray<RoslynNavigateToItem>, VoidResult, CancellationToken, Task> onItemsFound, Func<Task> onProjectCompleted) GetCallbacks(
        RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
    {
        return (
            async (array, _, cancellationToken) => await _callback.InvokeAsync((callback, cancellationToken) => callback.OnItemsFoundAsync(callbackId, array), cancellationToken).ConfigureAwait(false),
            async () => await _callback.InvokeAsync((callback, cancellationToken) => callback.OnProjectCompletedAsync(callbackId), cancellationToken).ConfigureAwait(false));
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
            var (onItemsFound, onProjectCompleted) = GetCallbacks(callbackId, cancellationToken);

            await AbstractNavigateToSearchService.SearchDocumentInCurrentProcessAsync(
                document, searchPattern, kinds.ToImmutableHashSet(), onItemsFound, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    public ValueTask SearchProjectsAsync(
        Checksum solutionChecksum,
        ImmutableArray<ProjectId> projectIds,
        ImmutableArray<DocumentId> priorityDocumentIds,
        string searchPattern,
        ImmutableArray<string> kinds,
        RemoteServiceCallbackId callbackId,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var projects = projectIds.SelectAsArray(solution.GetRequiredProject);
            var (onItemsFound, onProjectCompleted) = GetCallbacks(callbackId, cancellationToken);

            var priorityDocuments = priorityDocumentIds.SelectAsArray(d => solution.GetRequiredDocument(d));

            await AbstractNavigateToSearchService.SearchProjectsInCurrentProcessAsync(
                projects, priorityDocuments, searchPattern, kinds.ToImmutableHashSet(), onItemsFound, onProjectCompleted, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    public ValueTask SearchGeneratedDocumentsAsync(
        Checksum solutionChecksum,
        ImmutableArray<ProjectId> projectIds,
        string searchPattern,
        ImmutableArray<string> kinds,
        RemoteServiceCallbackId callbackId,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var projects = projectIds.SelectAsArray(solution.GetRequiredProject);
            var (onItemsFound, onProjectCompleted) = GetCallbacks(callbackId, cancellationToken);

            await AbstractNavigateToSearchService.SearchGeneratedDocumentsInCurrentProcessAsync(
                projects, searchPattern, kinds.ToImmutableHashSet(), onItemsFound, onProjectCompleted, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    public ValueTask SearchCachedDocumentsAsync(
        ImmutableArray<DocumentKey> documentKeys,
        ImmutableArray<DocumentKey> priorityDocumentKeys,
        string searchPattern,
        ImmutableArray<string> kinds,
        RemoteServiceCallbackId callbackId,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(async cancellationToken =>
        {
            // Intentionally do not call GetSolutionAsync here.  We do not want the cost of
            // synchronizing the solution over to the remote side.  Instead, we just directly
            // check whatever cached data we have from the previous vs session.
            var (onItemsFound, onProjectCompleted) = GetCallbacks(callbackId, cancellationToken);
            var storageService = GetWorkspaceServices().GetPersistentStorageService();
            await AbstractNavigateToSearchService.SearchCachedDocumentsInCurrentProcessAsync(
                storageService, documentKeys, priorityDocumentKeys, searchPattern, kinds.ToImmutableHashSet(), onItemsFound, onProjectCompleted, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }
}
