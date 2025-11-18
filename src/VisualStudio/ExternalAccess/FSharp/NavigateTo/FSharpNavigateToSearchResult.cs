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
    {
        AdditionalInformation = additionalInformation;
        Kind = kind;
        Name = name;
        MatchKind = matchKind;
        NavigableItem = navigateItem;
    }

    public string AdditionalInformation { get; }

    public string Kind { get; }

    public FSharpNavigateToMatchKind MatchKind { get; }

    public string Name { get; }

    public FSharpNavigableItem NavigableItem { get; }
}
