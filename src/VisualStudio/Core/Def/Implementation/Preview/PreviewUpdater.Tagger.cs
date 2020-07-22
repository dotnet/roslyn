// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview
{
    internal partial class PreviewUpdater
    {
        internal class PreviewTagger : ITagger<HighlightTag>
        {
            private readonly ITextBuffer _textBuffer;

            public PreviewTagger(ITextBuffer textBuffer)
            {
                _textBuffer = textBuffer;
            }

            public void OnTextBufferChanged()
            {
                if (PreviewUpdater.SpanToShow != default)
                {
                    if (TagsChanged != null)
                    {
                        var span = _textBuffer.CurrentSnapshot.GetFullSpan();
                        TagsChanged(this, new SnapshotSpanEventArgs(span));
                    }
                }
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

            public IEnumerable<ITagSpan<HighlightTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                var lines = _textBuffer.CurrentSnapshot.Lines.Where(line => line.Extent.OverlapsWith(PreviewUpdater.SpanToShow));

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
}
