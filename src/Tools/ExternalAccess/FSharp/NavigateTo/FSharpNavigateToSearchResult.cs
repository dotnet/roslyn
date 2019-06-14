// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo
{
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
}
