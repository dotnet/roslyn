// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteNavigateToSearchService : BrokeredServiceBase, IRemoteNavigateToSearchService
    {
        internal sealed class Factory : FactoryBase<IRemoteNavigateToSearchService>
        {
            protected override IRemoteNavigateToSearchService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteNavigateToSearchService(arguments);
        }

        public RemoteNavigateToSearchService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        public ValueTask<ImmutableArray<SerializableNavigateToSearchResult>> SearchDocumentAsync(
            PinnedSolutionInfo solutionInfo,
            DocumentId documentId,
            string searchPattern,
            ImmutableArray<string> kinds,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                    var document = solution.GetDocument(documentId);
                    var result = await AbstractNavigateToSearchService.SearchDocumentInCurrentProcessAsync(
                        document, searchPattern, kinds.ToImmutableHashSet(), cancellationToken).ConfigureAwait(false);

                    return Convert(result);
                }
            }, cancellationToken);
        }

        public ValueTask<ImmutableArray<SerializableNavigateToSearchResult>> SearchProjectAsync(
            PinnedSolutionInfo solutionInfo,
            ProjectId projectId,
            ImmutableArray<DocumentId> priorityDocumentIds,
            string searchPattern,
            ImmutableArray<string> kinds,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                    var project = solution.GetProject(projectId);
                    var priorityDocuments = priorityDocumentIds.Select(d => solution.GetDocument(d))
                                                               .ToImmutableArray();

                    var result = await AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync(
                        project, priorityDocuments, searchPattern, kinds.ToImmutableHashSet(), cancellationToken).ConfigureAwait(false);

                    return Convert(result);
                }
            }, cancellationToken);
        }

        private static ImmutableArray<SerializableNavigateToSearchResult> Convert(
            ImmutableArray<INavigateToSearchResult> result)
        {
            return result.SelectAsArray(SerializableNavigateToSearchResult.Dehydrate);
        }
    }
}
