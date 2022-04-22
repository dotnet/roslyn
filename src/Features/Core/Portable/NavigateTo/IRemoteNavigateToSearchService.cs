// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal interface IRemoteNavigateToSearchService
    {
        ValueTask SearchDocumentAsync(Checksum solutionChecksum, DocumentId documentId, string searchPattern, ImmutableArray<string> kinds, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
        ValueTask SearchProjectAsync(Checksum solutionChecksum, ProjectId projectId, ImmutableArray<DocumentId> priorityDocumentIds, string searchPattern, ImmutableArray<string> kinds, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);

        ValueTask SearchGeneratedDocumentsAsync(Checksum solutionChecksum, ProjectId projectId, string searchPattern, ImmutableArray<string> kinds, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
        ValueTask SearchCachedDocumentsAsync(ImmutableArray<DocumentKey> documentKeys, ImmutableArray<DocumentKey> priorityDocumentKeys, string searchPattern, ImmutableArray<string> kinds, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);

        ValueTask HydrateAsync(Checksum solutionChecksum, CancellationToken cancellationToken);

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
        {
            try
            {
                await _onResultFound(result).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex))
            {
            }
        }
    }
}
