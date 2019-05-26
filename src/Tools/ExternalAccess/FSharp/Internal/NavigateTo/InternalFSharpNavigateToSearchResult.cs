// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Navigation;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.NavigateTo
{
    internal class InternalFSharpNavigateToSearchResult : INavigateToSearchResult
    {
        public InternalFSharpNavigateToSearchResult(FSharpNavigateToSearchResult result)
        {
            AdditionalInformation = result.AdditionalInformation;
            Kind = result.Kind;
            MatchKind = FSharpNavigateToMatchKindHelpers.ConvertTo(result.MatchKind);
            Name = result.Name;
            NavigableItem = new InternalFSharpNavigableItem(result.NavigableItem);
        }

        public string AdditionalInformation { get; private set; }

        public string Kind { get; private set; }

        public NavigateToMatchKind MatchKind { get; private set; }

        public bool IsCaseSensitive => false;

        public string Name { get; private set; }

        public ImmutableArray<TextSpan> NameMatchSpans => ImmutableArray<TextSpan>.Empty;

        public string SecondarySort => null;

        public string Summary => null;

        public INavigableItem NavigableItem { get; private set; }
    }
}
