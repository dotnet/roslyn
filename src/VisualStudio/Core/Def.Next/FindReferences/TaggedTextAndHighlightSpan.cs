using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
