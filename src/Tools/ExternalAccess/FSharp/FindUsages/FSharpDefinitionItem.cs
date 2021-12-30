// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.FindUsages
{
    internal class FSharpDefinitionItem
    {
        private readonly Microsoft.CodeAnalysis.FindUsages.DefinitionItem _roslynDefinitionItem;

        private FSharpDefinitionItem(Microsoft.CodeAnalysis.FindUsages.DefinitionItem roslynDefinitionItem)
        {
            _roslynDefinitionItem = roslynDefinitionItem;
        }

        internal Microsoft.CodeAnalysis.FindUsages.DefinitionItem RoslynDefinitionItem
        {
            get
            {
                return _roslynDefinitionItem;
            }
        }

        public static FSharpDefinitionItem Create(ImmutableArray<string> tags, ImmutableArray<TaggedText> displayParts, FSharpDocumentSpan sourceSpan)
        {
            return new FSharpDefinitionItem(Microsoft.CodeAnalysis.FindUsages.DefinitionItem.Create(tags, displayParts, sourceSpan.ToRoslynDocumentSpan()));
        }

        public static FSharpDefinitionItem CreateNonNavigableItem(ImmutableArray<string> tags, ImmutableArray<TaggedText> displayParts, ImmutableArray<TaggedText> originationParts)
        {
            return new FSharpDefinitionItem(Microsoft.CodeAnalysis.FindUsages.DefinitionItem.CreateNonNavigableItem(tags, displayParts, originationParts));
        }
    }
}
