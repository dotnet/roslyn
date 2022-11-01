// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteNavigateToSearchService : BrokeredServiceBase, IRemoteNavigateToSearchService
    {
        /// <summary>
        /// Navigate to is implemented using <see cref="IAsyncEnumerable{T}"/>.  This API works by having the client
        /// "pull" for results from the server.  That means that the computation to produce the next result happens only
        /// when the client is actually asking for that.  We would like to utilize resources more thoroughly when
        /// searching, allowing for searching for results on multiple cores.  As such, we set a "max read ahead" amount
        /// that allows the server to keep processing and producing results, even as the client is processing the batch
        /// of results.
        /// </summary>
        /// <remarks>
        /// This value was not determined empirically.
        /// </remarks>
        private const int MaxReadAhead = 64;

        internal sealed class Factory : FactoryBase<IRemoteNavigateToSearchService>
        {
            protected override IRemoteNavigateToSearchService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteNavigateToSearchService(arguments);
        }

        public RemoteNavigateToSearchService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        public ValueTask HydrateAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            // All we need to do is request the solution.  This will ensure that all assets are
            // pulled over from the host side to the remote side.  Once this completes, the next
            // call to SearchFullyLoadedDocumentAsync or SearchFullyLoadedProjectAsync will be
            // quick as very little will need to by sync'ed over.
            return RunServiceAsync(solutionChecksum, solution => ValueTaskFactory.CompletedTask, cancellationToken);
        }

        public IAsyncEnumerable<RoslynNavigateToItem> SearchDocumentAsync(
            Checksum solutionChecksum,
            DocumentId documentId,
            string searchPattern,
            ImmutableArray<string> kinds,
            CancellationToken cancellationToken)
        {
            return StreamWithSolutionAsync(
                solutionChecksum,
                (solution, cancellationToken) =>
                {
                    var document = solution.GetRequiredDocument(documentId);

                    return AbstractNavigateToSearchService.SearchDocumentInCurrentProcessAsync(
                        document, searchPattern, kinds.ToImmutableHashSet(), cancellationToken);
                },
                cancellationToken).WithJsonRpcSettings(new JsonRpcEnumerableSettings { MaxReadAhead = MaxReadAhead });
        }

        public IAsyncEnumerable<RoslynNavigateToItem> SearchProjectAsync(
            Checksum solutionChecksum,
            ProjectId projectId,
            ImmutableArray<DocumentId> priorityDocumentIds,
            string searchPattern,
            ImmutableArray<string> kinds,
            CancellationToken cancellationToken)
        {
            return StreamWithSolutionAsync(
                solutionChecksum,
                (solution, cancellationToken) =>
                {
                    var project = solution.GetRequiredProject(projectId);

                    var priorityDocuments = priorityDocumentIds.SelectAsArray(d => solution.GetRequiredDocument(d));

                    return AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync(
                        project, priorityDocuments, searchPattern, kinds.ToImmutableHashSet(), cancellationToken);
                }, cancellationToken).WithJsonRpcSettings(new JsonRpcEnumerableSettings { MaxReadAhead = MaxReadAhead });
        }

        public IAsyncEnumerable<RoslynNavigateToItem> SearchGeneratedDocumentsAsync(
            Checksum solutionChecksum,
            ProjectId projectId,
            string searchPattern,
            ImmutableArray<string> kinds,
            CancellationToken cancellationToken)
        {
            return StreamWithSolutionAsync(
                solutionChecksum,
                (solution, cancellationToken) =>
                {
                    var project = solution.GetRequiredProject(projectId);

                    return AbstractNavigateToSearchService.SearchGeneratedDocumentsInCurrentProcessAsync(
                        project, searchPattern, kinds.ToImmutableHashSet(), cancellationToken);
                },
                cancellationToken).WithJsonRpcSettings(new JsonRpcEnumerableSettings { MaxReadAhead = MaxReadAhead });
        }

        public IAsyncEnumerable<RoslynNavigateToItem> SearchCachedDocumentsAsync(
            ImmutableArray<DocumentKey> documentKeys,
            ImmutableArray<DocumentKey> priorityDocumentKeys,
            string searchPattern,
            ImmutableArray<string> kinds,
            CancellationToken cancellationToken)
        {
            WorkspaceManager.SolutionAssetCache.UpdateLastActivityTime();

            // Intentionally do not call GetSolutionAsync here.  We do not want the cost of
            // synchronizing the solution over to the remote side.  Instead, we just directly
            // check whatever cached data we have from the previous VS session.
            var storageService = GetWorkspaceServices().GetPersistentStorageService();
            return AbstractNavigateToSearchService.SearchCachedDocumentsInCurrentProcessAsync(
                storageService, documentKeys, priorityDocumentKeys, searchPattern, kinds.ToImmutableHashSet(), cancellationToken);
        }
    }
}
