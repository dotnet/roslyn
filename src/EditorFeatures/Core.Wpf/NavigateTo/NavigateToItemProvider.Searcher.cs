// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Text.PatternMatching;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal partial class NavigateToItemProvider
    {
        private class Searcher
        {
            private readonly Solution _solution;
            private readonly IAsynchronousOperationListener _asyncListener;
            private readonly INavigateToItemDisplayFactory _displayFactory;
            private readonly INavigateToCallback _callback;
            private readonly string _searchPattern;
            private readonly bool _searchCurrentDocument;
            private readonly IImmutableSet<string> _kinds;
            private readonly Document _currentDocument;
            private readonly ProgressTracker _progress;
            private readonly CancellationToken _cancellationToken;

            public Searcher(
                Solution solution,
                IAsynchronousOperationListener asyncListener,
                INavigateToItemDisplayFactory displayFactory,
                INavigateToCallback callback,
                string searchPattern,
                bool searchCurrentDocument,
                IImmutableSet<string> kinds,
                CancellationToken cancellationToken)
            {
                _solution = solution;
                _asyncListener = asyncListener;
                _displayFactory = displayFactory;
                _callback = callback;
                _searchPattern = searchPattern;
                _searchCurrentDocument = searchCurrentDocument;
                _kinds = kinds;
                _cancellationToken = cancellationToken;
                _progress = new ProgressTracker((_, current, maximum) => callback.ReportProgress(current, maximum));

                if (_searchCurrentDocument)
                {
                    var documentService = _solution.Workspace.Services.GetService<IDocumentTrackingService>();
                    var activeId = documentService.TryGetActiveDocument();
                    _currentDocument = activeId != null ? _solution.GetDocument(activeId) : null;
                }
            }

            internal async Task SearchAsync()
            {
                try
                {
                    using var navigateToSearch = Logger.LogBlock(FunctionId.NavigateTo_Search, KeyValueLogMessage.Create(LogType.UserAction), _cancellationToken);
                    using var asyncToken = _asyncListener.BeginAsyncOperation(GetType() + ".Search");
                    _progress.AddItems(_solution.Projects.Count());

                    var workspace = _solution.Workspace;

                    // If the workspace is tracking documents, use that to prioritize our search
                    // order.  That way we provide results for the documents the user is working
                    // on faster than the rest of the solution.
                    var docTrackingService = workspace.Services.GetService<IDocumentTrackingService>();
                    if (docTrackingService != null)
                    {
                        await SearchProjectsInPriorityOrder(docTrackingService).ConfigureAwait(false);
                    }
                    else
                    {
                        await SearchAllProjectsAsync().ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    _callback.Done();
                }
            }

            private async Task SearchProjectsInPriorityOrder(IDocumentTrackingService docTrackingService)
            {
                var processedProjects = new HashSet<Project>();

                var activeDocOpt = docTrackingService.GetActiveDocument(_solution);
                var visibleDocs = docTrackingService.GetVisibleDocuments(_solution)
                                                    .Where(d => d != activeDocOpt)
                                                    .ToImmutableArray();

                // First, if there's an active document, search that project first, prioritizing
                // that active document and all visible documents from it.
                if (activeDocOpt != null)
                {
                    var activeProject = activeDocOpt.Project;
                    processedProjects.Add(activeProject);

                    var visibleDocsFromProject = visibleDocs.Where(d => d.Project == activeProject);
                    var priorityDocs = ImmutableArray.Create(activeDocOpt).AddRange(visibleDocsFromProject);

                    // Search the active project first.  That way we can deliver results that are
                    // closer in scope to the user quicker without forcing them to do something like
                    // NavToInCurrentDoc
                    await Task.Run(() => SearchAsync(activeProject, priorityDocs), _cancellationToken).ConfigureAwait(false);
                }

                // Now, process all visible docs that were not from the active project.
                var tasks = new List<Task>();
                foreach (var (currentProject, priorityDocs) in visibleDocs.GroupBy(d => d.Project))
                {
                    // make sure we only process this project if we didn't already process it above.
                    if (processedProjects.Add(currentProject))
                    {
                        tasks.Add(Task.Run(() => SearchAsync(currentProject, priorityDocs.ToImmutableArray()), _cancellationToken));
                    }
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                // Now, process the remainder of projects
                tasks.Clear();
                foreach (var currentProject in _solution.Projects)
                {
                    // make sure we only process this project if we didn't already process it above.
                    if (processedProjects.Add(currentProject))
                    {
                        tasks.Add(Task.Run(() => SearchAsync(currentProject, ImmutableArray<Document>.Empty), _cancellationToken));
                    }
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            private async Task SearchAllProjectsAsync()
            {
                // Search each project with an independent threadpool task.
                var searchTasks = _solution.Projects.Select(
                    p => Task.Run(() => SearchAsync(p, priorityDocuments: ImmutableArray<Document>.Empty), _cancellationToken)).ToArray();

                await Task.WhenAll(searchTasks).ConfigureAwait(false);
            }

            private async Task SearchAsync(Project project, ImmutableArray<Document> priorityDocuments)
            {
                try
                {
                    await SearchAsyncWorker(project, priorityDocuments).ConfigureAwait(false);
                }
                finally
                {
                    _progress.ItemCompleted();
                }
            }

            private async Task SearchAsyncWorker(Project project, ImmutableArray<Document> priorityDocuments)
            {
                if (_searchCurrentDocument && _currentDocument?.Project != project)
                {
                    return;
                }

                var cacheService = project.Solution.Services.CacheService;
                if (cacheService != null)
                {
                    using (cacheService.EnableCaching(project.Id))
                    {
                        var service = TryGetNavigateToSearchService(project);
                        if (service != null)
                        {
                            var searchTask = _currentDocument != null
                                ? service.SearchDocumentAsync(_currentDocument, _searchPattern, _kinds, _cancellationToken)
                                : service.SearchProjectAsync(project, priorityDocuments, _searchPattern, _kinds, _cancellationToken);

                            var results = await searchTask.ConfigureAwait(false);
                            if (results != null)
                            {
                                foreach (var result in results)
                                {
                                    ReportMatchResult(project, result);
                                }
                            }
                        }
                    }
                }
            }

            private void ReportMatchResult(Project project, INavigateToSearchResult result)
            {
                var matchedSpans = result.NameMatchSpans.SelectAsArray(t => t.ToSpan());

                var patternMatch = new PatternMatch(GetPatternMatchKind(result.MatchKind),
                    punctuationStripped: true, result.IsCaseSensitive, matchedSpans);

                var navigateToItem = new NavigateToItem(
                    result.Name,
                    result.Kind,
                    GetNavigateToLanguage(project.Language),
                    result.SecondarySort,
                    result,
                    patternMatch,
                    _displayFactory);
                _callback.AddItem(navigateToItem);
            }

            private PatternMatchKind GetPatternMatchKind(NavigateToMatchKind matchKind)
                => matchKind switch
                {
                    NavigateToMatchKind.Exact => PatternMatchKind.Exact,
                    NavigateToMatchKind.Prefix => PatternMatchKind.Prefix,
                    NavigateToMatchKind.Substring => PatternMatchKind.Substring,
                    NavigateToMatchKind.Regular => PatternMatchKind.Fuzzy,
                    NavigateToMatchKind.None => PatternMatchKind.Fuzzy,
                    NavigateToMatchKind.CamelCaseExact => PatternMatchKind.CamelCaseExact,
                    NavigateToMatchKind.CamelCasePrefix => PatternMatchKind.CamelCasePrefix,
                    NavigateToMatchKind.CamelCaseNonContiguousPrefix => PatternMatchKind.CamelCaseNonContiguousPrefix,
                    NavigateToMatchKind.CamelCaseSubstring => PatternMatchKind.CamelCaseSubstring,
                    NavigateToMatchKind.CamelCaseNonContiguousSubstring => PatternMatchKind.CamelCaseNonContiguousSubstring,
                    NavigateToMatchKind.Fuzzy => PatternMatchKind.Fuzzy,
                    _ => throw ExceptionUtilities.UnexpectedValue(matchKind),
                };

            /// <summary>
            /// Returns the name for the language used by the old Navigate To providers.
            /// </summary>
            /// <remarks> It turns out this string is used for sorting and for some SQM data, so it's best
            /// to keep it unchanged.</remarks>
            private static string GetNavigateToLanguage(string languageName)
                => languageName switch
                {
                    LanguageNames.CSharp => "csharp",
                    LanguageNames.VisualBasic => "vb",
                    _ => languageName,
                };
        }
    }
}
