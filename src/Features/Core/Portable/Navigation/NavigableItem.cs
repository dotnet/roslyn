// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal class NavigableItem : INavigableItem
    {
        public Document Document { get; }
        public TextSpan SourceSpan { get; }
        public Glyph Glyph { get; }
        public ImmutableArray<TaggedText> DisplayTaggedParts { get; }

        public bool DisplayFileLocation => false;
        public bool IsImplicitlyDeclared => false;
        public ImmutableArray<INavigableItem> ChildItems => ImmutableArray<INavigableItem>.Empty;

        public NavigableItem(
            Document document, TextSpan sourceSpan,
            Glyph glyph, ImmutableArray<TaggedText> displayTaggedParts)
        {
            Glyph = glyph;
            DisplayTaggedParts = displayTaggedParts;
            Document = document;
            SourceSpan = sourceSpan;
        }
    }
}