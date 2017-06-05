// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Experiments;
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
                new object[] { document.Id, searchPattern }, cancellationToken).ConfigureAwait(false);

            return serializableResults.SelectAsArray(r => r.Rehydrate(document.Project.Solution));
        }

        private async Task<ImmutableArray<INavigateToSearchResult>> SearchProjectInRemoteProcessAsync(
            RemoteHostClient.Session session, Project project, string searchPattern, CancellationToken cancellationToken)
        {
            var serializableResults = await session.InvokeAsync<ImmutableArray<SerializableNavigateToSearchResult>>(
                nameof(IRemoteNavigateToSearchService.SearchProjectAsync),
                new object[] { project.Id, searchPattern }, cancellationToken).ConfigureAwait(false);

            return serializableResults.SelectAsArray(r => r.Rehydrate(project.Solution));
        }

        private static Task<RemoteHostClient.Session> GetRemoteHostSessionAsync(Project project, CancellationToken cancellationToken)
        {
            return project.Solution.TryCreateCodeAnalysisServiceSessionAsync(
                RemoteFeatureOptions.NavigateToEnabled, cancellationToken);
        }
    }
}