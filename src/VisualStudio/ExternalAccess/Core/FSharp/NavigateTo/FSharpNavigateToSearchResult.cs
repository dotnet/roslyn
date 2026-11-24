// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo;

internal class FSharpNavigateToSearchResult
{
    public FSharpNavigateToSearchResult(
        string additionalInformation,
        string kind,
        FSharpNavigateToMatchKind matchKind,
        string name,
        FSharpNavigableItem navigateItem)
        : this(additionalInformation, kind, matchKind, isCaseSensitive: false, name, nameMatchSpans: [], navigateItem, parameterCount: 0, typeParameterCount: 0)
    {
    }

    /// <param name="additionalInformation">The text Navigate To shows beside <paramref name="name"/>, such as its container and project.</param>
    /// <param name="kind">The kind of the declaration, one of the <see cref="FSharpNavigateToItemKind"/> values.</param>
    /// <param name="matchKind">How <paramref name="name"/> matched the search pattern.</param>
    /// <param name="isCaseSensitive">Whether <paramref name="name"/> matched the search pattern in the pattern's case.</param>
    /// <param name="name">The name of the declaration, as Navigate To shows it.</param>
    /// <param name="nameMatchSpans">The spans of <paramref name="name"/> that matched the search pattern.</param>
    /// <param name="navigateItem">The declaration Navigate To goes to when the result is chosen.</param>
    /// <param name="parameterCount">Breaks ties between otherwise equal results, as for a C# or VB declaration.</param>
    /// <param name="typeParameterCount">Breaks the ties <paramref name="parameterCount"/> leaves.</param>
    public FSharpNavigateToSearchResult(
        string additionalInformation,
        string kind,
        FSharpNavigateToMatchKind matchKind,
        bool isCaseSensitive,
        string name,
        ImmutableArray<TextSpan> nameMatchSpans,
        FSharpNavigableItem navigateItem,
        int parameterCount,
        int typeParameterCount)
    {
        AdditionalInformation = additionalInformation;
        Kind = kind;
        Name = name;
        MatchKind = matchKind;
        IsCaseSensitive = isCaseSensitive;
        NameMatchSpans = nameMatchSpans.NullToEmpty();
        NavigableItem = navigateItem;
        ParameterCount = parameterCount;
        TypeParameterCount = typeParameterCount;
    }

    public string AdditionalInformation { get; }

    public string Kind { get; }

    public FSharpNavigateToMatchKind MatchKind { get; }

    public bool IsCaseSensitive { get; }

    public string Name { get; }

    public ImmutableArray<TextSpan> NameMatchSpans { get; }

    public FSharpNavigableItem NavigableItem { get; }

    public int ParameterCount { get; }

    public int TypeParameterCount { get; }
}
