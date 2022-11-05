// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService
    {
        public IAsyncEnumerable<INavigateToSearchResult> SearchDocumentAsync(
            Document document,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            CancellationToken cancellationToken)
        {
            var result = SearchDocumentWorkerAsync(cancellationToken);
            return ConvertItemsAsync(document.Project.Solution, activeDocument, result, cancellationToken);

            async IAsyncEnumerable<RoslynNavigateToItem> SearchDocumentWorkerAsync(
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    var channel = Channel.CreateUnbounded<RoslynNavigateToItem>();
                    Task.Run(async () =>
                    {
                        // Don't need to sync the full solution when searching a particular project.
                        await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                            document.Project,
                            (service, solutionInfo, callbackId, cancellationToken) =>
                            service.SearchDocumentAsync(solutionInfo, document.Id, searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                            new NavigateToSearchServiceCallback(channel), cancellationToken).ConfigureAwait(false);
                    }, cancellationToken).CompletesChannel(channel);

                    await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
                        yield return item;
                }
                else
                {
                    var result = SearchDocumentInCurrentProcessAsync(document, searchPattern, kinds, cancellationToken);

                    await foreach (var item in result.ConfigureAwait(false))
                        yield return item;
                }
            }
        }

        public static IAsyncEnumerable<RoslynNavigateToItem> SearchDocumentInCurrentProcessAsync(
            Document document, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
        {
            return SearchProjectInCurrentProcessAsync(
                document.Project, priorityDocuments: ImmutableArray<Document>.Empty, document,
                searchPattern, kinds, cancellationToken);
        }

        public IAsyncEnumerable<INavigateToSearchResult> SearchProjectAsync(
            Project project,
            ImmutableArray<Document> priorityDocuments,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            CancellationToken cancellationToken)
        {
            var result = SearchProjectWorkerAsync(cancellationToken);
            return ConvertItemsAsync(project.Solution, activeDocument, result, cancellationToken);

            async IAsyncEnumerable<RoslynNavigateToItem> SearchProjectWorkerAsync(
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                var solution = project.Solution;

                var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    var priorityDocumentIds = priorityDocuments.SelectAsArray(d => d.Id);

                    var channel = Channel.CreateUnbounded<RoslynNavigateToItem>();

                    Task.Run(async () =>
                    {
                        // Don't need to sync the full solution when searching a particular project.
                        await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                            solution,
                            (service, solutionInfo, callbackId, cancellationToken) =>
                                service.SearchProjectAsync(solutionInfo, project.Id, priorityDocumentIds, searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                            new NavigateToSearchServiceCallback(channel), cancellationToken).ConfigureAwait(false);
                    }, cancellationToken).CompletesChannel(channel);

                    await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
                        yield return item;
                }
                else
                {
                    var result = SearchProjectInCurrentProcessAsync(project, priorityDocuments, searchPattern, kinds, cancellationToken);
                    await foreach (var item in result.ConfigureAwait(false))
                        yield return item;
                }
            }
        }

        public static IAsyncEnumerable<RoslynNavigateToItem> SearchProjectInCurrentProcessAsync(
            Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
        {
            return SearchProjectInCurrentProcessAsync(
                project, priorityDocuments, searchDocument: null,
                pattern: searchPattern, kinds, cancellationToken);
        }
    }
}
