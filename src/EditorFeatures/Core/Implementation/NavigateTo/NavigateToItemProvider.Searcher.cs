// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Roslyn.Utilities;

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
            private readonly ProgressTracker _progress;
            private readonly IAsynchronousOperationListener _asyncListener;
            private readonly CancellationToken _cancellationToken;

            public Searcher(
                Solution solution,
                IAsynchronousOperationListener asyncListener,
                ItemDisplayFactory displayFactory,
                INavigateToCallback callback,
                string searchPattern,
                CancellationToken cancellationToken)
            {
                _solution = solution;
                _displayFactory = displayFactory;
                _callback = callback;
                _searchPattern = searchPattern;
                _cancellationToken = cancellationToken;
                _progress = new ProgressTracker(callback.ReportProgress);
                _asyncListener = asyncListener;
            }

            internal void Search()
            {
                var navigateToSearch = Logger.LogBlock(FunctionId.NavigateTo_Search, _cancellationToken);
                var asyncToken = _asyncListener.BeginAsyncOperation(GetType() + ".Search");

                _progress.AddItems(_solution.Projects.Count());

                // make sure we run actual search from other thread. and let this thread return to caller as soon as possible.
                var dummy = Task.Run(() => Search(navigateToSearch, asyncToken), _cancellationToken);
            }

            private void Search(IDisposable navigateToSearch, IAsyncToken asyncToken)
            {
                var searchTasks = _solution.Projects.Select(SearchAsync).ToArray();
                var whenAllTask = Task.WhenAll(searchTasks);

                // NOTE(cyrusn) This SafeContinueWith is *not* cancellable.  We must dispose of the notifier
                // in order for tests to work property.  Also, if we don't notify the callback that we're
                // done then the UI will never stop displaying the progress bar.
                whenAllTask.SafeContinueWith(_ =>
                {
                    _callback.Done();
                    navigateToSearch.Dispose();
                    asyncToken.Dispose();
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            }

            private async Task SearchAsync(Project project)
            {
                var cacheService = project.Solution.Services.CacheService;
                if (cacheService != null)
                {
                    using (cacheService.EnableCaching(project.Id))
                    {
                        var service = project.LanguageServices.GetService<INavigateToSearchService>();
                        if (service != null)
                        {
                            var results = await service.SearchProjectAsync(project, _searchPattern, _cancellationToken).ConfigureAwait(false);
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

                _progress.ItemCompleted();
            }

            private void ReportMatchResult(Project project, INavigateToSearchResult result)
            {
                var navigateToItem = new NavigateToItem(
                    result.Name,
                    result.Kind,
                    GetNavigateToLanguage(project.Language),
                    result.SecondarySort,
                    result,
                    result.MatchKind,
                    result.IsCaseSensitive,
                    _displayFactory);
                _callback.AddItem(navigateToItem);
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
