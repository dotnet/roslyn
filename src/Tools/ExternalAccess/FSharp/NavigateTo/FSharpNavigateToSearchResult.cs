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
            MatchKind = matchKind;
            NavigableItem = navigateItem;
        }

        public string AdditionalInformation { get; private set; }

        public string Kind { get; private set; }

        public FSharpNavigateToMatchKind MatchKind { get; private set; }

        public string Name { get; private set; }

        public FSharpNavigableItem NavigableItem { get; private set; }
    }
}
