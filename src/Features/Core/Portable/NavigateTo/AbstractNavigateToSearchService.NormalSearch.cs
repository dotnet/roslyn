// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo;

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
        var onItemsFound = GetOnItemsFoundCallback(solution, activeDocument: null, onResultsFound);

        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var callback = new NavigateToSearchServiceCallback(onItemsFound, onProjectCompleted: null, cancellationToken);
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

    public static async Task SearchDocumentInCurrentProcessAsync(
        Document document,
        string searchPattern,
        IImmutableSet<string> kinds,
        Func<ImmutableArray<RoslynNavigateToItem>, VoidResult, CancellationToken, Task> onItemsFound,
        CancellationToken cancellationToken)
    {
        var (patternName, patternContainerOpt) = PatternMatcher.GetNameAndContainer(searchPattern);
        var declaredSymbolInfoKindsSet = new DeclaredSymbolInfoKindSet(kinds);

        var results = new ConcurrentSet<RoslynNavigateToItem>();
        await SearchSingleDocumentAsync(
            document, patternName, patternContainerOpt, declaredSymbolInfoKindsSet, t => results.Add(t), cancellationToken).ConfigureAwait(false);

        if (results.Count > 0)
            await onItemsFound(results.ToImmutableArray(), default, cancellationToken).ConfigureAwait(false);
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
        var onItemsFound = GetOnItemsFoundCallback(solution, activeDocument, onResultsFound);

        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var priorityDocumentIds = priorityDocuments.SelectAsArray(d => d.Id);
            var callback = new NavigateToSearchServiceCallback(onItemsFound, onProjectCompleted, cancellationToken);

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
        Func<ImmutableArray<RoslynNavigateToItem>, VoidResult, CancellationToken, Task> onItemsFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken)
    {
        // We're doing a real search over the fully loaded solution now.  No need to hold onto the cached map
        // of potentially stale indices.
        ClearCachedData();

        var (patternName, patternContainerOpt) = PatternMatcher.GetNameAndContainer(searchPattern);
        var declaredSymbolInfoKindsSet = new DeclaredSymbolInfoKindSet(kinds);

        using var _ = GetPooledHashSet(priorityDocuments.Select(d => d.Project), out var highPriProjects);

        // Process each project on its own.  That way we can tell the client when we are done searching it.  Put the
        // projects with priority documents ahead of those without so we can get results for those faster.
        await ProducerConsumer<RoslynNavigateToItem>.RunParallelAsync(
            Prioritize(projects, highPriProjects.Contains),
            SearchSingleProjectAsync, onItemsFound, args: default, cancellationToken).ConfigureAwait(false);
        return;

        async Task SearchSingleProjectAsync(
            Project project,
            Action<RoslynNavigateToItem> onItemFound,
            VoidResult _,
            CancellationToken cancellationToken)
        {
            using var _1 = GetPooledHashSet(priorityDocuments.Where(d => project == d.Project), out var highPriDocs);

            await RoslynParallel.ForEachAsync(
                Prioritize(project.Documents, highPriDocs.Contains),
                cancellationToken,
                (document, cancellationToken) => SearchSingleDocumentAsync(
                    document, patternName, patternContainerOpt, declaredSymbolInfoKindsSet, onItemFound, cancellationToken)).ConfigureAwait(false);

            await onProjectCompleted().ConfigureAwait(false);
        }
    }
}
