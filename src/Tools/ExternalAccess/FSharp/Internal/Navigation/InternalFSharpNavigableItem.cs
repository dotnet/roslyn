// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Navigation
{
    internal class InternalFSharpNavigableItem : INavigableItem
    {
        public InternalFSharpNavigableItem(FSharpNavigableItem item)
        {
            Document = item.Document;
            SourceSpan = item.SourceSpan;
        }

        public Glyph Glyph => Glyph.BasicFile;

        public ImmutableArray<TaggedText> DisplayTaggedParts => ImmutableArray<TaggedText>.Empty;

        public bool DisplayFileLocation => true;

        public bool IsImplicitlyDeclared => false;

        public Document Document { get; private set; }

        public TextSpan SourceSpan { get; private set; }

        public ImmutableArray<INavigableItem> ChildItems => ImmutableArray<INavigableItem>.Empty;
    }
}
