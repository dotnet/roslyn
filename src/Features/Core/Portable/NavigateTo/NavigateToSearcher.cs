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
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    [Flags]
    internal enum NavigateToSearchScope
    {
        RegularDocuments = 0b01,
        GeneratedDocuments = 0b10,
        AllDocuments = RegularDocuments | GeneratedDocuments
    }

    internal class NavigateToSearcher
    {
        private readonly INavigateToSearcherHost _host;
        private readonly Solution _solution;
        private readonly INavigateToSearchCallback _callback;
        private readonly string _searchPattern;
        private readonly IImmutableSet<string> _kinds;
        private readonly IAsynchronousOperationListener _listener;
        private readonly IStreamingProgressTracker _progress_doNotAccessDirectly;

        private readonly Document? _activeDocument;
        private readonly ImmutableArray<Document> _visibleDocuments;

        private int _remainingProgressItems;

        private NavigateToSearcher(
            INavigateToSearcherHost host,
            Solution solution,
            INavigateToSearchCallback callback,
            string searchPattern,
            IImmutableSet<string> kinds,
            IAsynchronousOperationListener listener)
        {
            _host = host;
            _solution = solution;
            _callback = callback;
            _searchPattern = searchPattern;
            _kinds = kinds;
            _listener = listener;
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
            return Create(solution, asyncListener, callback, searchPattern, kinds, host);
        }

        public static NavigateToSearcher Create(
            Solution solution,
            IAsynchronousOperationListener asyncListener,
            INavigateToSearchCallback callback,
            string searchPattern,
            IImmutableSet<string> kinds,
            INavigateToSearcherHost host)
        {
            return new NavigateToSearcher(host, solution, callback, searchPattern, kinds, asyncListener);
        }

        private async Task AddProgressItemsAsync(int count, CancellationToken cancellationToken)
        {
            Debug.Assert(count >= 0);
            Debug.Assert(_remainingProgressItems >= 0);
            Interlocked.Add(ref _remainingProgressItems, count);
            await _progress_doNotAccessDirectly.AddItemsAsync(count, cancellationToken).ConfigureAwait(false);
        }

        private async Task ProgressItemsCompletedAsync(int count, CancellationToken cancellationToken)
        {
            var newValue = Interlocked.Add(ref _remainingProgressItems, -count);
            Debug.Assert(newValue >= 0);
            await _progress_doNotAccessDirectly.ItemsCompletedAsync(count, cancellationToken).ConfigureAwait(false);
        }

        public Task SearchAsync(bool searchCurrentDocument, CancellationToken cancellationToken)
            => SearchAsync(searchCurrentDocument, NavigateToSearchScope.AllDocuments, cancellationToken);

        public async Task SearchAsync(
            bool searchCurrentDocument,
            NavigateToSearchScope scope,
            CancellationToken cancellationToken)
        {
            var isFullyLoaded = true;

            try
            {
                using var navigateToSearch = Logger.LogBlock(FunctionId.NavigateTo_Search, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken);

                if (searchCurrentDocument)
                {
                    await SearchCurrentDocumentAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // We consider ourselves fully loaded when both the project system has completed loaded us, and we've
                    // totally hydrated the oop side.  Until that happens, we'll attempt to return cached data from languages
                    // that support that.
                    isFullyLoaded = await _host.IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false);

                    // Let the UI know if we're not fully loaded (and then might be reporting cached results).
                    if (!isFullyLoaded)
                        _callback.ReportIncomplete();

                    await SearchAllProjectsAsync(isFullyLoaded, scope, cancellationToken).ConfigureAwait(false);
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
                r => _callback.AddItemAsync(project, r, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        private INavigateToSearchService GetNavigateToSearchService(Project project)
            => _host.GetNavigateToSearchService(project) ?? NoOpNavigateToSearchService.Instance;

        private async Task SearchAllProjectsAsync(
            bool isFullyLoaded,
            NavigateToSearchScope scope,
            CancellationToken cancellationToken)
        {
            var seenItems = new HashSet<INavigateToSearchResult>(NavigateToSearchResultComparer.Instance);
            var orderedProjects = GetOrderedProjectsToProcess();

            var searchRegularDocuments = scope.HasFlag(NavigateToSearchScope.RegularDocuments);
            var searchGeneratedDocuments = scope.HasFlag(NavigateToSearchScope.GeneratedDocuments);
            Debug.Assert(searchRegularDocuments || searchGeneratedDocuments);

            var projectCount = orderedProjects.Sum(g => g.Length);

            if (isFullyLoaded)
            {
                // We're potentially about to make many calls over to our OOP service to perform searches.  Ensure the
                // solution we're searching stays pinned between us and it while this is happening.
                using var _ = RemoteKeepAliveSession.Create(_solution, _listener);

                // We may do up to two passes.  One for loaded docs.  One for source generated docs.
                await AddProgressItemsAsync(
                    projectCount * ((searchRegularDocuments ? 1 : 0) + (searchGeneratedDocuments ? 1 : 0)),
                    cancellationToken).ConfigureAwait(false);

                if (searchRegularDocuments)
                    await SearchFullyLoadedProjectsAsync(orderedProjects, seenItems, cancellationToken).ConfigureAwait(false);

                if (searchGeneratedDocuments)
                    await SearchGeneratedDocumentsAsync(seenItems, cancellationToken).ConfigureAwait(false);
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
                result.Add(ImmutableArray.Create(_activeDocument.Project));
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
            return result.ToImmutable();
        }

        private async Task ProcessOrderedProjectsAsync(
            bool parallel,
            ImmutableArray<ImmutableArray<Project>> orderedProjects,
            HashSet<INavigateToSearchResult> seenItems,
            Func<INavigateToSearchService, ImmutableArray<Project>, Func<Project, INavigateToSearchResult, Task>, Func<Task>, Task> processProjectAsync,
            CancellationToken cancellationToken)
        {
            // Process each group one at a time.  However, in each group process all projects in parallel to get results
            // as quickly as possible.  The net effect of this is that we will search the active doc immediately, then
            // the open docs in parallel, then the rest of the projects after that.  Because the active/open docs should
            // be a far smaller set, those results should come in almost immediately in a prioritized fashion, with the
            // rest of the results following soon after as best as we can find them.
            foreach (var projectGroup in orderedProjects)
            {
                var groups = projectGroup.GroupBy(GetNavigateToSearchService);

                if (!parallel)
                {
                    foreach (var group in groups)
                        await SearchCoreAsync(group).ConfigureAwait(false);
                }
                else
                {
                    var allTasks = groups.Select(SearchCoreAsync);
                    await Task.WhenAll(allTasks).ConfigureAwait(false);

                }
            }

            return;

            async Task SearchCoreAsync(IGrouping<INavigateToSearchService, Project> grouping)
            {
                await Task.Yield();

                var searchService = grouping.Key;
                await processProjectAsync(
                    searchService,
                    grouping.ToImmutableArray(),
                    (project, result) =>
                    {
                        // If we're seeing a dupe in another project, then filter it out here.  The results from
                        // the individual projects will already contain the information about all the projects
                        // leading to a better condensed view that doesn't look like it contains duplicate info.
                        lock (seenItems)
                        {
                            if (!seenItems.Add(result))
                                return Task.CompletedTask;
                        }

                        return _callback.AddItemAsync(project, result, cancellationToken);
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
                async (service, projects, onItemFound, onProjectCompleted) =>
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
                            onItemFound, onProjectCompleted, cancellationToken).ConfigureAwait(false);
                    }
                },
                cancellationToken);
        }

        private Task SearchGeneratedDocumentsAsync(
            HashSet<INavigateToSearchResult> seenItems,
            CancellationToken cancellationToken)
        {
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
            // Note the projects in each 'dependency set' are already sorted in topological order.  So they will process
            // in the desired order if we process serially.
            var allProjects = _solution
                .GetProjectDependencyGraph()
                .GetDependencySets(cancellationToken)
                .SelectAsArray(s => s.SelectAsArray(_solution.GetRequiredProject));

            return ProcessOrderedProjectsAsync(
                parallel: false,
                allProjects,
                seenItems,
                async (service, projects, onItemFound, onProjectCompleted) =>
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
                            _solution, projects, _searchPattern, _kinds, _activeDocument, onItemFound, onProjectCompleted, cancellationToken).ConfigureAwait(false);
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

            public Task SearchDocumentAsync(Document document, string searchPattern, IImmutableSet<string> kinds, Func<INavigateToSearchResult, Task> onResultFound, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public async Task SearchProjectsAsync(Solution solution, ImmutableArray<Project> projects, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, Document? activeDocument, Func<Project, INavigateToSearchResult, Task> onResultFound, Func<Task> onProjectCompleted, CancellationToken cancellationToken)
            {
                foreach (var _ in projects)
                    await onProjectCompleted().ConfigureAwait(false);
            }
        }
    }
}
