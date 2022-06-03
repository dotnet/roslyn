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

            var docTrackingService = _solution.Workspace.Services.GetRequiredService<IDocumentTrackingService>();

            // If the workspace is tracking documents, use that to prioritize our search
            // order.  That way we provide results for the documents the user is working
            // on faster than the rest of the solution.
            _activeDocument = docTrackingService.GetActiveDocument(_solution);
            _visibleDocuments = docTrackingService.GetVisibleDocuments(_solution)
                                                  .WhereAsArray(d => d != _activeDocument);
        }

        public static NavigateToSearcher Create(
            Solution solution,
            IAsynchronousOperationListener asyncListener,
            INavigateToSearchCallback callback,
            string searchPattern,
            IImmutableSet<string> kinds,
            CancellationToken disposalToken,
            INavigateToSearcherHost? host = null)
        {
            host ??= new DefaultNavigateToSearchHost(solution, asyncListener, disposalToken);
            return new NavigateToSearcher(host, solution, callback, searchPattern, kinds);
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
            var service = _host.GetNavigateToSearchService(project);
            if (service == null)
                return;

            await AddProgressItemsAsync(1, cancellationToken).ConfigureAwait(false);
            await service.SearchDocumentAsync(
                _activeDocument, _searchPattern, _kinds,
                r => _callback.AddItemAsync(project, r, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

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
                    // We do at least two passes.  One for cached docs.  One for normal docs.
                    await AddProgressItemsAsync(
                        projectCount * 2,
                        cancellationToken).ConfigureAwait(false);

                    await SearchCachedDocumentsAsync(orderedProjects, seenItems, cancellationToken).ConfigureAwait(false);

                    // If searching cached data returned any results, then we're done.  We've at least shown some results
                    // to the user.  That will hopefully serve them well enough until the solution fully loads.
                    if (seenItems.Count > 0)
                        return;

                    await SearchFullyLoadedProjectsAsync(orderedProjects, seenItems, cancellationToken).ConfigureAwait(false);

                    // Report a telemetry event to track if we found uncached items after failing to find cached items.
                    // In practice if we see that we are always finding uncached items, then it's likely something
                    // has broken in the caching system since we would expect to normally find values there.  Specifically
                    // we expect: foundFullItems <<< not foundFullItems.
                    Logger.Log(FunctionId.NavigateTo_CacheItemsMiss, KeyValueLogMessage.Create(m => m["FoundFullItems"] = seenItems.Count > 0));
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
        private ImmutableArray<Document> GetPriorityDocuments(Project project)
        {
            using var _ = ArrayBuilder<Document>.GetInstance(out var result);
            if (_activeDocument?.Project == project)
                result.Add(_activeDocument);

            foreach (var doc in _visibleDocuments)
            {
                if (doc.Project == project)
                    result.Add(doc);
            }

            result.RemoveDuplicates();
            return result.ToImmutable();
        }

        private async Task ProcessOrderedProjectsAsync(
            bool parallel,
            ImmutableArray<ImmutableArray<Project>> orderedProjects,
            HashSet<INavigateToSearchResult> seenItems,
            Func<INavigateToSearchService, Project, Func<INavigateToSearchResult, Task>, Task> processProjectAsync,
            CancellationToken cancellationToken)
        {
            // Process each group one at a time.  However, in each group process all projects in parallel to get results
            // as quickly as possible.  The net effect of this is that we will search the active doc immediately, then
            // the open docs in parallel, then the rest of the projects after that.  Because the active/open docs should
            // be a far smaller set, those results should come in almost immediately in a prioritized fashion, with the
            // rest of the results following soon after as best as we can find them.
            foreach (var projectGroup in orderedProjects)
            {
                if (!parallel)
                {
                    foreach (var project in projectGroup)
                        await SearchCoreAsync(project).ConfigureAwait(false);
                }
                else
                {
                    var allTasks = projectGroup.Select(p => Task.Run(() => SearchCoreAsync(p), cancellationToken));
                    await Task.WhenAll(allTasks).ConfigureAwait(false);
                }
            }

            return;

            async Task SearchCoreAsync(Project project)
            {
                try
                {
                    // If they don't even support the service, then always show them as having done the
                    // complete search.  That way we don't call back into this project ever.
                    var service = _host.GetNavigateToSearchService(project);
                    if (service == null)
                        return;

                    await processProjectAsync(service, project, result =>
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
                    }).ConfigureAwait(false);
                }
                finally
                {
                    // after each project is searched, increment our progress.
                    await ProgressItemsCompletedAsync(count: 1, cancellationToken).ConfigureAwait(false);
                }
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
                (s, p, cb) => s.SearchProjectAsync(p, GetPriorityDocuments(p), _searchPattern, _kinds, cb, cancellationToken),
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
                (s, p, cb) => s.SearchCachedDocumentsAsync(p, GetPriorityDocuments(p), _searchPattern, _kinds, cb, cancellationToken),
                cancellationToken);
        }

        private Task SearchGeneratedDocumentsAsync(
            HashSet<INavigateToSearchResult> seenItems,
            CancellationToken cancellationToken)
        {
            // Process all projects, serially, in topological order.  Generating source can be expensive.  It requires
            // creating and processing the entire compilation for a project, which itself may require dependent compilations
            // as references.  These dependents might also be skeleton references in the case of cross language projects.
            //
            // As such, we always want to compute the information for one project before moving onto a project that depends on
            // it.  That way information is available as soon as possible, and then computation for it immediately benefits 
            // what comes next.  Importantly, this avoids the problem of picking a project deep in the dependency tree, which
            // then pulls on N other projects, forcing results for this single project to pay that full price (that would 
            // be paid when we hit these through a normal topological walk).
            //
            // Note the projects in each 'dependency set' are already sorted in topological order.  So they will process in
            // the desired order if we process serially.
            var allProjects = _solution.GetProjectDependencyGraph()
                                       .GetDependencySets(cancellationToken)
                                       .SelectAsArray(s => s.SelectAsArray(_solution.GetRequiredProject));

            return ProcessOrderedProjectsAsync(
                parallel: false,
                allProjects,
                seenItems,
                (s, p, cb) => s.SearchGeneratedDocumentsAsync(p, _searchPattern, _kinds, cb, cancellationToken),
                cancellationToken);
        }
    }
}
