// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Navigation;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.NavigateTo;

internal class InternalFSharpNavigateToSearchResult : INavigateToSearchResult
{
    public string AdditionalInformation { get; }
    public string Kind { get; }
    public NavigateToMatchKind MatchKind { get; }
    public string Name { get; }
    public INavigableItem NavigableItem { get; }

    public InternalFSharpNavigateToSearchResult(FSharpNavigateToSearchResult result)
    {
        AdditionalInformation = result.AdditionalInformation;
        Kind = result.Kind;
        MatchKind = FSharpNavigateToMatchKindHelpers.ConvertTo(result.MatchKind);
        Name = result.Name;
        NavigableItem = new InternalFSharpNavigableItem(result.NavigableItem);
    }

    public bool IsCaseSensitive => false;

    public ImmutableArray<TextSpan> NameMatchSpans => [];

    public string SecondarySort => null;

    public string Summary => null;

    public ImmutableArray<PatternMatch> Matches => NavigateToSearchResultHelpers.GetMatches(this);
}
