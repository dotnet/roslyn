// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
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

        private readonly Document? _activeDocument;
        private readonly ImmutableArray<Document> _visibleDocuments;

        /// <summary>
        /// Single task used to both hydrate the remote host with the initial workspace solution,
        /// and track if that work completed.  Prior to it completing, we will try to get all
        /// navigate-to requests from our caches.  Once it is populated though, we can attempt to
        /// use the latest data instead.
        /// </summary>
        private static readonly object s_gate = new();
        private static Task s_remoteHostPopulatedTask = null!;

        private NavigateToSearcher(
            Solution solution,
            IAsynchronousOperationListener asyncListener,
            INavigateToSearchCallback callback,
            string searchPattern,
            bool searchCurrentDocument,
            IImmutableSet<string> kinds)
        {
            _solution = solution;
            _asyncListener = asyncListener;
            _callback = callback;
            _searchPattern = searchPattern;
            _searchCurrentDocument = searchCurrentDocument;
            _kinds = kinds;
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

        public static NavigateToSearcher Create(
            Solution solution,
            IAsynchronousOperationListener asyncListener,
            INavigateToSearchCallback callback,
            string searchPattern,
            bool searchCurrentDocument,
            IImmutableSet<string> kinds,
            CancellationToken disposalToken)
        {
            InitializeRemoteHostIfNecessary(solution, asyncListener, disposalToken);
            return new NavigateToSearcher(solution, asyncListener, callback, searchPattern, searchCurrentDocument, kinds);
        }

        private static void InitializeRemoteHostIfNecessary(
            Solution solution, IAsynchronousOperationListener asyncListener, CancellationToken disposalToken)
        {
            lock (s_gate)
            {
                if (s_remoteHostPopulatedTask != null)
                    return;

                // If there are no projects in this solution that use OOP, then there's nothing we need to do.
                if (solution.Projects.All(p => !RemoteSupportedLanguages.IsSupported(p.Language)))
                {
                    s_remoteHostPopulatedTask = Task.CompletedTask;
                    return;
                }

                var asyncToken = asyncListener.BeginAsyncOperation(nameof(InitializeRemoteHostIfNecessary));

                s_remoteHostPopulatedTask = Task.Run(async () =>
                {
                    var client = await RemoteHostClient.TryGetClientAsync(solution.Workspace, disposalToken).ConfigureAwait(false);
                    if (client != null)
                    {
                        await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                            solution,
                            (service, solutionInfo, cancellationToken) =>
                            service.HydrateAsync(solutionInfo, cancellationToken),
                            disposalToken).ConfigureAwait(false);
                    }
                }, disposalToken);
                s_remoteHostPopulatedTask.CompletesAsyncOperation(asyncToken);
            }
        }

        internal async Task SearchAsync(CancellationToken cancellationToken)
        {
            var searchWasComplete = true;

            try
            {
                using var navigateToSearch = Logger.LogBlock(FunctionId.NavigateTo_Search, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken);
                using var asyncToken = _asyncListener.BeginAsyncOperation(GetType() + ".Search");

                searchWasComplete = await SearchAllProjectsAsync(cancellationToken).ConfigureAwait(false);
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
        private async Task<bool> SearchAllProjectsAsync(CancellationToken cancellationToken)
        {
            var service = _solution.Workspace.Services.GetRequiredService<IWorkspaceStatusService>();
            var isProjectSystemFullyLoaded = await service.IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false);
            var isRemoteHostFullyLoaded = s_remoteHostPopulatedTask.IsCompleted;

            var isFullyLoaded = isProjectSystemFullyLoaded && isRemoteHostFullyLoaded;

            var orderedProjects = GetOrderedProjectsToProcess();
            var (itemsReported, projectResults) = await ProcessProjectsAsync(
                orderedProjects, isFullyLoaded, cancellationToken).ConfigureAwait(false);

            // If we're not fully loaded yet, this is the best we could do in terms of getting
            // results.  Nothing we can do at this point until we reach the fully loaded state.
            if (isFullyLoaded)
            {
                // We were fully loaded, so definitely let the user know the results are complete in the UI.
                return true;
            }

            // We were fully loaded *and* we reported some items to the user.
            var anyCached = projectResults.Any(t => t.cached);
            if (itemsReported > 0)
            {
                // Let our caller know if any of these results came from cached data.  If so, we'll
                // put a header on the nav-to window letting the user know that results may be stale.
                return !anyCached;
            }

            // We didn't have any items reported and we weren't fully loaded.  If it turns out that
            // some of our projects were using cached data then we can try searching them again, but
            // this tell them to use the latest data.  The ensures the user at least gets some
            // result instead of nothing.
            var projectsUsingCache = projectResults.Where(t => t.cached).SelectAsArray(t => t.project);
            await ProcessProjectsAsync(ImmutableArray.Create(projectsUsingCache), isFullyLoaded: true, cancellationToken).ConfigureAwait(false);

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
            ImmutableArray<ImmutableArray<Project>> orderedProjects, bool isFullyLoaded, CancellationToken cancellationToken)
        {
            var projectCount = orderedProjects.Sum(p => p.Length);
            await _progress.AddItemsAsync(projectCount).ConfigureAwait(false);

            using var _ = ArrayBuilder<(Project project, bool cached)>.GetInstance(out var result);

            var seenItems = new HashSet<INavigateToSearchResult>(NavigateToSearchResultComparer.Instance);
            foreach (var projectGroup in orderedProjects)
            {
                var tasks = projectGroup.SelectAsArray(p => Task.Run(() => SearchAsync(p, isFullyLoaded, seenItems, cancellationToken)));
                var taskResults = await Task.WhenAll(tasks).ConfigureAwait(false);
                foreach (var taskResult in taskResults)
                    result.AddIfNotNull(taskResult);
            }

            return (seenItems.Count, result.ToImmutable());
        }

        private async Task<(Project project, bool cached)?> SearchAsync(
            Project project, bool isFullyLoaded, HashSet<INavigateToSearchResult> seenItems, CancellationToken cancellationToken)
        {
            try
            {
                var location = await SearchCoreAsync(project, isFullyLoaded, seenItems, cancellationToken).ConfigureAwait(false);
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
            Project project, bool isFullyLoaded, HashSet<INavigateToSearchResult> seenItems, CancellationToken cancellationToken)
        {
            var service = project.GetLanguageService<INavigateToSearchService>();
            if (service == null)
                return null;

            if (_searchCurrentDocument)
            {
                Contract.ThrowIfNull(_currentDocument);
                return await service.SearchDocumentAsync(
                    _currentDocument, _searchPattern, _kinds, OnResultFound, isFullyLoaded, cancellationToken).ConfigureAwait(false);
            }

            var priorityDocuments = GetPriorityDocuments(project);
            return await service.SearchProjectAsync(
                project, priorityDocuments, _searchPattern, _kinds, OnResultFound, isFullyLoaded, cancellationToken).ConfigureAwait(false);

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

                await _callback.AddItemAsync(project, result, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
