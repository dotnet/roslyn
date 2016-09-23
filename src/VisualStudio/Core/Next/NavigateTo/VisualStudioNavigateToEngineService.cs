// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Arguments;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Remote;

namespace Microsoft.VisualStudio.LanguageServices.NavigateTo
{
    [ExportWorkspaceService(typeof(INavigateToEngineService)), Shared]
    internal class VisualStudioNavigateToEngineService : INavigateToEngineService
    {
        public async Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(
            Document document, string searchPattern, CancellationToken cancellationToken)
        {
            var client = await GetRemoteHostClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await DefaultNavigateToEngineService.SearchDocumentInCurrentProcessAsync(
                    document, searchPattern, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await SearchDocumentInRemoteProcessAsync(
                    client, document, searchPattern, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentInRemoteProcessAsync(
            RemoteHostClient client, Document document, string searchPattern, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;

            using (var session = await client.CreateCodeAnalysisServiceSessionAsync(
                solution, cancellationToken).ConfigureAwait(false))
            {
                var serializableResults = await session.InvokeAsync<SerializableNavigateToSearchResult[]>(
                    WellKnownServiceHubServices.CodeAnalysisService_SearchDocumentAsync,
                    SerializableDocumentId.Dehydrate(document),
                    searchPattern).ConfigureAwait(false);

                return serializableResults.Select(r => r.Rehydrate(solution)).ToImmutableArray();
            }
        }

        public async Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync(
            Project project, string searchPattern, CancellationToken cancellationToken)
        {
            var client = await GetRemoteHostClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await DefaultNavigateToEngineService.SearchProjectInCurrentProcessAsync(
                    project, searchPattern, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await SearchProjectInRemoteProcessAsync(
                    client, project, searchPattern, cancellationToken).ConfigureAwait(false);
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
                    WellKnownServiceHubServices.CodeAnalysisService_SearchProjectAsync,
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
