// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.InlineParameterNameHints
{
    /// <summary>
    /// The purpose of this tagger is to convert the <see cref="InlineParamNameHintDataTag"/> to
    /// the <see cref="InlineParamNameHintsTag"/>, which actually creates the UIElement. It reacts to
    /// tags changing and updates the adornments accordingly.
    /// </summary>
    internal sealed class InlineParamNameHintsTagger : ITagger<IntraTextAdornmentTag>, IDisposable
    {
        private readonly ITagAggregator<InlineParamNameHintDataTag> _tagAggregator;
        private readonly ITextBuffer _buffer;
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public InlineParamNameHintsTagger(ITextBuffer buffer, ITagAggregator<InlineParamNameHintDataTag> tagAggregator)
        {
            _buffer = buffer;
            _tagAggregator = tagAggregator;
            _tagAggregator.TagsChanged += OnTagAggregatorTagsChanged;
        }

        private void OnTagAggregatorTagsChanged(object sender, TagsChangedEventArgs e)
        {
            var spans = e.Span.GetSpans(_buffer);
            foreach (var span in spans)
            {
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));//new SnapshotSpan(span.Snapshot, 0, span.Snapshot.Length)));
            }
        }

        public IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var tagsList = new List<ITagSpan<IntraTextAdornmentTag>>();
            var dataTags = _tagAggregator.GetTags(spans);

            if (spans.Count <= 0)
            {
                return tagsList;
            }

            foreach (var tag in dataTags)
            {
                var dataTagSpans = tag.Span.GetSpans(spans[0].Snapshot);
                var textTag = tag.Tag;
                if (dataTagSpans.Count > 0)
                {
                    var dataTagSpan = dataTagSpans[0];
                    var adornmentSpan = new SnapshotSpan(dataTagSpan.Start, 0);
                    tagsList.Add(new TagSpan<IntraTextAdornmentTag>(adornmentSpan, new InlineParamNameHintsTag(textTag.ParameterName)));
                }
            }
            return tagsList;
        }

        public void Dispose()
        {
            _tagAggregator.Dispose();
        }
    }
}
