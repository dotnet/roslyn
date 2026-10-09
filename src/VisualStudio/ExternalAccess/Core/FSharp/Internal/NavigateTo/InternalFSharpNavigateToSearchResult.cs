// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
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
    private readonly Lazy<string> _secondarySort;

    public string AdditionalInformation { get; }
    public string Kind { get; }
    public NavigateToMatchKind MatchKind { get; }
    public string Name { get; }
    public INavigableItem NavigableItem { get; }

    /// <param name="activeDocument">The document the user was editing when they invoked the navigate-to
    /// operation, or <see langword="null"/> when the search was not invoked from a document.</param>
    public InternalFSharpNavigateToSearchResult(FSharpNavigateToSearchResult result, Document? activeDocument)
    {
        AdditionalInformation = result.AdditionalInformation;
        Kind = result.Kind;
        MatchKind = FSharpNavigateToMatchKindHelpers.ConvertTo(result.MatchKind);
        IsCaseSensitive = result.IsCaseSensitive;
        Name = result.Name;
        NameMatchSpans = result.NameMatchSpans;
        NavigableItem = new InternalFSharpNavigableItem(result.NavigableItem);

        (DocumentId id, IReadOnlyList<string> folders)? active = activeDocument is null ? null : (activeDocument.Id, activeDocument.Folders);
        _secondarySort = new Lazy<string>(() => NavigateToSearchResultHelpers.ComputeSecondarySort(
            active, NavigableItem.Document, result.ParameterCount, result.TypeParameterCount, result.Name));
    }

    public bool IsCaseSensitive { get; }

    public ImmutableArray<TextSpan> NameMatchSpans { get; }

    public string SecondarySort => _secondarySort.Value;

    public string? Summary => null;

    public ImmutableArray<PatternMatch> Matches => NavigateToSearchResultHelpers.GetMatches(this);
}
