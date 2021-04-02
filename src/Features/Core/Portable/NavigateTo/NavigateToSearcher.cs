﻿// Licensed to the .NET Foundation under one or more agreements.
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
        }

        internal async Task SearchAsync()
        {
            var isFullyLoaded = true;

            try
            {
                var service = _solution.Workspace.Services.GetRequiredService<IWorkspaceStatusService>();
                isFullyLoaded = await service.IsFullyLoadedAsync(_cancellationToken).ConfigureAwait(false);

                using var navigateToSearch = Logger.LogBlock(FunctionId.NavigateTo_Search, KeyValueLogMessage.Create(LogType.UserAction), _cancellationToken);
                using var asyncToken = _asyncListener.BeginAsyncOperation(GetType() + ".Search");
                await _progress.AddItemsAsync(_solution.Projects.Count()).ConfigureAwait(false);
                await SearchAllProjectsAsync(isFullyLoaded).ConfigureAwait(false);
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

        private async Task SearchAllProjectsAsync(bool isFullyLoaded)
        {
            var seenItems = new HashSet<INavigateToSearchResult>(NavigateToSearchResultComparer.Instance);
            var processedProjects = new HashSet<Project>();

            // If the workspace is tracking documents, use that to prioritize our search
            // order.  That way we provide results for the documents the user is working
            // on faster than the rest of the solution.
            var docTrackingService = _solution.Workspace.Services.GetService<IDocumentTrackingService>() ?? NoOpDocumentTrackingService.Instance;

            var activeDocument = docTrackingService.GetActiveDocument(_solution);
            var visibleDocs = docTrackingService.GetVisibleDocuments(_solution)
                                                .WhereAsArray(d => d != activeDocument);

            // First, if there's an active document, search that project first, prioritizing
            // that active document and all visible documents from it.
            if (activeDocument != null)
            {
                var activeProject = activeDocument.Project;
                processedProjects.Add(activeProject);

                var visibleDocsFromProject = visibleDocs.Where(d => d.Project == activeProject);
                var priorityDocs = ImmutableArray.Create(activeDocument).AddRange(visibleDocsFromProject);

                // Search the active project first.  That way we can deliver results that are
                // closer in scope to the user quicker without forcing them to do something like
                // NavToInCurrentDoc
                await Task.Run(() => SearchAsync(activeProject, priorityDocs, seenItems, isFullyLoaded), _cancellationToken).ConfigureAwait(false);
            }

            // Now, process all visible docs that were not from the active project.
            var tasks = new List<Task>();
            foreach (var (currentProject, priorityDocs) in visibleDocs.GroupBy(d => d.Project))
            {
                // make sure we only process this project if we didn't already process it above.
                if (processedProjects.Add(currentProject))
                    tasks.Add(Task.Run(() => SearchAsync(currentProject, priorityDocs.ToImmutableArray(), seenItems, isFullyLoaded), _cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            // Now, process the remainder of projects
            tasks.Clear();
            foreach (var currentProject in _solution.Projects)
            {
                // make sure we only process this project if we didn't already process it above.
                if (processedProjects.Add(currentProject))
                    tasks.Add(Task.Run(() => SearchAsync(currentProject, ImmutableArray<Document>.Empty, seenItems, isFullyLoaded), _cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task SearchAsync(
            Project project,
            ImmutableArray<Document> priorityDocuments,
            HashSet<INavigateToSearchResult> seenItems,
            bool isFullyLoaded)
        {
            try
            {
                await SearchCoreAsync(project, priorityDocuments, seenItems, isFullyLoaded).ConfigureAwait(false);
            }
            finally
            {
                await _progress.ItemCompletedAsync().ConfigureAwait(false);
            }
        }

        private async Task SearchCoreAsync(
            Project project,
            ImmutableArray<Document> priorityDocuments,
            HashSet<INavigateToSearchResult> seenItems,
            bool isFullyLoaded)
        {
            if (_searchCurrentDocument && _currentDocument?.Project != project)
                return;

            var service = project.GetLanguageService<INavigateToSearchService>();
            if (service == null)
                return;

            var task = _currentDocument != null
                ? service.SearchDocumentAsync(_currentDocument, _searchPattern, _kinds, OnResultFound, isFullyLoaded, _cancellationToken)
                : service.SearchProjectAsync(project, priorityDocuments, _searchPattern, _kinds, OnResultFound, isFullyLoaded, _cancellationToken);

            await task.ConfigureAwait(false);
            return;

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
