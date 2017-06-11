// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService
    {
        private async Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentInRemoteProcessAsync(
            RemoteHostClient.Session session, Document document, string searchPattern, CancellationToken cancellationToken)
        {
            var serializableResults = await session.InvokeAsync<ImmutableArray<SerializableNavigateToSearchResult>>(
                nameof(IRemoteNavigateToSearchService.SearchDocumentAsync),
                document.Id, searchPattern).ConfigureAwait(false);

            return serializableResults.SelectAsArray(r => r.Rehydrate(document.Project.Solution));
        }

        private async Task<ImmutableArray<INavigateToSearchResult>> SearchProjectInRemoteProcessAsync(
            RemoteHostClient.Session session, Project project, string searchPattern, CancellationToken cancellationToken)
        {
            var serializableResults = await session.InvokeAsync<ImmutableArray<SerializableNavigateToSearchResult>>(
                nameof(IRemoteNavigateToSearchService.SearchProjectAsync),
                project.Id, searchPattern).ConfigureAwait(false);

            return serializableResults.SelectAsArray(r => r.Rehydrate(project.Solution));
        }

        private static Task<RemoteHostClient.Session> GetRemoteHostSessionAsync(Project project, CancellationToken cancellationToken)
        {
            return project.Solution.TryCreateCodeAnalysisServiceSessionAsync(
                RemoteFeatureOptions.NavigateToEnabled, cancellationToken);
        }
    }
}