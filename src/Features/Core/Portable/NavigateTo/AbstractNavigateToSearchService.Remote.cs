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
            var serializableResults = await session.InvokeAsync<SerializableNavigateToSearchResult[]>(
                nameof(IRemoteNavigateToSearchService.SearchDocumentAsync),
                new object[] { document.Id, searchPattern }, cancellationToken).ConfigureAwait(false);

            serializableResults = serializableResults ?? Array.Empty<SerializableNavigateToSearchResult>();
            return serializableResults.Select(r => r.Rehydrate(document.Project.Solution)).ToImmutableArray();
        }

        private async Task<ImmutableArray<INavigateToSearchResult>> SearchProjectInRemoteProcessAsync(
            RemoteHostClient.Session session, Project project, string searchPattern, CancellationToken cancellationToken)
        {
            var serializableResults = await session.InvokeAsync<SerializableNavigateToSearchResult[]>(
                nameof(IRemoteNavigateToSearchService.SearchProjectAsync),
                new object[] { project.Id, searchPattern }, cancellationToken).ConfigureAwait(false);

            serializableResults = serializableResults ?? Array.Empty<SerializableNavigateToSearchResult>();
            return serializableResults.Select(r => r.Rehydrate(project.Solution)).ToImmutableArray();
        }

        private static Task<RemoteHostClient.Session> GetRemoteHostSessionAsync(Project project, CancellationToken cancellationToken)
        {
            return project.Solution.TryCreateCodeAnalysisServiceSessionAsync(
                NavigateToOptions.OutOfProcessAllowed, WellKnownExperimentNames.OutOfProcessAllowed, cancellationToken);
        }
    }
}