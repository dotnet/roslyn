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
        private readonly bool _searchCurrentDocument;
        private readonly IImmutableSet<string> _kinds;
        private readonly Document? _currentDocument;
        private readonly IStreamingProgressTracker _progress;

        private readonly Document? _activeDocument;
        private readonly ImmutableArray<Document> _visibleDocuments;

        private NavigateToSearcher(
            INavigateToSearcherHost host,
            Solution solution,
            IAsynchronousOperationListener asyncListener,
            INavigateToSearchCallback callback,
            string searchPattern,
            bool searchCurrentDocument,
            IImmutableSet<string> kinds)
        {
            _host = host;
            _solution = solution;
            _asyncListener = asyncListener;
            _callback = callback;
            _searchPattern = searchPattern;
            _searchCurrentDocument = searchCurrentDocument;
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

            if (_searchCurrentDocument)
            {
                _currentDocument = _activeDocument;
            }
        }

        public static NavigateToSearcher Create(
            Solution solution,
            IAsynchronousOperationListener asyncListener,
            INavigateToSearchCallback callback,
            string searchPattern,
            bool searchCurrentDocument,
            IImmutableSet<string> kinds,
            CancellationToken disposalToken,
            INavigateToSearcherHost? host = null)
        {
            host ??= new DefaultNavigateToSearchHost(solution, asyncListener, disposalToken);
            return new NavigateToSearcher(host, solution, asyncListener, callback, searchPattern, searchCurrentDocument, kinds);
        }

        internal async Task SearchAsync(CancellationToken cancellationToken)
        {
            var isFullyLoaded = true;

            try
            {
                using var navigateToSearch = Logger.LogBlock(FunctionId.NavigateTo_Search, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken);
                using var asyncToken = _asyncListener.BeginAsyncOperation(GetType() + ".Search");

                // We consider ourselves fully loaded when both the project system has completed loaded us, and we've
                // totally hydrated the oop side.  Until that happens, we'll attempt to return cached data from languages
                // that support that.
                isFullyLoaded = await _host.IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false);
                await SearchAllProjectsAsync(isFullyLoaded, cancellationToken).ConfigureAwait(false);
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

        private async Task SearchAllProjectsAsync(bool isFullyLoaded, CancellationToken cancellationToken)
        {
            var orderedProjects = GetOrderedProjectsToProcess();
            var (itemsReported, projectResults) = await ProcessProjectsAsync(orderedProjects, isFullyLoaded, cancellationToken).ConfigureAwait(false);

            // If we're fully loaded then we're done at this point.  All the searches would have been against the latest
            // computed data and we don't need to do anything else.
            if (isFullyLoaded)
                return;

            // We weren't fully loaded *but* we reported some items to the user, then consider that good enough for now.
            // The user will have some results they can use, and (in the case that we actually examined the cache for
            // data) we will tell the user that the results may be incomplete/inaccurate and they should try again soon.
            if (itemsReported > 0)
                return;

            // We didn't have any items reported *and* we weren't fully loaded.  If it turns out that some of our
            // projects were using cached data then we can try searching them again, but this tell them to use the
            // latest data.  The ensures the user at least gets some result instead of nothing.
            var projectsUsingCache = projectResults.SelectAsArray(t => t.location == NavigateToSearchLocation.Cache, t => t.project);
            await ProcessProjectsAsync(ImmutableArray.Create(projectsUsingCache), isFullyLoaded: true, cancellationToken).ConfigureAwait(false);
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
            // If we're only searching the current doc, we don't need to examine anything else but that.
            if (_searchCurrentDocument)
            {
                // Note: _currentDocument may still be null.  Just because the user asked to search current document
                // doesn't mean we were able to map the view to an active doc inside Roslyn.  In this case, we just
                // don't search anything.
                var project = _currentDocument?.Project;
                return project == null
                    ? ImmutableArray<ImmutableArray<Project>>.Empty
                    : ImmutableArray.Create(ImmutableArray.Create(project));
            }

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

        private async Task<(int itemsReported, ImmutableArray<(Project project, NavigateToSearchLocation location)>)> ProcessProjectsAsync(
            ImmutableArray<ImmutableArray<Project>> orderedProjects, bool isFullyLoaded, CancellationToken cancellationToken)
        {
            await _progress.AddItemsAsync(orderedProjects.Sum(p => p.Length), cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<(Project project, NavigateToSearchLocation location)>.GetInstance(out var result);

            var seenItems = new HashSet<INavigateToSearchResult>(NavigateToSearchResultComparer.Instance);
            foreach (var projectGroup in orderedProjects)
                result.AddRange(await Task.WhenAll(projectGroup.Select(p => Task.Run(() => SearchAsync(p, isFullyLoaded, seenItems, cancellationToken)))).ConfigureAwait(false));

            return (seenItems.Count, result.ToImmutable());
        }

        private async Task<(Project project, NavigateToSearchLocation location)> SearchAsync(
            Project project, bool isFullyLoaded, HashSet<INavigateToSearchResult> seenItems, CancellationToken cancellationToken)
        {
            try
            {
                var location = await SearchCoreAsync(project, isFullyLoaded, seenItems, cancellationToken).ConfigureAwait(false);
                return (project, location);
            }
            finally
            {
                await _progress.ItemCompletedAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<NavigateToSearchLocation> SearchCoreAsync(
            Project project, bool isFullyLoaded, HashSet<INavigateToSearchResult> seenItems, CancellationToken cancellationToken)
        {
            // If they don't even support the service, then always show them as having done the
            // complete search.  That way we don't call back into this project ever.
            var service = _host.GetNavigateToSearchService(project);
            if (service == null)
                return NavigateToSearchLocation.Latest;

            if (_searchCurrentDocument)
            {
                Contract.ThrowIfNull(_currentDocument);
                return await service.SearchDocumentAsync(
                    _currentDocument, _searchPattern, _kinds, OnResultFound, isFullyLoaded, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await service.SearchProjectAsync(
                    project, GetPriorityDocuments(project), _searchPattern, _kinds, OnResultFound, isFullyLoaded, cancellationToken).ConfigureAwait(false);
            }

            Task OnResultFound(INavigateToSearchResult result)
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
            }
        }
    }
}
