﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Text.PatternMatching;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal partial class NavigateToItemProvider
    {
        private class NavigateToItemProviderCallback : INavigateToSearchCallback
        {
            private readonly INavigateToItemDisplayFactory _displayFactory;
            private readonly INavigateToCallback _callback;

            public NavigateToItemProviderCallback(INavigateToItemDisplayFactory displayFactory, INavigateToCallback callback)
            {
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

            public Task AddItemAsync(Project project, INavigateToSearchResult result, CancellationToken cancellationToken)
            {
                ReportMatchResult(project, result);
                return Task.CompletedTask;
            }

            public void ReportProgress(int current, int maximum)
            {
                _callback.ReportProgress(current, maximum);
            }

            private void ReportMatchResult(Project project, INavigateToSearchResult result)
            {
                var matchedSpans = result.NameMatchSpans.SelectAsArray(t => t.ToSpan());

                var patternMatch = new PatternMatch(
                    GetPatternMatchKind(result.MatchKind),
                    punctuationStripped: false,
                    result.IsCaseSensitive,
                    matchedSpans);

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
}
