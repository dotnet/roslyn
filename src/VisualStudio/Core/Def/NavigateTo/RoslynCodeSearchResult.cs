// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Search.Data;
using Microsoft.VisualStudio.Text.PatternMatching;

namespace Microsoft.CodeAnalysis.NavigateTo;

internal sealed partial class RoslynSearchItemsSourceProvider
{
    /// <summary>
    /// Trivial subclass of <see cref="CodeSearchResult"/>.  Exists just so we can hold onto the original <see
    /// cref="INavigateToSearchResult"/> object we got back from the search so we can present the UI with the data
    /// from it.
    /// </summary>
    private sealed class RoslynCodeSearchResult : CodeSearchResult
    {
        public readonly INavigateToSearchResult SearchResult;

        public RoslynCodeSearchResult(
            RoslynSearchItemsSourceProvider provider,
            INavigateToSearchResult searchResult,
            string resultType,
            string primarySortText,
            string secondarySortText,
            IReadOnlyCollection<PatternMatch> patternMatches,
            string? location,
            float perProviderItemPriority,
            string language)
            : base(
                  provider._viewFactory,
                  resultType,
                  primarySortText,
                  secondarySortText,
                  patternMatches,
                  location,
                  perProviderItemPriority: perProviderItemPriority,
                  language: language)
        {
            SearchResult = searchResult;
        }
    }
}
