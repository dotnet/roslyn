// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal class NavigateToSearcher
    {
        private readonly INavigateToSearcherHost _host;
        private readonly Solution _solution;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly INavigateToSearchCallback _callback;
        private readonly string _searchPattern;
        private readonly IImmutableSet<string> _kinds;
        private readonly IStreamingProgressTracker _progress;

        private readonly Document? _activeDocument;
        private readonly ImmutableArray<Document> _visibleDocuments;

        private NavigateToSearcher(
            INavigateToSearcherHost host,
            Solution solution,
            IAsynchronousOperationListener asyncListener,
            INavigateToSearchCallback callback,
            string searchPattern,
            IImmutableSet<string> kinds)
        {
            _host = host;
            _solution = solution;
            _asyncListener = asyncListener;
            _callback = callback;
            _searchPattern = searchPattern;
            _kinds = kinds;
            _progress = new StreamingProgressTracker((current, maximum, ct) =>
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
            return new NavigateToSearcher(host, solution, asyncListener, callback, searchPattern, kinds);
        }

        internal async Task SearchAsync(bool searchCurrentDocument, CancellationToken cancellationToken)
        {
            var isFullyLoaded = true;

            try
            {
                using var navigateToSearch = Logger.LogBlock(FunctionId.NavigateTo_Search, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken);
                using var asyncToken = _asyncListener.BeginAsyncOperation(GetType() + ".Search");

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
                    await SearchAllProjectsAsync(isFullyLoaded, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                // providing this extra information will make UI to show indication to users
                // that result might not contain full data
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

            await _progress.AddItemsAsync(1, cancellationToken).ConfigureAwait(false);
            try
            {
                await service.SearchDocumentAsync(
                    _activeDocument, _searchPattern, _kinds,
                    r => _callback.AddItemAsync(project, r, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await _progress.ItemCompletedAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SearchAllProjectsAsync(bool isFullyLoaded, CancellationToken cancellationToken)
        {
            var seenItems = new HashSet<INavigateToSearchResult>(NavigateToSearchResultComparer.Instance);
            var orderedProjects = GetOrderedProjectsToProcess();
            var searchGeneratedDocuments = isFullyLoaded;

            var projectCount = orderedProjects.Sum(g => g.Length);

            if (!isFullyLoaded)
            {
                // If we're not fully loaded, do a quick search through any cached data we have.
                // If we don't find any results, load all the projects and search through them.
                await _progress.AddItemsAsync(projectCount * 2, cancellationToken).ConfigureAwait(false);

                await SearchCachedDocumentsAsync(orderedProjects, seenItems, cancellationToken).ConfigureAwait(false);

                // If searching cached data returned any results, then we're done.  We've at least shown some results
                // to the user.  That will hopefully serve them well enough until the solution fully loads.
                if (seenItems.Count > 0)
                    return;

                await SearchFullyLoadedProjectsAsync(orderedProjects, seenItems, cancellationToken).ConfigureAwait(false);

                // Report a telemetry even to track if we found uncached items after failing to find cached items.
                // In practice if we see that we are always finding uncached items, then it's likely something
                // has broken in the caching system since we would expect to normally find values there.  Specifically
                // we expect: foundFullItems <<< not foundFullItems.
                Logger.Log(FunctionId.NavigateTo_CacheItemsMiss, KeyValueLogMessage.Create(m => m["FoundFullItems"] = seenItems.Count > 0));
            }
            else
            {
                // If we're fully loaded, do a full search through all projects.
                // Then do a full search through all generated documents.
                await _progress.AddItemsAsync(projectCount * 2, cancellationToken).ConfigureAwait(false);

                await SearchFullyLoadedProjectsAsync(orderedProjects, seenItems, cancellationToken).ConfigureAwait(false);
                await SearchGeneratedDocumentsAsync(orderedProjects, seenItems, cancellationToken).ConfigureAwait(false);
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

        private async Task SearchOrderedProjectsAsync(
            ImmutableArray<ImmutableArray<Project>> orderedProjects,
            HashSet<INavigateToSearchResult> seenItems,
            Func<INavigateToSearchService, Project, Func<INavigateToSearchResult, Task>, Task> searchProjectAsync,
            CancellationToken cancellationToken)
        {
            // Process each group one at a time.  However, in each group process all projects in parallel to get results
            // as quickly as possible.  The net effect of this is that we will search the active doc immediately, then
            // the open docs in parallel, then the rest of the projects after that.  Because the active/open docs should
            // be a far smaller set, those results should come in almost immediately in a prioritized fashion, with the
            // rest of the results following soon after as best as we can find them.
            foreach (var projectGroup in orderedProjects)
            {
                var allTasks = projectGroup.Select(p => Task.Run(() => SearchCoreAsync(p)));
                await Task.WhenAll(allTasks).ConfigureAwait(false);
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

                    var onResultFound = GetOnResultFound(project, seenItems, cancellationToken);
                    await searchProjectAsync(service, project, onResultFound).ConfigureAwait(false);
                }
                finally
                {
                    // after each project is searched, increment our progress.
                    await _progress.ItemCompletedAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private Task SearchFullyLoadedProjectsAsync(
            ImmutableArray<ImmutableArray<Project>> orderedProjects,
            HashSet<INavigateToSearchResult> seenItems,
            CancellationToken cancellationToken)
        {
            return SearchOrderedProjectsAsync(
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
            return SearchOrderedProjectsAsync(
                orderedProjects,
                seenItems,
                (s, p, cb) => s.SearchCachedDocumentsAsync(p, GetPriorityDocuments(p), _searchPattern, _kinds, cb, cancellationToken),
                cancellationToken);
        }

        private Task SearchGeneratedDocumentsAsync(
            ImmutableArray<ImmutableArray<Project>> orderedProjects,
            HashSet<INavigateToSearchResult> seenItems,
            CancellationToken cancellationToken)
        {
            return SearchOrderedProjectsAsync(
                orderedProjects,
                seenItems,
                (s, p, cb) => s.SearchGeneratedDocumentsAsync(p, _searchPattern, _kinds, cb, cancellationToken),
                cancellationToken);
        }

        private Func<INavigateToSearchResult, Task> GetOnResultFound(
            Project project, HashSet<INavigateToSearchResult> seenItems, CancellationToken cancellationToken)
        {
            return result =>
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
            };
        }
    }
}
