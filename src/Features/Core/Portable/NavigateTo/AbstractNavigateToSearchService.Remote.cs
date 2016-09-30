﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService
    {
        private async Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentInRemoteProcessAsync(
            RemoteHostClient client, Document document, string searchPattern, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;

            using (var session = await client.CreateCodeAnalysisServiceSessionAsync(
                solution, cancellationToken).ConfigureAwait(false))
            {
                var serializableResults = await session.InvokeAsync<SerializableNavigateToSearchResult[]>(
                    nameof(IRemoteNavigateToSearchService.SearchDocumentAsync),
                    SerializableDocumentId.Dehydrate(document),
                    searchPattern).ConfigureAwait(false);

                return serializableResults.Select(r => r.Rehydrate(solution)).ToImmutableArray();
            }
        }

        private async Task<ImmutableArray<INavigateToSearchResult>> SearchProjectInRemoteProcessAsync(
            RemoteHostClient client, Project project, string searchPattern, CancellationToken cancellationToken)
        {
            var solution = project.Solution;

            using (var session = await client.CreateCodeAnalysisServiceSessionAsync(
                solution, cancellationToken).ConfigureAwait(false))
            {
                var serializableResults = await session.InvokeAsync<SerializableNavigateToSearchResult[]>(
                    nameof(IRemoteNavigateToSearchService.SearchProjectAsync),
                    SerializableProjectId.Dehydrate(project.Id),
                    searchPattern).ConfigureAwait(false);

                return serializableResults.Select(r => r.Rehydrate(solution)).ToImmutableArray();
            }
        }

        private static Task<RemoteHostClient> GetRemoteHostClientAsync(
            Project project, CancellationToken cancellationToken)
        {
            var clientService = project.Solution.Workspace.Services.GetService<IRemoteHostClientService>();
            return clientService.GetRemoteHostClientAsync(cancellationToken);
        }
    }
}