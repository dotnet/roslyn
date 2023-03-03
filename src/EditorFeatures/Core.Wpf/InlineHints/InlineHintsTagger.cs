﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

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
        private readonly List<(IMappingTagSpan<InlineHintDataTag> mappingTagSpan, ITagSpan<IntraTextAdornmentTag>? tagSpan)> _cache = new();

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
            _taggerProvider = taggerProvider;

            _textView = textView;
            _buffer = buffer;

            _tagAggregator = tagAggregator;
            _formatMap = taggerProvider.ClassificationFormatMapService.GetClassificationFormatMap(textView);
            _hintClassification = taggerProvider.ClassificationTypeRegistryService.GetClassificationType(InlineHintsTag.TagId);
            _formatMap.ClassificationFormatMappingChanged += this.OnClassificationFormatMappingChanged;
            _tagAggregator.BatchedTagsChanged += TagAggregator_BatchedTagsChanged;
        }

        /// <summary>
        /// Goes through all the spans in which tags have changed and
        /// invokes a TagsChanged event. Using the BatchedTagsChangedEvent since it is raised
        /// on the same thread that created the tag aggregator, unlike TagsChanged.
        /// </summary>
        private void TagAggregator_BatchedTagsChanged(object sender, BatchedTagsChangedEventArgs e)
        {
            _taggerProvider.ThreadingContext.ThrowIfNotOnUIThread();
            InvalidateCache();

            var tagsChanged = TagsChanged;
            if (tagsChanged is null)
            {
                return;
            }

            var mappingSpans = e.Spans;
            foreach (var item in mappingSpans)
            {
                var spans = item.GetSpans(_buffer);
                foreach (var span in spans)
                {
                    if (tagsChanged != null)
                    {
                        tagsChanged.Invoke(this, new SnapshotSpanEventArgs(span));
                    }
                }
            }
        }

        private void OnClassificationFormatMappingChanged(object sender, EventArgs e)
        {
            _taggerProvider.ThreadingContext.ThrowIfNotOnUIThread();
            if (_format != null)
            {
                _format = null;
                InvalidateCache();

                // When classifications change we need to rebuild the inline tags with updated Font and Color information.
                var tags = GetTags(new NormalizedSnapshotSpanCollection(_textView.TextViewLines.FormattedSpan));

                foreach (var tag in tags)
                {
                    TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(tag.Span));
                }
            }
        }

        private TextFormattingRunProperties Format
        {
            get
            {
                _taggerProvider.ThreadingContext.ThrowIfNotOnUIThread();
                _format ??= _formatMap.GetTextProperties(_hintClassification);
                return _format;
            }
        }

        private void InvalidateCache()
        {
            _taggerProvider.ThreadingContext.ThrowIfNotOnUIThread();
            _cacheSnapshot = null;
            _cache.Clear();
        }

        public IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            try
            {
                if (spans.Count == 0)
                {
                    return Array.Empty<ITagSpan<IntraTextAdornmentTag>>();
                }

                var snapshot = spans[0].Snapshot;
                if (snapshot != _cacheSnapshot)
                {
                    // Calculate UI elements
                    _cache.Clear();
                    _cacheSnapshot = snapshot;

                    // Calling into the InlineParameterNameHintsDataTaggerProvider which only responds with the current
                    // active view and disregards and requests for tags not in that view
                    var fullSpan = new SnapshotSpan(snapshot, 0, snapshot.Length);
                    var tags = _tagAggregator.GetTags(new NormalizedSnapshotSpanCollection(fullSpan));
                    foreach (var tag in tags)
                    {
                        // Gets the associated span from the snapshot span and creates the IntraTextAdornmentTag from the data
                        // tags. Only dealing with the dataTagSpans if the count is 1 because we do not see a multi-buffer case
                        // occurring
                        var dataTagSpans = tag.Span.GetSpans(snapshot);
                        if (dataTagSpans.Count == 1)
                        {
                            _cache.Add((tag, tagSpan: null));
                        }
                    }
                }

                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                var classify = document != null && _taggerProvider.EditorOptionsService.GlobalOptions.GetOption(InlineHintsViewOptionsStorage.ColorHints, document.Project.Language);

                var selectedSpans = new List<ITagSpan<IntraTextAdornmentTag>>();
                for (var i = 0; i < _cache.Count; i++)
                {
                    var tagSpans = _cache[i].mappingTagSpan.Span.GetSpans(snapshot);
                    if (tagSpans.Count == 1)
                    {
                        var tagSpan = tagSpans[0];
                        if (spans.IntersectsWith(tagSpan))
                        {
                            if (_cache[i].tagSpan is not { } hintTagSpan)
                            {
                                var hintUITag = InlineHintsTag.Create(
                                        _cache[i].mappingTagSpan.Tag.Hint, Format, _textView, tagSpan, _taggerProvider, _formatMap, classify);

                                hintTagSpan = new TagSpan<IntraTextAdornmentTag>(tagSpan, hintUITag);
                                _cache[i] = (_cache[i].mappingTagSpan, hintTagSpan);
                            }

                            selectedSpans.Add(hintTagSpan);
                        }
                    }
                }

                return selectedSpans;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.General))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        public void Dispose()
        {
            _tagAggregator.BatchedTagsChanged -= TagAggregator_BatchedTagsChanged;
            _tagAggregator.Dispose();
            _formatMap.ClassificationFormatMappingChanged -= OnClassificationFormatMappingChanged;
        }
    }
}
