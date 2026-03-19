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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo;

[Flags]
internal enum NavigateToDocumentSupport
{
    RegularDocuments = 0b01,
    GeneratedDocuments = 0b10,
    AllDocuments = RegularDocuments | GeneratedDocuments
}

internal enum NavigateToSearchScope
{
    // Intentionally first so that no value indicates searching the entire solution.
    Solution,
    Project,
    Document,
}

internal sealed class NavigateToSearcher
{
    private static readonly ObjectPool<HashSet<INavigateToSearchResult>> s_searchResultPool = new(() => new(NavigateToSearchResultComparer.Instance));

    private readonly INavigateToSearcherHost _host;
    private readonly Solution _solution;
    private readonly INavigateToSearchCallback _callback;
    private readonly string _searchPattern;
    private readonly IImmutableSet<string> _kinds;
    private readonly IStreamingProgressTracker _progress_doNotAccessDirectly;

    private readonly Document? _activeDocument;
    private readonly ImmutableArray<Document> _visibleDocuments;

    private int _remainingProgressItems;

    private NavigateToSearcher(
        INavigateToSearcherHost host,
        Solution solution,
        INavigateToSearchCallback callback,
        string searchPattern,
        IImmutableSet<string> kinds)
    {
        _host = host;
        _solution = solution;
        _callback = callback;
        _searchPattern = searchPattern;
        _kinds = kinds;
        _progress_doNotAccessDirectly = new StreamingProgressTracker((current, maximum, ct) =>
        {
            callback.ReportProgress(current, maximum);
            return new ValueTask();
        });

        var docTrackingService = _solution.Services.GetRequiredService<IDocumentTrackingService>();

        // If the workspace is tracking documents, use that to prioritize our search
        // order.  That way we provide results for the documents the user is working
        // on faster than the rest of the solution.
        _activeDocument = docTrackingService.GetActiveDocument(_solution);
        _visibleDocuments = docTrackingService.GetVisibleDocuments(_solution)
                                              .WhereAsArray(d => d != _activeDocument);
    }

#pragma warning disable CA1200 // Avoid using cref tags with a prefix
    /// <summary>
    /// Creates a searcher using the default host.
    /// </summary>
    /// <param name="disposalToken">Disposal token normally provided by <see
    /// cref="T:Microsoft.CodeAnalysis.Editor.Shared.Utilities.IThreadingContext.DisposalToken"/>.  Used to control
    /// the lifetime of internal async work within the default host.</param>
    public static NavigateToSearcher Create(
#pragma warning restore CA1200 // Avoid using cref tags with a prefix
        Solution solution,
        IAsynchronousOperationListener asyncListener,
        INavigateToSearchCallback callback,
        string searchPattern,
        IImmutableSet<string> kinds,
        CancellationToken disposalToken)
    {
        var host = new DefaultNavigateToSearchHost(solution, asyncListener, disposalToken);
        return Create(solution, callback, searchPattern, kinds, host);
    }

    public static NavigateToSearcher Create(
        Solution solution,
        INavigateToSearchCallback callback,
        string searchPattern,
        IImmutableSet<string> kinds,
        INavigateToSearcherHost host)
    {
        return new NavigateToSearcher(host, solution, callback, searchPattern, kinds);
    }

    private async Task AddProgressItemsAsync(int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Debug.Assert(count >= 0);
        Debug.Assert(_remainingProgressItems >= 0);
        Interlocked.Add(ref _remainingProgressItems, count);
        await _progress_doNotAccessDirectly.AddItemsAsync(count, cancellationToken).ConfigureAwait(false);
    }

    private async Task ProgressItemsCompletedAsync(int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var newValue = Interlocked.Add(ref _remainingProgressItems, -count);
        Debug.Assert(newValue >= 0);
        await _progress_doNotAccessDirectly.ItemsCompletedAsync(count, cancellationToken).ConfigureAwait(false);
    }

    public Task SearchAsync(NavigateToSearchScope searchScope, CancellationToken cancellationToken)
        => SearchAsync(searchScope, NavigateToDocumentSupport.AllDocuments, cancellationToken);

