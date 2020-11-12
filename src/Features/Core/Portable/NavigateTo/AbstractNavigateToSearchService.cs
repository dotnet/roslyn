// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService : INavigateToSearchService
    {
        public IImmutableSet<string> KindsProvided { get; } = ImmutableHashSet.Create(
            NavigateToItemKind.Class,
            NavigateToItemKind.Record,
            NavigateToItemKind.Constant,
            NavigateToItemKind.Delegate,
            NavigateToItemKind.Enum,
            NavigateToItemKind.EnumItem,
            NavigateToItemKind.Event,
            NavigateToItemKind.Field,
            NavigateToItemKind.Interface,
            NavigateToItemKind.Method,
            NavigateToItemKind.Module,
            NavigateToItemKind.Property,
            NavigateToItemKind.Structure);

        public bool CanFilter => true;

        public async Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(
            Document document, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var solution = document.Project.Solution;

                var result = await client.TryInvokeAsync<IRemoteNavigateToSearchService, ImmutableArray<SerializableNavigateToSearchResult>>(
                    solution,
                    (service, solutionInfo, cancellationToken) => service.SearchDocumentAsync(solutionInfo, document.Id, searchPattern, kinds.ToImmutableArray(), cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                return result.HasValue ? result.Value.SelectAsArray(r => r.Rehydrate(solution)) : ImmutableArray<INavigateToSearchResult>.Empty;
            }

            return await SearchDocumentInCurrentProcessAsync(
                document, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync(
            Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var solution = project.Solution;
                var priorityDocumentIds = priorityDocuments.SelectAsArray(d => d.Id);
                var result = await client.TryInvokeAsync<IRemoteNavigateToSearchService, ImmutableArray<SerializableNavigateToSearchResult>>(
                    solution,
                    (service, solutionInfo, cancellationToken) => service.SearchProjectAsync(solutionInfo, project.Id, priorityDocumentIds, searchPattern, kinds.ToImmutableArray(), cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                return result.HasValue ? result.Value.SelectAsArray(r => r.Rehydrate(solution)) : ImmutableArray<INavigateToSearchResult>.Empty;
            }

            return await SearchProjectInCurrentProcessAsync(
                project, priorityDocuments, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
        }
    }
}
