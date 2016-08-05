// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal struct TaggedTextAndHighlightSpan
    {
        public readonly ImmutableArray<TaggedText> TaggedText;
        public readonly TextSpan HighlightSpan;

        public TaggedTextAndHighlightSpan(ImmutableArray<TaggedText> taggedText, TextSpan highlightSpan)
        {
            TaggedText = taggedText;
            HighlightSpan = highlightSpan;
        }
    }
}