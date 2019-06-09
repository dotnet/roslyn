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
    internal abstract partial class AbstractNavigateToSearchService
    {
        private async Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentInRemoteProcessAsync(
            RemoteHostClient client, Document document, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;

            var serializableResults = await client.TryRunCodeAnalysisRemoteAsync<IList<SerializableNavigateToSearchResult>>(
                solution, nameof(IRemoteNavigateToSearchService.SearchDocumentAsync),
                new object[] { document.Id, searchPattern, kinds.ToArray() }, cancellationToken).ConfigureAwait(false);

            return serializableResults.SelectAsArray(r => r.Rehydrate(solution));
        }

        private async Task<ImmutableArray<INavigateToSearchResult>> SearchProjectInRemoteProcessAsync(
            RemoteHostClient client, Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
        {
            var solution = project.Solution;

            var serializableResults = await client.TryRunCodeAnalysisRemoteAsync<IList<SerializableNavigateToSearchResult>>(
                solution, nameof(IRemoteNavigateToSearchService.SearchProjectAsync),
                new object[] { project.Id, priorityDocuments.Select(d => d.Id).ToArray(), searchPattern, kinds.ToArray() }, cancellationToken).ConfigureAwait(false);

            return serializableResults.SelectAsArray(r => r.Rehydrate(solution));
        }

        private static async Task<RemoteHostClient> TryGetRemoteHostClientAsync(Project project, CancellationToken cancellationToken)
        {
            // This service is only defined for C# and VB, but we'll be a bit paranoid.
            if (!RemoteSupportedLanguages.IsSupported(project.Language))
            {
                return null;
            }

            return await project.Solution.Workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
