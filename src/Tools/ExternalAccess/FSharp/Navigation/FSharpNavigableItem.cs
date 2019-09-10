// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation
{
    internal class FSharpNavigableItem
    {
        public FSharpNavigableItem(FSharpGlyph glyph, ImmutableArray<TaggedText> displayTaggedParts, Document document, TextSpan sourceSpan)
        {
            Glyph = glyph;
            DisplayTaggedParts = displayTaggedParts;
            Document = document;
            SourceSpan = sourceSpan;
        }

        public FSharpGlyph Glyph { get; }

        public ImmutableArray<TaggedText> DisplayTaggedParts { get; }

        public Document Document { get; }

        public TextSpan SourceSpan { get; }
    }
}
