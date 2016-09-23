// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal partial class NavigateToItemProvider
    {
        private class Searcher
        {
            private readonly Solution _solution;
            private readonly ItemDisplayFactory _displayFactory;
            private readonly INavigateToCallback _callback;
            private readonly string _searchPattern;
            private readonly bool _searchCurrentDocument;
            private readonly Document _currentDocument;
            private readonly ProgressTracker _progress;
            private readonly IAsynchronousOperationListener _asyncListener;
            private readonly CancellationToken _cancellationToken;

            public Searcher(
                Solution solution,
                IAsynchronousOperationListener asyncListener,
                ItemDisplayFactory displayFactory,
                INavigateToCallback callback,
                string searchPattern,
                bool searchCurrentDocument,
                CancellationToken cancellationToken)
            {
                _solution = solution;
                _displayFactory = displayFactory;
                _callback = callback;
                _searchPattern = searchPattern;
                _searchCurrentDocument = searchCurrentDocument;
                _cancellationToken = cancellationToken;
                _progress = new ProgressTracker(callback.ReportProgress);
                _asyncListener = asyncListener;

                if (_searchCurrentDocument)
                {
                    var documentService = _solution.Workspace.Services.GetService<IDocumentTrackingService>();
                    var activeId = documentService.GetActiveDocument();
                    _currentDocument = activeId != null ? _solution.GetDocument(activeId) : null;
                }
            }

            internal async void Search()
            {
                try
                {
                    using (var navigateToSearch = Logger.LogBlock(FunctionId.NavigateTo_Search, _cancellationToken))
                    using (var asyncToken = _asyncListener.BeginAsyncOperation(GetType() + ".Search"))
                    {
                        _progress.AddItems(_solution.Projects.Count());

                        // Search each project with an independent threadpool task.
                        var searchTasks = _solution.Projects.Select(
                            p => Task.Run(() => SearchAsync(p))).ToArray();

                        await Task.WhenAll(searchTasks).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _callback.Done();
                }
            }

            private async Task SearchAsync(Project project)
            {
                try
                {
                    await SearchAsyncWorker(project).ConfigureAwait(false);
                }
                finally
                {
                    _progress.ItemCompleted();
                }
            }

            private async Task SearchAsyncWorker(Project project)
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
                        var service = project.LanguageServices.GetService<INavigateToSearchService>();
                        if (service != null)
                        {
                            var searchTask = _currentDocument != null
                                ? service.SearchDocumentAsync(_currentDocument, _searchPattern, _cancellationToken)
                                : service.SearchProjectAsync(project, _searchPattern, _cancellationToken);

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
                var navigateToItem = new NavigateToItem(
                    result.Name,
                    result.Kind,
                    GetNavigateToLanguage(project.Language),
                    result.SecondarySort,
                    result,
                    GetMatchKind(result.MatchKind),
                    result.IsCaseSensitive,
                    _displayFactory);
                _callback.AddItem(navigateToItem);
            }

            private MatchKind GetMatchKind(NavigateToMatchKind matchKind)
            {
                switch (matchKind)
                {
                    case NavigateToMatchKind.Exact: return MatchKind.Exact;
                    case NavigateToMatchKind.Prefix: return MatchKind.Prefix;
                    case NavigateToMatchKind.Substring: return MatchKind.Substring;
                    case NavigateToMatchKind.Regular: return MatchKind.Regular;
                    default: return MatchKind.None;
                }
            }

            /// <summary>
            /// Returns the name for the language used by the old Navigate To providers.
            /// </summary>
            /// <remarks> It turns out this string is used for sorting and for some SQM data, so it's best
            /// to keep it unchanged.</remarks>
            private static string GetNavigateToLanguage(string languageName)
            {
                switch (languageName)
                {
                    case LanguageNames.CSharp:
                        return "csharp";
                    case LanguageNames.VisualBasic:
                        return "vb";
                    default:
                        return languageName;
                }
            }
        }
    }
}
