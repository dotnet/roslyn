﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
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

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ProjectionSpanTaggerProvider()
        {
        }

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
                if (!_textView.Properties.TryGetProperty(PropertyName, out List<Span> allSpans))
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
