// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal interface INavigateToSearchResult
    {
        string AdditionalInformation { get; }
        string Kind { get; }
        MatchKind MatchKind { get; }
        bool IsCaseSensitive { get; }
        string Name { get; }
        string SecondarySort { get; }
        string Summary { get; }

        INavigableItem NavigableItem { get; }
    }
}
