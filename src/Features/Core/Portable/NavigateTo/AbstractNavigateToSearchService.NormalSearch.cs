// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
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
        public async IAsyncEnumerable<INavigateToSearchResult> SearchDocumentAsync(
            Document document,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            await foreach (var item in ConvertItemsAsync(
                document.Project.Solution, activeDocument, SearchDocumentWorkerAsync(), cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }

            IAsyncEnumerable<RoslynNavigateToItem> SearchDocumentWorkerAsync()
            {
                if (client != null)
                {
                    var channel = Channel.CreateUnbounded<RoslynNavigateToItem>();

                    // Kick off the work to do the search in another thread.  That work will push the results into the
                    // channel.  When the work finishes (for any reason, including cancellation), the channel will be 
                    // completed.
                    Task.Run(async () => await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                            // Don't need to sync the full solution when searching a particular project.
                            document.Project,
                            (service, solutionInfo, callbackId, cancellationToken) =>
                                service.SearchDocumentAsync(solutionInfo, document.Id, searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                            new NavigateToSearchServiceCallback(channel), cancellationToken).ConfigureAwait(false), cancellationToken)
                        .CompletesChannel(channel);

                    return channel.Reader.ReadAllAsync(cancellationToken);
                }
                else
                {
                    return SearchDocumentInCurrentProcessAsync(document, searchPattern, kinds, cancellationToken);
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

        public async IAsyncEnumerable<INavigateToSearchResult> SearchProjectAsync(
            Project project,
            ImmutableArray<Document> priorityDocuments,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            await foreach (var item in ConvertItemsAsync(
                project.Solution, activeDocument, SearchProjectWorkerAsync(), cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }

            IAsyncEnumerable<RoslynNavigateToItem> SearchProjectWorkerAsync()
            {
                var solution = project.Solution;

                if (client != null)
                {
                    var priorityDocumentIds = priorityDocuments.SelectAsArray(d => d.Id);

                    var channel = Channel.CreateUnbounded<RoslynNavigateToItem>();

                    // Kick off the work to do the search in another thread.  That work will push the results into the
                    // channel.  When the work finishes (for any reason, including cancellation), the channel will be 
                    // completed.
                    Task.Run(async () => await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                            solution,
                            (service, solutionInfo, callbackId, cancellationToken) =>
                                service.SearchProjectAsync(solutionInfo, project.Id, priorityDocumentIds, searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                            new NavigateToSearchServiceCallback(channel), cancellationToken).ConfigureAwait(false), cancellationToken)
                        .CompletesChannel(channel);

                    return channel.Reader.ReadAllAsync(cancellationToken);
                }
                else
                {
                    return SearchProjectInCurrentProcessAsync(project, priorityDocuments, searchPattern, kinds, cancellationToken);
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
