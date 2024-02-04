// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.FindUsages
{
    internal class FSharpDefinitionItem
    {
        private readonly DefinitionItem _roslynDefinitionItem;

        private FSharpDefinitionItem(DefinitionItem roslynDefinitionItem)
        {
            _roslynDefinitionItem = roslynDefinitionItem;
        }

        internal DefinitionItem RoslynDefinitionItem
            => _roslynDefinitionItem;

        public static FSharpDefinitionItem Create(ImmutableArray<string> tags, ImmutableArray<TaggedText> displayParts, FSharpDocumentSpan sourceSpan)
            => new(
                DefinitionItem.Create(
                    tags,
                    displayParts,
                    sourceSpans: [sourceSpan.ToRoslynDocumentSpan()],
                    classifiedSpans: [null],
                    metadataLocations: [],
                    nameDisplayParts: default,
                    displayIfNoReferences: true));

        public static FSharpDefinitionItem CreateNonNavigableItem(ImmutableArray<string> tags, ImmutableArray<TaggedText> displayParts, ImmutableArray<TaggedText> originationParts)
            => new(
                DefinitionItem.CreateNonNavigableItem(
                    tags,
                    displayParts,
                    originationParts));
    }
}
