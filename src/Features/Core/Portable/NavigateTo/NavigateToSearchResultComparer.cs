// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    /// <summary>
    /// Comparer that considers to navigate to results the same if they will navigate to the same document and span.
    /// This ensures that we don't see tons of results for the same symbol when a file is linked into many projects.
    /// </summary>
    internal class NavigateToSearchResultComparer : IEqualityComparer<INavigateToSearchResult>
    {
        public static readonly IEqualityComparer<INavigateToSearchResult> Instance = new NavigateToSearchResultComparer();

        private NavigateToSearchResultComparer()
        {
        }

        public bool Equals(INavigateToSearchResult? x, INavigateToSearchResult? y)
            => x?.NavigableItem.Document.FilePath == y?.NavigableItem.Document.FilePath &&
               x?.NavigableItem.SourceSpan == y?.NavigableItem.SourceSpan;

        public int GetHashCode(INavigateToSearchResult? obj)
            => Hash.Combine(obj?.NavigableItem.Document.FilePath, obj?.NavigableItem.SourceSpan.GetHashCode() ?? 0);
    }
}
