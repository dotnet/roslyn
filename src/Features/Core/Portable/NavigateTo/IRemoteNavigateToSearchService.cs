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
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal interface IRemoteNavigateToSearchService
    {
        ValueTask SearchFullyLoadedDocumentAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, string searchPattern, ImmutableArray<string> kinds, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
        ValueTask SearchFullyLoadedProjectAsync(PinnedSolutionInfo solutionInfo, ProjectId projectId, ImmutableArray<DocumentId> priorityDocumentIds, string searchPattern, ImmutableArray<string> kinds, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
        ValueTask SearchCachedDocumentsAsync(ImmutableArray<DocumentKey> documentKeys, ImmutableArray<DocumentKey> priorityDocumentKeys, StorageDatabase database, string searchPattern, ImmutableArray<string> kinds, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
        ValueTask HydrateAsync(PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken);

        public interface ICallback
        {
            ValueTask OnResultFoundAsync(RemoteServiceCallbackId callbackId, RoslynNavigateToItem result);
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

        public ValueTask OnResultFoundAsync(RemoteServiceCallbackId callbackId, RoslynNavigateToItem result)
            => GetCallback(callbackId).OnResultFoundAsync(result);
    }

    internal sealed class NavigateToSearchServiceCallback
    {
        private readonly Func<RoslynNavigateToItem, Task> _onResultFound;

        public NavigateToSearchServiceCallback(Func<RoslynNavigateToItem, Task> onResultFound)
        {
            _onResultFound = onResultFound;
        }

        public async ValueTask OnResultFoundAsync(RoslynNavigateToItem result)
            => await _onResultFound(result).ConfigureAwait(false);
    }
}
