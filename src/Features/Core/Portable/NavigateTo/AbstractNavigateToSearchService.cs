// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService : INavigateToSearchService
    {
        public IImmutableSet<string> KindsProvided { get; } = ImmutableHashSet.Create(
            NavigateToItemKind.Class,
            NavigateToItemKind.Record,
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

        public async Task SearchDocumentAsync(
            Document document, string searchPattern, IImmutableSet<string> kinds,
            Func<INavigateToSearchResult, Task> onResultFound, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var solution = document.Project.Solution;
                var callback = new NavigateToSearchServiceCallback(solution, onResultFound, cancellationToken);
                await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                    solution,
                    (service, solutionInfo, callbackId, cancellationToken) => service.SearchDocumentAsync(solutionInfo, document.Id, searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                    callback, cancellationToken).ConfigureAwait(false);

                return;
            }

            await SearchDocumentInCurrentProcessAsync(
                document, searchPattern, kinds, onResultFound, cancellationToken).ConfigureAwait(false);
        }

        public async Task SearchProjectAsync(
            Project project, ImmutableArray<Document> priorityDocuments, string searchPattern,
            IImmutableSet<string> kinds, Func<INavigateToSearchResult, Task> onResultFound, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var solution = project.Solution;
                var priorityDocumentIds = priorityDocuments.SelectAsArray(d => d.Id);
                var callback = new NavigateToSearchServiceCallback(solution, onResultFound, cancellationToken);
                await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                    solution,
                    (service, solutionInfo, callbackId, cancellationToken) => service.SearchProjectAsync(solutionInfo, project.Id, priorityDocumentIds, searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                    callback, cancellationToken).ConfigureAwait(false);

                return;
            }

            await SearchProjectInCurrentProcessAsync(
                project, priorityDocuments, searchPattern, kinds, onResultFound, cancellationToken).ConfigureAwait(false);
        }
    }
}
