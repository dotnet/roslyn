// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Search.Data;
using Microsoft.VisualStudio.Text.PatternMatching;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal sealed partial class RoslynSearchItemsSourceProvider
    {
        /// <summary>
        /// A callback to be passed to the <see cref="NavigateToSearcher"/>.  Results it pushes into us will then be
        /// converted and pushed into <see cref="_searchCallback"/>.
        /// </summary>
        private sealed class RoslynNavigateToSearchCallback : INavigateToSearchCallback
        {
            private readonly RoslynSearchItemsSourceProvider _provider;
            private readonly ISearchCallback _searchCallback;

            public RoslynNavigateToSearchCallback(
                RoslynSearchItemsSourceProvider provider,
                ISearchCallback searchCallback)
            {
                _provider = provider;
                _searchCallback = searchCallback;
            }

            public void Done(bool isFullyLoaded)
            {
                if (!isFullyLoaded)
                    ReportIncomplete();

                _searchCallback.ReportProgress(1, 1);
            }

            public void ReportProgress(int current, int maximum)
                => _searchCallback.ReportProgress(current, maximum);

            public void ReportIncomplete()
            {
                // IncompleteReason.Parsing corresponds to:
                // "The results may be inaccurate because the search information is still being updated."
                //
                // This the most accurate message for us as we only report this when we're currently reporting
                // potentially stale results from the nav-to cache.
                _searchCallback.ReportIncomplete(IncompleteReason.Parsing);
            }

            public Task AddItemAsync(Project project, INavigateToSearchResult result, CancellationToken cancellationToken)
            {
                var patternMatch = new PatternMatch(
                    GetPatternMatchKind(result.MatchKind),
                    punctuationStripped: false,
                    result.IsCaseSensitive,
                    result.NameMatchSpans.SelectAsArray(t => t.ToSpan()));

                _searchCallback.AddItem(new RoslynCodeSearchResult(
                    _provider,
                    result,
                    patternMatch,
                    GetResultType(result.Kind),
                    result.Name,
                    result.SecondarySort,
                    new[] { patternMatch },
                    result.NavigableItem.Document?.FilePath,
                    tieBreakingSortText: null,
                    perProviderItemPriority: (int)result.MatchKind,
                    flags: SearchResultFlags.Default,
                    project.Language));

                return Task.CompletedTask;
            }

            private static string GetResultType(string kind)
            {
                return kind switch
                {
                    NavigateToItemKind.Class => CodeSearchResultType.Class,
                    NavigateToItemKind.Constant => CodeSearchResultType.Constant,
                    NavigateToItemKind.Delegate => CodeSearchResultType.Delegate,
                    NavigateToItemKind.Enum => CodeSearchResultType.Enum,
                    NavigateToItemKind.EnumItem => CodeSearchResultType.EnumItem,
                    NavigateToItemKind.Event => CodeSearchResultType.Event,
                    NavigateToItemKind.Field => CodeSearchResultType.Field,
                    NavigateToItemKind.Interface => CodeSearchResultType.Interface,
                    NavigateToItemKind.Method => CodeSearchResultType.Method,
                    NavigateToItemKind.Module => CodeSearchResultType.Module,
                    NavigateToItemKind.Property => CodeSearchResultType.Property,
                    NavigateToItemKind.Structure => CodeSearchResultType.Structure,
                    _ => kind
                };
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
        }
    }
}