    public async Task SearchAsync(
        NavigateToSearchScope searchScope,
        NavigateToDocumentSupport documentSupport,
        CancellationToken cancellationToken)
    {
        var isFullyLoaded = true;

        try
        {
            using var navigateToSearch = Logger.LogBlock(FunctionId.NavigateTo_Search, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken);

            switch (searchScope)
            {
                case NavigateToSearchScope.Document:
                    await SearchCurrentDocumentAsync(cancellationToken).ConfigureAwait(false);
                    return;

                case NavigateToSearchScope.Project:
                    await SearchCurrentProjectAsync(documentSupport, cancellationToken).ConfigureAwait(false);
                    return;

                case NavigateToSearchScope.Solution:
                    // We consider ourselves fully loaded when both the project system has completed loaded us, and we've
                    // totally hydrated the oop side.  Until that happens, we'll attempt to return cached data from languages
                    // that support that.
                    isFullyLoaded = await _host.IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false);

                    // Let the UI know if we're not fully loaded (and then might be reporting cached results).
                    if (!isFullyLoaded)
                        _callback.ReportIncomplete();

                    await SearchAllProjectsAsync(isFullyLoaded, documentSupport, cancellationToken).ConfigureAwait(false);
                    return;

                default:
                    throw ExceptionUtilities.UnexpectedValue(searchScope);
            }
        }
        finally
        {
            // Ensure that we actually complete all our remaining progress items so that the progress bar completes.
            await ProgressItemsCompletedAsync(_remainingProgressItems, cancellationToken).ConfigureAwait(false);
            Debug.Assert(_remainingProgressItems == 0);

            // Pass along isFullyLoaded so that the UI can show indication to users that results may be incomplete.
            _callback.Done(isFullyLoaded);
        }
    }

    private async Task SearchCurrentDocumentAsync(CancellationToken cancellationToken)
    {
        if (_activeDocument == null)
            return;

        var project = _activeDocument.Project;
        var service = GetNavigateToSearchService(project);

        await AddProgressItemsAsync(1, cancellationToken).ConfigureAwait(false);
        await service.SearchDocumentAsync(
            _activeDocument, _searchPattern, _kinds,
            r => _callback.AddResultsAsync(r, _activeDocument, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private Task SearchCurrentProjectAsync(
        NavigateToDocumentSupport documentSupport,
        CancellationToken cancellationToken)
    {
        if (_activeDocument == null)
            return Task.CompletedTask;

        var activeProject = _activeDocument.Project;
        return SearchSpecificProjectsAsync(
            // Because we're only searching the current project, it's fine to bring that project fully up to date before
            // searching it.  We only do the work to search cached files when doing the initial load of something huge
            // (the full solution).
            isFullyLoaded: true,
            documentSupport,
            [[activeProject]],
            cancellationToken);
    }

    private INavigateToSearchService GetNavigateToSearchService(Project project)
        => _host.GetNavigateToSearchService(project) ?? NoOpNavigateToSearchService.Instance;

    private async Task SearchAllProjectsAsync(
        bool isFullyLoaded,
        NavigateToDocumentSupport documentSupport,
        CancellationToken cancellationToken)
    {
        var orderedProjects = GetOrderedProjectsToProcess();
        await SearchSpecificProjectsAsync(isFullyLoaded, documentSupport, orderedProjects, cancellationToken).ConfigureAwait(false);
    }

    private async Task SearchSpecificProjectsAsync(
        bool isFullyLoaded,
        NavigateToDocumentSupport documentSupport,
        ImmutableArray<ImmutableArray<Project>> orderedProjects,
        CancellationToken cancellationToken)
    {
        using var _1 = s_searchResultPool.GetPooledObject(out var seenItems);

        var searchRegularDocuments = documentSupport.HasFlag(NavigateToDocumentSupport.RegularDocuments);
        var searchGeneratedDocuments = documentSupport.HasFlag(NavigateToDocumentSupport.GeneratedDocuments);
        Debug.Assert(searchRegularDocuments || searchGeneratedDocuments);

        var projectCount = orderedProjects.Sum(g => g.Length);

        if (isFullyLoaded)
        {
            // We're potentially about to make many calls over to our OOP service to perform searches.  Ensure the
            // solution we're searching stays pinned between us and it while this is happening.
            using var _2 = await RemoteKeepAliveSession.CreateAsync(_solution, cancellationToken).ConfigureAwait(false);

            // We may do up to two passes.  One for loaded docs.  One for source generated docs.
            await AddProgressItemsAsync(
                projectCount * ((searchRegularDocuments ? 1 : 0) + (searchGeneratedDocuments ? 1 : 0)),
                cancellationToken).ConfigureAwait(false);

            if (searchRegularDocuments)
                await SearchFullyLoadedProjectsAsync(orderedProjects, seenItems, cancellationToken).ConfigureAwait(false);

            if (searchGeneratedDocuments)
                await SearchGeneratedDocumentsAsync(orderedProjects, seenItems, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // If we're not fully loaded, we only search regular documents.  Generated documents must wait until
            // we're fully loaded (and thus have all the information necessary to properly run generators).
            if (searchRegularDocuments)
            {
                await AddProgressItemsAsync(projectCount, cancellationToken).ConfigureAwait(false);
                await SearchCachedDocumentsAsync(orderedProjects, seenItems, cancellationToken).ConfigureAwait(false);

                // Note: we only bother searching cached documents during this time.  Telemetry shows no meaningful
                // change if we do a full search after this point.  That prevents us from showing the user a
                // glacially slow progress meter as we load everything and end up finding nothing.
            }
        }
    }

    /// <summary>
    /// Returns a sequence of groups of projects to process.  The sequence is in priority order, and all projects in
    /// a particular group should be processed before the next group.  This allows us to associate CPU resources in
    /// likely areas the user wants, while also still allowing for good parallelization.  Specifically, we consider
    /// the active-document the most important to get results for, as some users use navigate-to to navigate within
    /// the doc they are editing.  So we want those results to appear as quick as possible, without the search for
    /// them contending with the searches for other projects for CPU time.
    /// </summary>
    private ImmutableArray<ImmutableArray<Project>> GetOrderedProjectsToProcess()
    {
        using var result = TemporaryArray<ImmutableArray<Project>>.Empty;

        using var _ = PooledHashSet<Project>.GetInstance(out var processedProjects);

        // First, if there's an active document, search that project first, prioritizing that active document and
        // all visible documents from it.
        if (_activeDocument != null)
        {
            processedProjects.Add(_activeDocument.Project);
            result.Add([_activeDocument.Project]);
        }

        // Next process all visible docs that were not from the active project.
        using var buffer = TemporaryArray<Project>.Empty;
        foreach (var doc in _visibleDocuments)
        {
            if (processedProjects.Add(doc.Project))
                buffer.Add(doc.Project);
        }

        if (buffer.Count > 0)
            result.Add(buffer.ToImmutableAndClear());

        // Finally, process the remainder of projects
        foreach (var project in _solution.Projects)
        {
            if (processedProjects.Add(project))
                buffer.Add(project);
        }

        if (buffer.Count > 0)
            result.Add(buffer.ToImmutableAndClear());

        return result.ToImmutableAndClear();
    }

    /// <summary>
    /// Given a search within a particular project, this returns any documents within that project that should take
    /// precedence when searching.  This allows results to get to the user more quickly for common cases (like using
    /// nav-to to find results in the file you currently have open
    /// </summary>
    private ImmutableArray<Document> GetPriorityDocuments(ImmutableArray<Project> projects)
    {
        using var _1 = PooledHashSet<Project>.GetInstance(out var projectsSet);
        projectsSet.AddRange(projects);

        using var _2 = ArrayBuilder<Document>.GetInstance(out var result);
        if (_activeDocument?.Project != null && projectsSet.Contains(_activeDocument.Project))
            result.Add(_activeDocument);

        foreach (var doc in _visibleDocuments)
        {
            if (projectsSet.Contains(doc.Project))
                result.Add(doc);
        }

        result.RemoveDuplicates();
        return result.ToImmutableAndClear();
    }

    private async Task ProcessOrderedProjectsAsync(
        bool parallel,
        ImmutableArray<ImmutableArray<Project>> orderedProjects,
        HashSet<INavigateToSearchResult> seenResults,
        Func<INavigateToSearchService, ImmutableArray<Project>, Func<ImmutableArray<INavigateToSearchResult>, Task>, Func<Task>, Task> processProjectAsync,
        CancellationToken cancellationToken)
    {
        // Process each group one at a time.  However, in each group process all projects in parallel to get results
        // as quickly as possible.  The net effect of this is that we will search the active doc immediately, then
        // the open docs in parallel, then the rest of the projects after that.  Because the active/open docs should
        // be a far smaller set, those results should come in almost immediately in a prioritized fashion, with the
        // rest of the results following soon after as best as we can find them.
        foreach (var projectGroup in orderedProjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var groups = projectGroup.GroupBy(GetNavigateToSearchService);

            if (!parallel)
            {
                foreach (var group in groups)
                    await SearchCoreAsync(group, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await Parallel.ForEachAsync(
                    source: groups,
                    cancellationToken,
                    SearchCoreAsync).ConfigureAwait(false);
            }
        }

        return;

        async ValueTask SearchCoreAsync(IGrouping<INavigateToSearchService, Project> grouping, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var searchService = grouping.Key;
            await processProjectAsync(
                searchService,
                [.. grouping],
                async results =>
                {
                    using var _ = ArrayBuilder<INavigateToSearchResult>.GetInstance(results.Length, out var nonDuplicates);

                    // If we're seeing a dupe in another project, then filter it out here.  The results from
                    // the individual projects will already contain the information about all the projects
                    // leading to a better condensed view that doesn't look like it contains duplicate info.
                    lock (seenResults)
                    {
                        foreach (var result in results)
                        {
                            if (seenResults.Add(result))
                                nonDuplicates.Add(result);
                        }
                    }

                    if (nonDuplicates.Count > 0)
                    {
                        await _callback.AddResultsAsync(
                            nonDuplicates.ToImmutableAndClear(), _activeDocument, cancellationToken).ConfigureAwait(false);
                    }
                },
                () => this.ProgressItemsCompletedAsync(count: 1, cancellationToken)).ConfigureAwait(false);
        }
    }

    private Task SearchFullyLoadedProjectsAsync(
        ImmutableArray<ImmutableArray<Project>> orderedProjects,
        HashSet<INavigateToSearchResult> seenItems,
        CancellationToken cancellationToken)
    {
        // Search the fully loaded project in parallel.  We know this will be called after we've already hydrated the 
        // oop side.  So all calls will immediately see the solution as ready on the other end, and can start checking
        // all the docs it has.  Most docs will then find a hit in the index and can return results immediately.  Docs
        // that are not in the cache can be rescanned and have their new index contents checked.
        return ProcessOrderedProjectsAsync(
            parallel: true,
            orderedProjects,
            seenItems,
            (s, ps, cb1, cb2) => s.SearchProjectsAsync(
                _solution, ps, GetPriorityDocuments(ps), _searchPattern, _kinds, _activeDocument, cb1, cb2, cancellationToken),
            cancellationToken);
    }

    private Task SearchCachedDocumentsAsync(
        ImmutableArray<ImmutableArray<Project>> orderedProjects,
        HashSet<INavigateToSearchResult> seenItems,
        CancellationToken cancellationToken)
    {
        // We search cached information in parallel.  This is because there's no syncing step when searching cached
        // docs.  As such, we can just send a request for all projects in parallel to our OOP host and have it read
        // and search the local DB easily.  The DB can easily scale to feed all the threads trying to read from it
        // and we can get high throughput just processing everything in parallel.
        return ProcessOrderedProjectsAsync(
            parallel: true,
            orderedProjects,
            seenItems,
            async (service, projects, onResultsFound, onProjectCompleted) =>
            {
                // if the language doesn't support searching cached docs, immediately transition the project to the
                // completed state.
                if (service is not IAdvancedNavigateToSearchService advancedService)
                {
                    foreach (var project in projects)
                        await onProjectCompleted().ConfigureAwait(false);
                }
                else
                {
                    await advancedService.SearchCachedDocumentsAsync(
                        _solution, projects, GetPriorityDocuments(projects), _searchPattern, _kinds, _activeDocument,
                        onResultsFound, onProjectCompleted, cancellationToken).ConfigureAwait(false);
                }
            },
            cancellationToken);
    }

    private Task SearchGeneratedDocumentsAsync(
        ImmutableArray<ImmutableArray<Project>> orderedProjects,
        HashSet<INavigateToSearchResult> seenItems,
        CancellationToken cancellationToken)
    {
        using var _ = PooledHashSet<ProjectId>.GetInstance(out var allProjectIdSet);
        allProjectIdSet.AddRange(orderedProjects.SelectMany(x => x).Select(p => p.Id));

        // Process all projects, serially, in topological order.  Generating source can be expensive.  It requires
        // creating and processing the entire compilation for a project, which itself may require dependent
        // compilations as references.  These dependents might also be skeleton references in the case of cross
        // language projects.
        //
        // As such, we always want to compute the information for one project before moving onto a project that
        // depends on it.  That way information is available as soon as possible, and then computation for it
        // immediately benefits what comes next.  Importantly, this avoids the problem of picking a project deep in
        // the dependency tree, which then pulls on N other projects, forcing results for this single project to pay
        // that full price (that would be paid when we hit these through a normal topological walk).
        //
        // Note: the projects in each 'dependency set' are already sorted in topological order.  So they will process in
        // the desired order if we process serially.
        //
        // Note: we should only process the projects that are in the ordered-list of projects the searcher is searching
        // as a whole.
        var filteredProjects = _solution
            .GetProjectDependencyGraph()
            .GetDependencySets(cancellationToken)
            .SelectAsArray(projectIdSet =>
                projectIdSet.SelectAsArray(
                    predicate: id => allProjectIdSet.Contains(id),
                    selector: id => _solution.GetRequiredProject(id)));

        Contract.ThrowIfFalse(orderedProjects.SelectMany(s => s).Count() == filteredProjects.SelectMany(s => s).Count());

        return ProcessOrderedProjectsAsync(
            parallel: false,
            filteredProjects,
            seenItems,
            async (service, projects, onResultsFound, onProjectCompleted) =>
            {
                // if the language doesn't support searching generated docs, immediately transition the project to the
                // completed state.
                if (service is not IAdvancedNavigateToSearchService advancedService)
                {
                    foreach (var project in projects)
                        await onProjectCompleted().ConfigureAwait(false);
                }
                else
                {
                    await advancedService.SearchGeneratedDocumentsAsync(
                        _solution, projects, _searchPattern, _kinds, _activeDocument, onResultsFound, onProjectCompleted, cancellationToken).ConfigureAwait(false);
                }
            },
            cancellationToken);
    }

    private sealed class NoOpNavigateToSearchService : INavigateToSearchService
    {
        public static readonly INavigateToSearchService Instance = new NoOpNavigateToSearchService();

        private NoOpNavigateToSearchService()
        {
        }

        public IImmutableSet<string> KindsProvided
            => ImmutableHashSet<string>.Empty;

        public bool CanFilter
            => false;

        public Task SearchDocumentAsync(Document document, string searchPattern, IImmutableSet<string> kinds, Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public async Task SearchProjectsAsync(Solution solution, ImmutableArray<Project> projects, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, Document? activeDocument, Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound, Func<Task> onProjectCompleted, CancellationToken cancellationToken)
        {
            foreach (var _ in projects)
                await onProjectCompleted().ConfigureAwait(false);
        }
    }
}
