// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal interface IRemoteNavigateToSearchService
    {
        ValueTask SearchDocumentAsync(
            PinnedSolutionInfo solutionInfo, DocumentId documentId, string searchPattern, ImmutableArray<string> kinds, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);

        ValueTask SearchProjectAsync(
            PinnedSolutionInfo solutionInfo, ProjectId projectId, ImmutableArray<DocumentId> priorityDocumentIds, string searchPattern, ImmutableArray<string> kinds, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);

        public interface ICallback
        {
            ValueTask OnResultFoundAsync(RemoteServiceCallbackId callbackId, SerializableNavigateToSearchResult result);
        }
    }

    [ExportRemoteServiceCallbackDispatcher(typeof(IRemoteNavigateToSearchService)), Shared]
    internal sealed class NavigateToSearchServiceServerCallbackDispatcher : RemoteServiceCallbackDispatcher, IRemoteNavigateToSearchService.ICallback
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NavigateToSearchServiceServerCallbackDispatcher()
        {
        }

        private new NavigateToSearchServiceCallback GetCallback(RemoteServiceCallbackId callbackId)
            => (NavigateToSearchServiceCallback)base.GetCallback(callbackId);

        public ValueTask OnResultFoundAsync(RemoteServiceCallbackId callbackId, SerializableNavigateToSearchResult result)
            => GetCallback(callbackId).OnResultFoundAsync(result);
    }

    internal sealed class NavigateToSearchServiceCallback
    {
        private readonly Solution _solution;
        private readonly Func<INavigateToSearchResult, Task> _onResultFound;
        private readonly CancellationToken _cancellationToken;

        public NavigateToSearchServiceCallback(
            Solution solution,
            Func<INavigateToSearchResult, Task> onResultFound,
            CancellationToken cancellationToken)
        {
            _solution = solution;
            _onResultFound = onResultFound;
            _cancellationToken = cancellationToken;
        }

        public async ValueTask OnResultFoundAsync(SerializableNavigateToSearchResult result)
            => await _onResultFound(await result.RehydrateAsync(_solution, _cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
    }
}
