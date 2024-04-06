// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NavigateTo;

internal static class NavigateToUtilities
{
    public static ImmutableHashSet<string> GetKindsProvided(Solution solution)
    {
        var result = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        foreach (var project in solution.Projects)
        {
            var navigateToSearchService = project.GetLanguageService<INavigateToSearchService>();
            if (navigateToSearchService != null)
                result.UnionWith(navigateToSearchService.KindsProvided);
        }

        return result.ToImmutable();
    }

    public static TextSpan GetBoundedSpan(INavigableItem item, SourceText sourceText)
    {
        var spanStart = item.SourceSpan.Start;
        var spanEnd = item.SourceSpan.End;
        if (item.IsStale)
        {
            // in the case of a stale item, the span may be out of bounds of the document. Cap
            // us to the end of the document as that's where we're going to navigate the user
            // to.
            spanStart = spanStart > sourceText.Length ? sourceText.Length : spanStart;
            spanEnd = spanEnd > sourceText.Length ? sourceText.Length : spanEnd;
        }

        return TextSpan.FromBounds(spanStart, spanEnd);
    }
}
