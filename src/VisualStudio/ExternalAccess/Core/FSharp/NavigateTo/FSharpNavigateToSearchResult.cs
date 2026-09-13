// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo;

internal class FSharpNavigateToSearchResult
{
    public FSharpNavigateToSearchResult(
        string additionalInformation,
        string kind,
        FSharpNavigateToMatchKind matchKind,
        string name,
        FSharpNavigableItem navigateItem)
        : this(additionalInformation, kind, matchKind, name, navigateItem, parameterCount: 0, typeParameterCount: 0)
    {
    }

    /// <param name="parameterCount">The number of parameters the declaration takes, if any.  Results that are
    /// otherwise equal are sorted by it, as they are for a C# or VB declaration.</param>
    /// <param name="typeParameterCount">The number of type parameters the declaration takes, if any.  Results that
    /// are otherwise equal, including in <paramref name="parameterCount"/>, are sorted by it.</param>
    public FSharpNavigateToSearchResult(
        string additionalInformation,
        string kind,
        FSharpNavigateToMatchKind matchKind,
        string name,
        FSharpNavigableItem navigateItem,
        int parameterCount,
        int typeParameterCount)
    {
        AdditionalInformation = additionalInformation;
        Kind = kind;
        Name = name;
        MatchKind = matchKind;
        NavigableItem = navigateItem;
        ParameterCount = parameterCount;
        TypeParameterCount = typeParameterCount;
    }

    public string AdditionalInformation { get; }

    public string Kind { get; }

    public FSharpNavigateToMatchKind MatchKind { get; }

    public string Name { get; }

    public FSharpNavigableItem NavigableItem { get; }

    public int ParameterCount { get; }

    public int TypeParameterCount { get; }
}
