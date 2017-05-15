// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

            var serializableResults = await client.RunCodeAnalysisServiceOnRemoteHostAsync<SerializableNavigateToSearchResult[]>(
                solution, nameof(IRemoteNavigateToSearchService.SearchDocumentAsync),
                new object[] { document.Id, searchPattern }, cancellationToken).ConfigureAwait(false);

            serializableResults = serializableResults ?? Array.Empty<SerializableNavigateToSearchResult>();
            return serializableResults.Select(r => r.Rehydrate(solution)).ToImmutableArray();
        }

        private async Task<ImmutableArray<INavigateToSearchResult>> SearchProjectInRemoteProcessAsync(
            RemoteHostClient client, Project project, string searchPattern, CancellationToken cancellationToken)
        {
            var solution = project.Solution;

            var serializableResults = await client.RunCodeAnalysisServiceOnRemoteHostAsync<SerializableNavigateToSearchResult[]>(
                solution, nameof(IRemoteNavigateToSearchService.SearchProjectAsync),
                new object[] { project.Id, searchPattern }, cancellationToken).ConfigureAwait(false);

            serializableResults = serializableResults ?? Array.Empty<SerializableNavigateToSearchResult>();
            return serializableResults.Select(r => r.Rehydrate(solution)).ToImmutableArray();
        }

        private static async Task<RemoteHostClient> GetRemoteHostClientAsync(Project project, CancellationToken cancellationToken)
        {
            var outOfProcessAllowed = project.Solution.Workspace.Options.GetOption(NavigateToOptions.OutOfProcessAllowed);
            if (!outOfProcessAllowed)
            {
                return null;
            }

            return await project.Solution.Workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}