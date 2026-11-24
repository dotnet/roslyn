// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.IO;
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

    public InternalFSharpNavigateToSearchResult(FSharpNavigateToSearchResult result, Document activeDocument)
    {
        AdditionalInformation = result.AdditionalInformation;
        Kind = result.Kind;
        MatchKind = FSharpNavigateToMatchKindHelpers.ConvertTo(result.MatchKind);
        IsCaseSensitive = result.IsCaseSensitive;
        Name = result.Name;
        NameMatchSpans = result.NameMatchSpans;
        NavigableItem = new InternalFSharpNavigableItem(result.NavigableItem);
        _secondarySort = new Lazy<string>(() => ComputeSecondarySort(result, NavigableItem.Document, activeDocument));
    }

    public bool IsCaseSensitive { get; }

    public ImmutableArray<TextSpan> NameMatchSpans { get; }

    public string SecondarySort => _secondarySort.Value;

    public string Summary => null;

    public ImmutableArray<PatternMatch> Matches => NavigateToSearchResultHelpers.GetMatches(this);

    // The key RoslynNavigateToItem builds for C# and VB results, so that equal F# results sort the same way.
    private static string ComputeSecondarySort(FSharpNavigateToSearchResult result, INavigableItem.NavigableDocument itemDocument, Document activeDocument)
        => string.Join(" ", [
            ComputeFolderDistance(itemDocument, activeDocument).ToString("X4"),
            result.ParameterCount.ToString("X4"),
            result.TypeParameterCount.ToString("X4"),
            result.Name,
            .. Path.GetFileNameWithoutExtension(itemDocument.FilePath ?? "").Split('.')]);

    private static int ComputeFolderDistance(INavigableItem.NavigableDocument itemDocument, Document activeDocument)
    {
        if (activeDocument is null || activeDocument.Id == itemDocument.Id)
            return 0;

        var commonCount = 0;
        while (commonCount < activeDocument.Folders.Count && commonCount < itemDocument.Folders.Count
            && activeDocument.Folders[commonCount] == itemDocument.Folders[commonCount])
        {
            commonCount++;
        }

        return activeDocument.Folders.Count + itemDocument.Folders.Count - 2 * commonCount + 1;
    }
}
