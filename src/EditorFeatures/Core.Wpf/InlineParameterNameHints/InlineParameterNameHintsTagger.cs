// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.InlineParameterNameHints
{
    /// <summary>
    /// The purpose of this tagger is to convert the <see cref="InlineParameterNameHintDataTag"/> to
    /// the <see cref="InlineParameterNameHintsTag"/>, which actually creates the UIElement. It reacts to
    /// tags changing and updates the adornments accordingly.
    /// </summary>
    internal sealed class InlineParameterNameHintsTagger : ITagger<IntraTextAdornmentTag>, IDisposable
    {
        private readonly ITagAggregator<InlineParameterNameHintDataTag> _tagAggregator;
        private readonly ITextBuffer _buffer;
        private readonly ITextView _textView;

        /// <summary>
        /// stores the parameter hint tags in a global location 
        /// </summary>
        private readonly List<ITagSpan<IntraTextAdornmentTag>> _cache;

        /// <summary>
        /// Stores the snapshot associated with the cached tags in <see cref="_cache" /> 
        /// </summary>
        private ITextSnapshot? _cacheSnapshot;

        private readonly IClassificationFormatMap _formatMap;

        /// <summary>
        /// Lazy initialized, use <see cref="Format"/> for access
        /// </summary>
        private TextFormattingRunProperties? _format;
        private readonly IClassificationType _hintClassification;
        private readonly ForegroundThreadAffinitizedObject _threadAffinitizedObject;

        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

        public InlineParameterNameHintsTagger(InlineParameterNameHintsTaggerProvider taggerProvider, ITextView textView, ITextBuffer buffer, ITagAggregator<InlineParameterNameHintDataTag> tagAggregator)
        {
            _cache = new List<ITagSpan<IntraTextAdornmentTag>>();
            _threadAffinitizedObject = new ForegroundThreadAffinitizedObject(taggerProvider.ThreadingContext);
            _textView = textView;
            _buffer = buffer;
            _tagAggregator = tagAggregator;
            _formatMap = taggerProvider.ClassificationFormatMapService.GetClassificationFormatMap(textView);
            _hintClassification = taggerProvider.ClassificationTypeRegistryService.GetClassificationType(InlineParameterNameHintsTag.TagId);
            _formatMap.ClassificationFormatMappingChanged += this.OnClassificationFormatMappingChanged;
            _tagAggregator.TagsChanged += OnTagAggregatorTagsChanged;
        }

        private void OnClassificationFormatMappingChanged(object sender, EventArgs e)
        {
            _threadAffinitizedObject.AssertIsForeground();
            if (_format != null)
            {
                _format = null;
                _cache.Clear();
            }
        }

        private void OnTagAggregatorTagsChanged(object sender, TagsChangedEventArgs e)
        {
            _cache.Clear();
            var spans = e.Span.GetSpans(_buffer);
            foreach (var span in spans)
            {
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
            }
        }

        private TextFormattingRunProperties Format
        {
            get
            {
                _threadAffinitizedObject.AssertIsForeground();
                _format ??= _formatMap.GetTextProperties(_hintClassification);
                return _format;
            }
        }

        public IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                return Array.Empty<ITagSpan<IntraTextAdornmentTag>>();
            }

            var snapshot = spans[0].Snapshot;
            if (_cache.Count == 0 || snapshot != _cacheSnapshot)
            {
                // Calculate UI elements
                _cache.Clear();
                _cacheSnapshot = snapshot;

                // Calling into the InlineParameterNameHintsDataTaggerProvider which only responds with the current
                // active view and disregards and requests for tags not in that view
                var fullSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);
                var dataTags = _tagAggregator.GetTags(new NormalizedSnapshotSpanCollection(fullSpan));
                foreach (var dataTag in dataTags)
                {
                    // Gets the associated span from the snapshot span and creates the IntraTextAdornmentTag from the data
                    // tags. Only dealing with the dataTagSpans if the count is 1 because we do not see a multi-buffer case
                    // occuring 
                    var dataTagSpans = dataTag.Span.GetSpans(snapshot);
                    var textTag = dataTag.Tag;
                    if (dataTagSpans.Count == 1)
                    {
                        var dataTagSpan = dataTagSpans[0];
                        _cache.Add(new TagSpan<IntraTextAdornmentTag>(new SnapshotSpan(dataTagSpan.Start, 0), new InlineParameterNameHintsTag(textTag.ParameterName, _textView.LineHeight, Format)));
                    }
                }
            }

            var selectedSpans = new List<ITagSpan<IntraTextAdornmentTag>>();
            foreach (var tagSpan in _cache)
            {
                if (spans.IntersectsWith(tagSpan.Span))
                {
                    selectedSpans.Add(tagSpan);
                }
            }

            return selectedSpans;
        }

        public void Dispose()
        {
            _tagAggregator.TagsChanged -= OnTagAggregatorTagsChanged;
            _tagAggregator.Dispose();
            _formatMap.ClassificationFormatMappingChanged -= OnClassificationFormatMappingChanged;
        }
    }
}
