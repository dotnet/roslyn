// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Text.PatternMatching;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;

internal partial class NavigateToItemProvider
{
    private sealed class NavigateToItemProviderCallback : INavigateToSearchCallback
    {
        private readonly Solution _solution;
        private readonly INavigateToItemDisplayFactory _displayFactory;
        private readonly INavigateToCallback _callback;

        public NavigateToItemProviderCallback(Solution solution, INavigateToItemDisplayFactory displayFactory, INavigateToCallback callback)
        {
            _solution = solution;
            _displayFactory = displayFactory;
            _callback = callback;
        }

        public void Done(bool isFullyLoaded)
        {
            if (!isFullyLoaded && _callback is INavigateToCallback2 callback2)
            {
                callback2.Done(IncompleteReason.SolutionLoading);
            }
            else
            {
                _callback.Done();
            }
        }

        public async Task AddResultsAsync(ImmutableArray<INavigateToSearchResult> results, Document? activeDocument, CancellationToken cancellationToken)
        {
            foreach (var result in results)
            {
                var matchedSpans = result.NameMatchSpans.SelectAsArray(t => t.ToSpan());

                var patternMatch = new PatternMatch(
                    GetPatternMatchKind(result.MatchKind),
                    punctuationStripped: false,
                    result.IsCaseSensitive,
                    matchedSpans);

                var project = _solution.GetRequiredProject(result.NavigableItem.Document.Project.Id);
                var navigateToItem = new NavigateToItem(
                    result.Name,
                    result.Kind,
                    GetNavigateToLanguage(project.Language),
                    result.SecondarySort,
                    result,
                    patternMatch,
                    _displayFactory);

                try
                {
                    _callback.AddItem(navigateToItem);
                }
                catch (InvalidOperationException ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
                {
                    // Mitigation for race condition in platform https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1534364
                    //
                    // Catch this so that don't tear down OOP, but still report the exception so that we ensure this issue
                    // gets attention and is fixed.
                }
            }
        }

        public void ReportProgress(int current, int maximum)
        {
            _callback.ReportProgress(current, maximum);
        }

        public void ReportIncomplete()
        {
        }

        private static PatternMatchKind GetPatternMatchKind(NavigateToMatchKind matchKind)
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
