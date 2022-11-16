// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
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
                var matches = result.Matches.SelectAsArray(static m => new PatternMatch(
                    ConvertKind(m.Kind),
                    punctuationStripped: false,
                    m.IsCaseSensitive,
                    m.MatchedSpans.SelectAsArray(static s => s.ToSpan())));

                _searchCallback.AddItem(new RoslynCodeSearchResult(
                    _provider,
                    result,
                    // Last match in the matches array corresponds to the name portion of the match.
                    matches.Last(),
                    GetResultType(result.Kind),
                    result.Name,
                    result.SecondarySort,
                    matches,
                    result.NavigableItem.Document?.FilePath,
                    tieBreakingSortText: null,
                    perProviderItemPriority: (int)result.MatchKind,
                    flags: SearchResultFlags.Default,
                    project.Language));

                return Task.CompletedTask;
            }

            private static PatternMatchKind ConvertKind(PatternMatching.PatternMatchKind kind)
                => kind switch
                {
                    PatternMatching.PatternMatchKind.Exact => PatternMatchKind.Exact,
                    PatternMatching.PatternMatchKind.Prefix => PatternMatchKind.Prefix,
                    PatternMatching.PatternMatchKind.NonLowercaseSubstring => PatternMatchKind.Substring,
                    PatternMatching.PatternMatchKind.StartOfWordSubstring => PatternMatchKind.Substring,
                    PatternMatching.PatternMatchKind.CamelCaseExact => PatternMatchKind.CamelCaseExact,
                    PatternMatching.PatternMatchKind.CamelCasePrefix => PatternMatchKind.CamelCasePrefix,
                    PatternMatching.PatternMatchKind.CamelCaseNonContiguousPrefix => PatternMatchKind.CamelCaseNonContiguousPrefix,
                    PatternMatching.PatternMatchKind.CamelCaseSubstring => PatternMatchKind.CamelCaseSubstring,
                    PatternMatching.PatternMatchKind.CamelCaseNonContiguousSubstring => PatternMatchKind.CamelCaseNonContiguousSubstring,
                    PatternMatching.PatternMatchKind.Fuzzy => PatternMatchKind.Fuzzy,
                    // Map our value to 'Fuzzy' as that's the lower value the platform supports.
                    PatternMatching.PatternMatchKind.LowercaseSubstring => PatternMatchKind.Fuzzy,
                    _ => PatternMatchKind.Fuzzy,
                };

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
        }
    }
}
