﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    /// <summary>
    /// The purpose of this tagger is to convert the <see cref="InlineHintDataTag"/> to the <see
    /// cref="InlineHintsTag"/>, which actually creates the UIElement. It reacts to tags changing and updates the
    /// adornments accordingly.
    /// </summary>
    internal sealed class InlineHintsTagger : ITagger<IntraTextAdornmentTag>, IDisposable
    {
        private readonly ITagAggregator<InlineHintDataTag> _tagAggregator;

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
        private readonly InlineHintsTaggerProvider _taggerProvider;

        private readonly ITextBuffer _buffer;
        private readonly IWpfTextView _textView;

        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

        public InlineHintsTagger(
            InlineHintsTaggerProvider taggerProvider,
            IWpfTextView textView,
            ITextBuffer buffer,
            ITagAggregator<InlineHintDataTag> tagAggregator)
        {
            _cache = new List<ITagSpan<IntraTextAdornmentTag>>();

            _threadAffinitizedObject = new ForegroundThreadAffinitizedObject(taggerProvider.ThreadingContext);
            _taggerProvider = taggerProvider;

            _textView = textView;
            _buffer = buffer;

            _tagAggregator = tagAggregator;
            _formatMap = taggerProvider.ClassificationFormatMapService.GetClassificationFormatMap(textView);
            _hintClassification = taggerProvider.ClassificationTypeRegistryService.GetClassificationType(InlineHintsTag.TagId);
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

                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                var classify = document?.Project.Solution.Workspace.Options.GetOption(InlineHintsOptions.ColorHints, document?.Project.Language) ?? false;

                // Calling into the InlineParameterNameHintsDataTaggerProvider which only responds with the current
                // active view and disregards and requests for tags not in that view
                var fullSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);
                var tags = _tagAggregator.GetTags(new NormalizedSnapshotSpanCollection(fullSpan));
                foreach (var tag in tags)
                {
                    // Gets the associated span from the snapshot span and creates the IntraTextAdornmentTag from the data
                    // tags. Only dealing with the dataTagSpans if the count is 1 because we do not see a multi-buffer case
                    // occuring 
                    var dataTagSpans = tag.Span.GetSpans(snapshot);
                    if (dataTagSpans.Count == 1)
                    {
                        var dataTagSpan = dataTagSpans[0];
                        var parameterHintUITag = InlineHintsTag.Create(
                            tag.Tag.Hint, Format, _textView, dataTagSpan, _taggerProvider, _formatMap, classify);

                        _cache.Add(new TagSpan<IntraTextAdornmentTag>(dataTagSpan, parameterHintUITag));
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
