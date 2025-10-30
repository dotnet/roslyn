// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview;

internal partial class PreviewUpdater
{
    internal sealed class PreviewTagger : ITagger<HighlightTag>
    {
        private readonly ITextBuffer _textBuffer;
        private Span _span;

        public PreviewTagger(ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
        }

        public Span Span
        {
            get
            {
                return _span;
            }

            set
            {
                _span = value;

                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(_textBuffer.CurrentSnapshot.GetFullSpan()));
            }
        }

        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

        IEnumerable<ITagSpan<HighlightTag>> ITagger<HighlightTag>.GetTags(NormalizedSnapshotSpanCollection spans)
            => GetTags();

        public IEnumerable<TagSpan<HighlightTag>> GetTags()
        {
            var lines = _textBuffer.CurrentSnapshot.Lines.Where(line => line.Extent.OverlapsWith(_span));

            foreach (var line in lines)
            {
                var firstNonWhitespace = line.GetFirstNonWhitespacePosition();
                var lastNonWhitespace = line.GetLastNonWhitespacePosition();

                if (firstNonWhitespace.HasValue && lastNonWhitespace.HasValue)
                {
                    yield return new TagSpan<HighlightTag>(new SnapshotSpan(_textBuffer.CurrentSnapshot, Span.FromBounds(firstNonWhitespace.Value, lastNonWhitespace.Value + 1)), new HighlightTag());
                }
            }
        }
    }
}
