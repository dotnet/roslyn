// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo;

#if NET
using Parallel = System.Threading.Tasks.Parallel;
#else
using Parallel = Roslyn.Utilities.Parallel;
#endif

internal abstract partial class AbstractNavigateToSearchService
{
    public async Task SearchDocumentAsync(
        Document document,
        string searchPattern,
        IImmutableSet<string> kinds,
        Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound,
        CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var onItemsFound = GetOnItemsFoundCallback(solution, activeDocument: null, onResultsFound, cancellationToken);

        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var callback = new NavigateToSearchServiceCallback(onItemsFound, onProjectCompleted: null);
            // Don't need to sync the full solution when searching a single document.  Just sync the project that doc is in.
            await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                document.Project,
                (service, solutionInfo, callbackId, cancellationToken) =>
                service.SearchDocumentAsync(solutionInfo, document.Id, searchPattern, [.. kinds], callbackId, cancellationToken),
                callback, cancellationToken).ConfigureAwait(false);

            return;
        }

        await SearchDocumentInCurrentProcessAsync(document, searchPattern, kinds, onItemsFound, cancellationToken).ConfigureAwait(false);
    }

    public static async Task SearchDocumentInCurrentProcessAsync(Document document, string searchPattern, IImmutableSet<string> kinds, Func<ImmutableArray<RoslynNavigateToItem>, Task> onItemsFound, CancellationToken cancellationToken)
    {
        var results = new ConcurrentSet<RoslynNavigateToItem>();
        await SearchProjectInCurrentProcessAsync(
            document.Project, priorityDocuments: [], document, searchPattern, kinds,
            t => results.Add(t), () => Task.CompletedTask, cancellationToken).ConfigureAwait(false);

        if (results.Count > 0)
            await onItemsFound(results.ToImmutableArray()).ConfigureAwait(false);
    }

    public async Task SearchProjectsAsync(
        Solution solution,
        ImmutableArray<Project> projects,
        ImmutableArray<Document> priorityDocuments,
        string searchPattern,
        IImmutableSet<string> kinds,
        Document? activeDocument,
        Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        Contract.ThrowIfTrue(projects.IsEmpty);
        Contract.ThrowIfTrue(projects.Select(p => p.Language).Distinct().Count() != 1);

        Debug.Assert(priorityDocuments.All(d => projects.Contains(d.Project)));
        var onItemsFound = GetOnItemsFoundCallback(solution, activeDocument, onResultsFound, cancellationToken);

        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var priorityDocumentIds = priorityDocuments.SelectAsArray(d => d.Id);
            var callback = new NavigateToSearchServiceCallback(onItemsFound, onProjectCompleted);

            await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                // Intentionally sync the full solution.   When SearchProjectAsync is called, we're searching all
                // projects (just in parallel).  So best for them all to sync and share a single solution snapshot
                // on the oop side.
                solution,
                (service, solutionInfo, callbackId, cancellationToken) =>
                    service.SearchProjectsAsync(solutionInfo, projects.SelectAsArray(p => p.Id), priorityDocumentIds, searchPattern, [.. kinds], callbackId, cancellationToken),
                callback, cancellationToken).ConfigureAwait(false);

            return;
        }

        await SearchProjectsInCurrentProcessAsync(
            projects, priorityDocuments, searchPattern, kinds, onItemsFound, onProjectCompleted, cancellationToken).ConfigureAwait(false);
    }

    public static async Task SearchProjectsInCurrentProcessAsync(
        ImmutableArray<Project> projects,
        ImmutableArray<Document> priorityDocuments,
        string searchPattern,
        IImmutableSet<string> kinds,
        Func<ImmutableArray<RoslynNavigateToItem>, Task> onItemsFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<RoslynNavigateToItem>(s_channelOptions);

        await Task.WhenAll(
            FindAllItemsAndWriteToChannelAsync(channel.Writer, SearchProjectsAsync),
            ReadItemsFromChannelAndReportToCallbackAsync(channel.Reader, onItemsFound, cancellationToken)).ConfigureAwait(false);

        return;

        async Task SearchProjectsAsync(Action<RoslynNavigateToItem> onItemFound)
        {
            using var _1 = GetPooledHashSet(priorityDocuments.Select(d => d.Project), out var highPriProjects);
            using var _2 = GetPooledHashSet(projects.Where(p => !highPriProjects.Contains(p)), out var lowPriProjects);

            Debug.Assert(projects.SetEquals(highPriProjects.Concat(lowPriProjects)));

            await Parallel.ForEachAsync(
                highPriProjects.Concat(lowPriProjects),
                cancellationToken,
                (project, cancellationToken) =>
                    SearchProjectInCurrentProcessAsync(
                        project, priorityDocuments.WhereAsArray(d => d.Project == project), searchDocument: null,
                        searchPattern, kinds, onItemFound, onProjectCompleted, cancellationToken)).ConfigureAwait(false);
        }
    }
}
