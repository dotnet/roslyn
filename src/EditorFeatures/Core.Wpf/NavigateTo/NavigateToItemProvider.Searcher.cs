// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.PatternMatching;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal partial class NavigateToItemProvider
    {
        private class Searcher
        {
            private readonly Solution _solution;
            private readonly INavigateToItemDisplayFactory _displayFactory;
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
                INavigateToItemDisplayFactory displayFactory,
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
                    using (var navigateToSearch = Logger.LogBlock(FunctionId.NavigateTo_Search, KeyValueLogMessage.Create(LogType.UserAction), _cancellationToken))
                    using (var asyncToken = _asyncListener.BeginAsyncOperation(GetType() + ".Search"))
                    {
                        _progress.AddItems(_solution.Projects.Count());

                        // Search each project with an independent threadpool task.
                        var searchTasks = _solution.Projects.Select(
                            p => Task.Run(() => SearchAsync(p), _cancellationToken)).ToArray();

                        await Task.WhenAll(searchTasks).ConfigureAwait(false);
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

#pragma warning disable CS0618 // MatchKind is obsolete
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
            {
                switch (matchKind)
                {
                    case NavigateToMatchKind.Exact: return PatternMatchKind.Exact;
                    case NavigateToMatchKind.Prefix: return PatternMatchKind.Prefix;
                    case NavigateToMatchKind.Substring: return PatternMatchKind.Substring;
                    case NavigateToMatchKind.Regular: return PatternMatchKind.Fuzzy;
                    case NavigateToMatchKind.None: return PatternMatchKind.Fuzzy;
                    case NavigateToMatchKind.CamelCaseExact: return PatternMatchKind.CamelCaseExact;
                    case NavigateToMatchKind.CamelCasePrefix: return PatternMatchKind.CamelCasePrefix;
                    case NavigateToMatchKind.CamelCaseNonContiguousPrefix: return PatternMatchKind.CamelCaseNonContiguousPrefix;
                    case NavigateToMatchKind.CamelCaseSubstring: return PatternMatchKind.CamelCaseSubstring;
                    case NavigateToMatchKind.CamelCaseNonContiguousSubstring: return PatternMatchKind.CamelCaseNonContiguousSubstring;
                    case NavigateToMatchKind.Fuzzy: return PatternMatchKind.Fuzzy;
                    default: throw ExceptionUtilities.UnexpectedValue(matchKind);
                }
            }
#pragma warning restore CS0618 // MatchKind is obsolete

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
