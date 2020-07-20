// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public string AdditionalInformation { get; }

        public string Kind { get; }

        public NavigateToMatchKind MatchKind { get; }

        public bool IsCaseSensitive => false;

        public string Name { get; }

        public ImmutableArray<TextSpan> NameMatchSpans => ImmutableArray<TextSpan>.Empty;

        public string SecondarySort => null;

        public string Summary => null;

        public INavigableItem NavigableItem { get; }
    }
}
