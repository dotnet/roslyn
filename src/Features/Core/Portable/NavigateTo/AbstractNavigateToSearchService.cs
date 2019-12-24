// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService : INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate
    {
        public IImmutableSet<string> KindsProvided { get; } = ImmutableHashSet.Create(
            NavigateToItemKind.Class,
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

                var result = await client.TryRunRemoteAsync<IList<SerializableNavigateToSearchResult>>(
                    WellKnownServiceHubServices.CodeAnalysisService,
                    nameof(IRemoteNavigateToSearchService.SearchDocumentAsync),
                    solution,
                    new object[] { document.Id, searchPattern, kinds.ToArray() },
                    callbackTarget: null,
                    cancellationToken).ConfigureAwait(false);

                if (result.HasValue)
                {
                    return result.Value.SelectAsArray(r => r.Rehydrate(solution));
                }
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

                var result = await client.TryRunRemoteAsync<IList<SerializableNavigateToSearchResult>>(
                    WellKnownServiceHubServices.CodeAnalysisService,
                    nameof(IRemoteNavigateToSearchService.SearchProjectAsync),
                    solution,
                    new object[] { project.Id, priorityDocuments.Select(d => d.Id).ToArray(), searchPattern, kinds.ToArray() },
                    callbackTarget: null,
                    cancellationToken).ConfigureAwait(false);

                if (result.HasValue)
                {
                    return result.Value.SelectAsArray(r => r.Rehydrate(solution));
                }
            }

            return await SearchProjectInCurrentProcessAsync(
                project, priorityDocuments, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
        }
    }
}
