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
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal partial class NavigateToSearcher
    {
        private readonly Solution _solution;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly INavigateToSearchCallback _callback;
        private readonly string _searchPattern;
        private readonly bool _searchCurrentDocument;
        private readonly IImmutableSet<string> _kinds;
        private readonly Document? _currentDocument;
        private readonly IStreamingProgressTracker _progress;
        private readonly CancellationToken _cancellationToken;

        private readonly Document? _activeDocument;
        private readonly ImmutableArray<Document> _visibleDocuments;

        public NavigateToSearcher(
            Solution solution,
            IAsynchronousOperationListener asyncListener,
            INavigateToSearchCallback callback,
            string searchPattern,
            bool searchCurrentDocument,
            IImmutableSet<string> kinds,
            CancellationToken cancellationToken)
        {
            _solution = solution;
            _asyncListener = asyncListener;
            _callback = callback;
            _searchPattern = searchPattern;
            _searchCurrentDocument = searchCurrentDocument;
            _kinds = kinds;
            _cancellationToken = cancellationToken;
            _progress = new StreamingProgressTracker((current, maximum) =>
            {
                callback.ReportProgress(current, maximum);
                return new ValueTask();
            });

            if (_searchCurrentDocument)
            {
                var documentService = _solution.Workspace.Services.GetRequiredService<IDocumentTrackingService>();
                var activeId = documentService.TryGetActiveDocument();
                _currentDocument = activeId != null ? _solution.GetDocument(activeId) : null;
            }

            var docTrackingService = _solution.Workspace.Services.GetService<IDocumentTrackingService>() ?? NoOpDocumentTrackingService.Instance;

            // If the workspace is tracking documents, use that to prioritize our search
            // order.  That way we provide results for the documents the user is working
            // on faster than the rest of the solution.
            _activeDocument = docTrackingService.GetActiveDocument(_solution);
            _visibleDocuments = docTrackingService.GetVisibleDocuments(_solution)
                                                  .WhereAsArray(d => d != _activeDocument);
        }

        internal async Task SearchAsync()
        {
            var searchWasComplete = true;

            try
            {
                using var navigateToSearch = Logger.LogBlock(FunctionId.NavigateTo_Search, KeyValueLogMessage.Create(LogType.UserAction), _cancellationToken);
                using var asyncToken = _asyncListener.BeginAsyncOperation(GetType() + ".Search");

                searchWasComplete = await SearchAllProjectsAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                // providing this extra information will make UI to show indication to users
                // that result might not contain full data
                _callback.Done(searchWasComplete);
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if all searches were performed against the latest data
        /// and represent the complete set of results. Or <see langword="false"/> if any searches
        /// were performed against cached data.
        /// </summary>
        private async Task<bool> SearchAllProjectsAsync()
        {
            var service = _solution.Workspace.Services.GetRequiredService<IWorkspaceStatusService>();
            var isFullyLoaded = await service.IsFullyLoadedAsync(_cancellationToken).ConfigureAwait(false);

            var orderedProjects = GetOrderedProjectsToProcess();
            var (itemsReported, projectResults) = await ProcessProjectsAsync(
                orderedProjects, isFullyLoaded, allowCachedData: true).ConfigureAwait(false);

            // If we're not fully loaded yet, this is the best we could do in terms of getting
            // results.  Nothing we can do at this point until we reach the fully loaded state.
            if (!isFullyLoaded)
            {
                // We weren't fully loaded, so definitely let the user know the results are
                // incomplete in the UI.
                return false;
            }

            // We were fully loaded *and* we reported some items to the user.
            var anyCached = projectResults.Any(t => t.cached);
            if (itemsReported > 0)
            {
                // Let our caller know if any of these results came from cached data.  If so, we'll
                // put a header on the nav-to window letting the user know that results may be stale.
                return !anyCached;
            }

            // We didn't have any items reported.  If it turns out that some of our projects were
            // using cached data, try searching them again, but this time disallow checking them for
            // cached results.
            var projectsUsingCache = projectResults.Where(t => t.cached).SelectAsArray(t => t.project);
            Contract.ThrowIfFalse(isFullyLoaded);
            await ProcessProjectsAsync(
                ImmutableArray.Create(projectsUsingCache), isFullyLoaded: true, allowCachedData: false).ConfigureAwait(false);

            // we did a full uncached search.  return true to indicate that no message should be
            // shown to the user.
            return true;
        }

        private ImmutableArray<ImmutableArray<Project>> GetOrderedProjectsToProcess()
        {
            // If we're only searching the current doc, we don't need to examine anything else but that.
            if (_searchCurrentDocument)
            {
                var project = _currentDocument?.Project;
                return project == null
                    ? ImmutableArray<ImmutableArray<Project>>.Empty
                    : ImmutableArray.Create(ImmutableArray.Create(project));
            }

            using var result = TemporaryArray<ImmutableArray<Project>>.Empty;

            using var _ = PooledHashSet<Project>.GetInstance(out var processedProjects);

            // First, if there's an active document, search that project first, prioritizing that
            // active document and all visible documents from it.
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

        private ImmutableArray<Document> GetPriorityDocuments(Project project)
        {
            using var result = TemporaryArray<Document>.Empty;
            if (_activeDocument?.Project == project)
                result.Add(_activeDocument);

            foreach (var doc in _visibleDocuments)
            {
                if (doc.Project == project)
                    result.Add(doc);
            }

            return result.ToImmutableAndClear();
        }

        private async Task<(int itemsReported, ImmutableArray<(Project project, bool cached)>)> ProcessProjectsAsync(
            ImmutableArray<ImmutableArray<Project>> orderedProjects,
            bool isFullyLoaded,
            bool allowCachedData)
        {
            var projectCount = orderedProjects.Sum(p => p.Length);
            await _progress.AddItemsAsync(projectCount).ConfigureAwait(false);

            using var _ = ArrayBuilder<(Project project, bool cached)>.GetInstance(out var result);

            var seenItems = new HashSet<INavigateToSearchResult>(NavigateToSearchResultComparer.Instance);
            foreach (var projectGroup in orderedProjects)
            {
                var tasks = projectGroup.SelectAsArray(p => Task.Run(() => SearchAsync(p, isFullyLoaded, allowCachedData, seenItems)));
                var taskResults = await Task.WhenAll(tasks).ConfigureAwait(false);
                foreach (var taskResult in taskResults)
                    result.AddIfNotNull(taskResult);
            }

            return (seenItems.Count, result.ToImmutable());
        }

        private async Task<(Project project, bool cached)?> SearchAsync(
            Project project, bool isFullyLoaded, bool allowCachedData,
            HashSet<INavigateToSearchResult> seenItems)
        {
            try
            {
                var location = await SearchCoreAsync(project, allowCachedData, isFullyLoaded, seenItems).ConfigureAwait(false);
                if (location == null)
                    return null;

                return (project, cached: location == NavigateToSearchLocation.Cache);
            }
            finally
            {
                await _progress.ItemCompletedAsync().ConfigureAwait(false);
            }
        }

        private async Task<NavigateToSearchLocation?> SearchCoreAsync(
            Project project, bool isFullyLoaded, bool allowCachedData,
            HashSet<INavigateToSearchResult> seenItems)
        {
            var service = project.GetLanguageService<INavigateToSearchService>();
            if (service == null)
                return null;

            if (_searchCurrentDocument)
            {
                Contract.ThrowIfNull(_currentDocument);
                return await service.SearchDocumentAsync(
                    _currentDocument, _searchPattern, _kinds, OnResultFound, isFullyLoaded, allowCachedData, _cancellationToken).ConfigureAwait(false);
            }

            var priorityDocuments = GetPriorityDocuments(project);
            return await service.SearchProjectAsync(
                project, priorityDocuments, _searchPattern, _kinds, OnResultFound, isFullyLoaded, allowCachedData, _cancellationToken).ConfigureAwait(false);

            async Task OnResultFound(INavigateToSearchResult result)
            {
                // If we're seeing a dupe in another project, then filter it out here.  The results from
                // the individual projects will already contain the information about all the projects
                // leading to a better condensed view that doesn't look like it contains duplicate info.
                lock (seenItems)
                {
                    if (!seenItems.Add(result))
                        return;
                }

                await _callback.AddItemAsync(project, result, _cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
