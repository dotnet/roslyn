// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Roslyn.Hosting.Diagnostics.VenusMargin
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("text")]
    [TagType(typeof(TextMarkerTag))]
    internal class ProjectionSpanTaggerProvider : IViewTaggerProvider
    {
        public const string PropertyName = "Projection Tags";

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            return new Tagger(textView) as ITagger<T>;
        }

        internal class Tagger : ITagger<TextMarkerTag>, IDisposable
        {
            private readonly ITextView _textView;

            public Tagger(ITextView textView)
            {
                _textView = textView;

                ProjectionBufferMargin.SelectionChanged += OnProjectionBufferMarginSelectionChanged;
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

            private void OnProjectionBufferMarginSelectionChanged(object sender, EventArgs e)
            {
                var snapshot = _textView.TextBuffer.CurrentSnapshot;
                RaiseTagsChanged(new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, new Span(0, snapshot.Length))));
            }

            private void RaiseTagsChanged(SnapshotSpanEventArgs args)
            {
                this.TagsChanged?.Invoke(this, args);
            }

            public IEnumerable<ITagSpan<TextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                List<Span> allSpans;
                if (!_textView.Properties.TryGetProperty(PropertyName, out allSpans))
                {
                    return null;
                }

                return allSpans
                    .Where(s => spans.Any(ss => ss.IntersectsWith(s)))
                    .Select(s => new TagSpan<TextMarkerTag>(new SnapshotSpan(spans.First().Snapshot, s), ProjectionSpanTag.Instance));
            }

            public void Dispose()
            {
                ProjectionBufferMargin.SelectionChanged -= OnProjectionBufferMarginSelectionChanged;
            }
        }
    }
}
