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
            RemoteHostClient client, Document document, string searchPattern, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;

            var serializableResults = await client.TryRunCodeAnalysisRemoteAsync<ImmutableArray<SerializableNavigateToSearchResult>>(
                solution, nameof(IRemoteNavigateToSearchService.SearchDocumentAsync),
                new object[] { document.Id, searchPattern }, cancellationToken).ConfigureAwait(false);

            return serializableResults.NullToEmpty().SelectAsArray(r => r.Rehydrate(solution));
        }

        private async Task<ImmutableArray<INavigateToSearchResult>> SearchProjectInRemoteProcessAsync(
            RemoteHostClient client, Project project, string searchPattern, CancellationToken cancellationToken)
        {
            var solution = project.Solution;

            var serializableResults = await client.TryRunCodeAnalysisRemoteAsync<ImmutableArray<SerializableNavigateToSearchResult>>(
                solution, nameof(IRemoteNavigateToSearchService.SearchProjectAsync),
                new object[] { project.Id, searchPattern }, cancellationToken).ConfigureAwait(false);

            return serializableResults.NullToEmpty().SelectAsArray(r => r.Rehydrate(solution));
        }

        private static async Task<RemoteHostClient> TryGetRemoteHostClientAsync(Project project, CancellationToken cancellationToken)
        {
            // This service is only defined for C# and VB, but we'll be a bit paranoid.
            if (!RemoteSupportedLanguages.IsSupported(project.Language))
            {
                return null;
            }

            return await project.Solution.Workspace.TryGetRemoteHostClientAsync(RemoteFeatureOptions.NavigateToEnabled, cancellationToken).ConfigureAwait(false);
        }
    }
}
