// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
