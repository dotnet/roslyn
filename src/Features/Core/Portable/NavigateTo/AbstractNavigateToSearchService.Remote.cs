// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService
    {
        private async Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentInRemoteProcessAsync(
            RemoteHostClient.Session session, Document document, string searchPattern, CancellationToken cancellationToken)
        {
            var serializableResults = await session.InvokeAsync<IList<SerializableNavigateToSearchResult>>(
                nameof(IRemoteNavigateToSearchService.SearchDocumentAsync),
                document.Id, searchPattern).ConfigureAwait(false);

            return serializableResults.SelectAsArray(r => r.Rehydrate(document.Project.Solution));
        }

        private async Task<ImmutableArray<INavigateToSearchResult>> SearchProjectInRemoteProcessAsync(
            RemoteHostClient.Session session, Project project, string searchPattern, CancellationToken cancellationToken)
        {
            var serializableResults = await session.InvokeAsync<IList<SerializableNavigateToSearchResult>>(
                nameof(IRemoteNavigateToSearchService.SearchProjectAsync),
                project.Id, searchPattern).ConfigureAwait(false);

            return serializableResults.SelectAsArray(r => r.Rehydrate(project.Solution));
        }

        private static Task<RemoteHostClient.Session> GetRemoteHostSessionAsync(Project project, CancellationToken cancellationToken)
        {
            // This service is only defined for C# and VB, but we'll be a bit paranoid.
            if (!RemoteSupportedLanguages.IsSupported(project.Language))
            {
                return null;
            }

            return project.Solution.TryCreateCodeAnalysisServiceSessionAsync(
                RemoteFeatureOptions.NavigateToEnabled, cancellationToken);
        }
    }
}