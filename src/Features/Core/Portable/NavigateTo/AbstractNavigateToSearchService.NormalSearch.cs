// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo;

internal abstract partial class AbstractNavigateToSearchService
{
    public async Task SearchDocumentAsync(
        Document document,
        string searchPattern,
        IImmutableSet<string> kinds,
        Func<INavigateToSearchResult, Task> onResultFound,
        CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var onItemFound = GetOnItemFoundCallback(solution, activeDocument: null, (_, i) => onResultFound(i), cancellationToken);

        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var callback = new NavigateToSearchServiceCallback(onItemFound, onProjectCompleted: null);
            // Don't need to sync the full solution when searching a single document.  Just sync the project that doc is in.
            await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                document.Project,
                (service, solutionInfo, callbackId, cancellationToken) =>
                service.SearchDocumentAsync(solutionInfo, document.Id, searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                callback, cancellationToken).ConfigureAwait(false);

            return;
        }

        await SearchDocumentInCurrentProcessAsync(document, searchPattern, kinds, onItemFound, cancellationToken).ConfigureAwait(false);
    }

    public static Task SearchDocumentInCurrentProcessAsync(Document document, string searchPattern, IImmutableSet<string> kinds, Func<RoslynNavigateToItem, Task> onItemFound, CancellationToken cancellationToken)
    {
        return SearchProjectInCurrentProcessAsync(
            document.Project, priorityDocuments: [], document, searchPattern, kinds,
            onItemFound, () => Task.CompletedTask, cancellationToken);
    }

    public async Task SearchProjectsAsync(
        Solution solution,
        ImmutableArray<Project> projects,
        ImmutableArray<Document> priorityDocuments,
        string searchPattern,
        IImmutableSet<string> kinds,
        Document? activeDocument,
        Func<Project, INavigateToSearchResult, Task> onResultFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Contract.ThrowIfTrue(projects.IsEmpty);
        Contract.ThrowIfTrue(projects.Select(p => p.Language).Distinct().Count() != 1);

        Debug.Assert(priorityDocuments.All(d => projects.Contains(d.Project)));
        var onItemFound = GetOnItemFoundCallback(solution, activeDocument, onResultFound, cancellationToken);

        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var priorityDocumentIds = priorityDocuments.SelectAsArray(d => d.Id);
            var callback = new NavigateToSearchServiceCallback(onItemFound, onProjectCompleted);

            await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                // Intentionally sync the full solution.   When SearchProjectAsync is called, we're searching all
                // projects (just in parallel).  So best for them all to sync and share a single solution snapshot
                // on the oop side.
                solution,
                (service, solutionInfo, callbackId, cancellationToken) =>
                    service.SearchProjectsAsync(solutionInfo, projects.SelectAsArray(p => p.Id), priorityDocumentIds, searchPattern, kinds.ToImmutableArray(), callbackId, cancellationToken),
                callback, cancellationToken).ConfigureAwait(false);

            return;
        }

        await SearchProjectsInCurrentProcessAsync(
            projects, priorityDocuments, searchPattern, kinds, onItemFound, onProjectCompleted, cancellationToken).ConfigureAwait(false);
    }

    public static async Task SearchProjectsInCurrentProcessAsync(
        ImmutableArray<Project> projects,
        ImmutableArray<Document> priorityDocuments,
        string searchPattern,
        IImmutableSet<string> kinds,
        Func<RoslynNavigateToItem, Task> onItemFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var _1 = GetPooledHashSet(priorityDocuments.Select(d => d.Project), out var highPriProjects);
        using var _2 = GetPooledHashSet(projects.Where(p => !highPriProjects.Contains(p)), out var lowPriProjects);

        Debug.Assert(projects.SetEquals(highPriProjects.Concat(lowPriProjects)));

        await ProcessProjectsAsync(highPriProjects).ConfigureAwait(false);
        await ProcessProjectsAsync(lowPriProjects).ConfigureAwait(false);

        return;

        async Task ProcessProjectsAsync(HashSet<Project> projects)
        {
            using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

            foreach (var project in projects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                tasks.Add(SearchProjectInCurrentProcessAsync(
                    project, priorityDocuments.WhereAsArray(d => d.Project == project), searchDocument: null,
                    searchPattern, kinds, onItemFound, onProjectCompleted, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
