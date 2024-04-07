// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo;

internal interface INavigateToSearchResult
{
    string AdditionalInformation { get; }
    string Kind { get; }
    NavigateToMatchKind MatchKind { get; }
    bool IsCaseSensitive { get; }
    string Name { get; }
    ImmutableArray<TextSpan> NameMatchSpans { get; }
    string SecondarySort { get; }
    string? Summary { get; }

    INavigableItem NavigableItem { get; }
    ImmutableArray<PatternMatch> Matches { get; }
}

internal static class NavigateToSearchResultHelpers
{
    /// <summary>
    /// Helper to bridge from old api that only returned one pattern match, to new API which can return many.
    /// </summary>
    public static ImmutableArray<PatternMatch> GetMatches(INavigateToSearchResult result)
    {
        var patternMatch = new PatternMatch(
            GetPatternMatchKind(result.MatchKind),
            punctuationStripped: false,
            result.IsCaseSensitive,
            result.NameMatchSpans);
        return [patternMatch];
    }

    private static PatternMatchKind GetPatternMatchKind(NavigateToMatchKind matchKind)
        => matchKind switch
        {
            NavigateToMatchKind.Exact => PatternMatchKind.Exact,
            NavigateToMatchKind.Prefix => PatternMatchKind.Prefix,
            NavigateToMatchKind.Substring => PatternMatchKind.NonLowercaseSubstring,
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
