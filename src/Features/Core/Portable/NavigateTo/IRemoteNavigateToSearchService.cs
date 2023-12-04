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
        ValueTask SearchProjectsAsync(Checksum solutionChecksum, ImmutableArray<ProjectId> projectIds, ImmutableArray<DocumentId> priorityDocumentIds, string searchPattern, ImmutableArray<string> kinds, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);

        ValueTask SearchGeneratedDocumentsAsync(Checksum solutionChecksum, ImmutableArray<ProjectId> projectIds, string searchPattern, ImmutableArray<string> kinds, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
        ValueTask SearchCachedDocumentsAsync(ImmutableArray<DocumentKey> documentKeys, ImmutableArray<DocumentKey> priorityDocumentKeys, string searchPattern, ImmutableArray<string> kinds, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);

        ValueTask HydrateAsync(Checksum solutionChecksum, CancellationToken cancellationToken);

        public interface ICallback
        {
            ValueTask OnResultFoundAsync(RemoteServiceCallbackId callbackId, RoslynNavigateToItem result);
            ValueTask OnProjectCompletedAsync(RemoteServiceCallbackId callbackId);
        }
    }

    [ExportRemoteServiceCallbackDispatcher(typeof(IRemoteNavigateToSearchService)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class NavigateToSearchServiceServerCallbackDispatcher() : RemoteServiceCallbackDispatcher, IRemoteNavigateToSearchService.ICallback
    {
        private new NavigateToSearchServiceCallback GetCallback(RemoteServiceCallbackId callbackId)
            => (NavigateToSearchServiceCallback)base.GetCallback(callbackId);

        public ValueTask OnResultFoundAsync(RemoteServiceCallbackId callbackId, RoslynNavigateToItem result)
            => GetCallback(callbackId).OnResultFoundAsync(result);

        public ValueTask OnProjectCompletedAsync(RemoteServiceCallbackId callbackId)
            => GetCallback(callbackId).OnProjectCompletedAsync();
    }

    internal sealed class NavigateToSearchServiceCallback(
        Func<RoslynNavigateToItem, Task> onResultFound,
        Func<Task>? onProjectCompleted)
    {
        public async ValueTask OnResultFoundAsync(RoslynNavigateToItem result)
        {
            try
            {
                await onResultFound(result).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex))
            {
            }
        }

        public async ValueTask OnProjectCompletedAsync()
        {
            try
            {
                if (onProjectCompleted is null)
                    return;

                await onProjectCompleted().ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex))
            {
            }
        }
    }
}
