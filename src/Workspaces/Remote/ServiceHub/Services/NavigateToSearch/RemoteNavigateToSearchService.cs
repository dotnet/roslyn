// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;

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

        public ValueTask SearchDocumentAsync(
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
                var document = solution.GetDocument(documentId);
                var callback = GetCallback(callbackId, cancellationToken);

                await AbstractNavigateToSearchService.SearchDocumentInCurrentProcessAsync(
                    document, searchPattern, kinds.ToImmutableHashSet(), callback, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask SearchProjectAsync(
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
                var project = solution.GetProject(projectId);
                var callback = GetCallback(callbackId, cancellationToken);

                var priorityDocuments = priorityDocumentIds.Select(d => solution.GetDocument(d))
                                                           .ToImmutableArray();

                await AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync(
                    project, priorityDocuments, searchPattern, kinds.ToImmutableHashSet(), callback, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        private Func<INavigateToSearchResult, Task> GetCallback(
            RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
        {
            return async r => await _callback.InvokeAsync((callback, c) =>
                callback.OnResultFoundAsync(callbackId, SerializableNavigateToSearchResult.Dehydrate(r)),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
