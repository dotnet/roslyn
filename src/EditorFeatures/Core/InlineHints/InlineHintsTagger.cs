// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.InlineHints;

internal partial class InlineHintsTaggerProvider
{
    /// <summary>
    /// The purpose of this tagger is to convert the <see cref="InlineHintDataTag{TAdditionalInformation}"/> to the <see
    /// cref="InlineHintsTag"/>, which actually creates the UIElement. It reacts to tags changing and updates the
    /// adornments accordingly.
    /// </summary>
    private sealed class InlineHintsTagger : EfficientTagger<IntraTextAdornmentTag>
    {
        private readonly EfficientTagger<InlineHintDataTag<CachedAdornmentTagSpan>> _underlyingTagger;

        private readonly IClassificationFormatMap _formatMap;

        /// <summary>
        /// Lazy initialized, use <see cref="Format"/> for access
        /// </summary>
        private TextFormattingRunProperties? _format;
        private readonly IClassificationType _hintClassification;

        private readonly InlineHintsTaggerProvider _taggerProvider;

        private readonly IWpfTextView _textView;
        private readonly ITextBuffer _subjectBuffer;

        public InlineHintsTagger(
            InlineHintsTaggerProvider taggerProvider,
            IWpfTextView textView,
            ITextBuffer subjectBuffer,
            EfficientTagger<InlineHintDataTag<CachedAdornmentTagSpan>> tagger)
        {
            _taggerProvider = taggerProvider;

            _textView = textView;
            _subjectBuffer = subjectBuffer;

            // When the underlying tagger produced new data tags, inform any clients of us that we have new adornment tags.
            _underlyingTagger = tagger;
            _underlyingTagger.TagsChanged += OnTagsChanged;

            _formatMap = taggerProvider.ClassificationFormatMapService.GetClassificationFormatMap(textView);
            _hintClassification = taggerProvider.ClassificationTypeRegistryService.GetClassificationType(InlineHintsTag.TagId);

            _formatMap.ClassificationFormatMappingChanged += this.OnClassificationFormatMappingChanged;
            _taggerProvider.GlobalOptionService.AddOptionChangedHandler(this, OnGlobalOptionChanged);
        }

        public override void Dispose()
        {
            _formatMap.ClassificationFormatMappingChanged -= OnClassificationFormatMappingChanged;
            _taggerProvider.GlobalOptionService.RemoveOptionChangedHandler(this, OnGlobalOptionChanged);
            _underlyingTagger.TagsChanged -= OnTagsChanged;
            _underlyingTagger.Dispose();
        }

        private void OnClassificationFormatMappingChanged(object sender, EventArgs e)
        {
            _taggerProvider.ThreadingContext.ThrowIfNotOnUIThread();

            // When classifications change we need to rebuild the inline tags with updated Font and Color information.

            if (_format != null)
            {
                _format = null;
                OnTagsChanged(this, new SnapshotSpanEventArgs(_subjectBuffer.CurrentSnapshot.GetFullSpan()));
            }
        }

        private void OnGlobalOptionChanged(object sender, object target, OptionChangedEventArgs e)
        {
            // Reclassify everything.
            if (e.HasOption(option => option.Equals(InlineHintsViewOptionsStorage.ColorHints)))
                OnTagsChanged(this, new SnapshotSpanEventArgs(_subjectBuffer.CurrentSnapshot.GetFullSpan()));
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

        public override void AddTags(
            NormalizedSnapshotSpanCollection spans,
            SegmentedList<TagSpan<IntraTextAdornmentTag>> adornmentTagSpans)
        {
            try
            {
                if (spans.Count == 0)
                    return;

                // If the snapshot has changed, we can't use any of the cached data, as it is associated with the
                // original snapshot they were created against.
                var snapshot = spans[0].Snapshot;

                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document is null)
                    return;

                var colorHints = _taggerProvider.EditorOptionsService.GlobalOptions.GetOption(InlineHintsViewOptionsStorage.ColorHints, document.Project.Language);

                using var _1 = SegmentedListPool.GetPooledList<TagSpan<InlineHintDataTag<CachedAdornmentTagSpan>>>(out var dataTagSpans);
                _underlyingTagger.AddTags(spans, dataTagSpans);

                // Presize so we can add the elements below without continually resizing.
                adornmentTagSpans.Capacity += dataTagSpans.Count;

                using var _2 = PooledHashSet<int>.GetInstance(out var seenPositions);

                var format = this.Format;
                foreach (var dataTagSpan in dataTagSpans)
                {
                    if (seenPositions.Add(dataTagSpan.Span.Start))
                        adornmentTagSpans.Add(GetOrCreateAdornmentTagsSpan(dataTagSpan, colorHints, format));
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.General))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private TagSpan<IntraTextAdornmentTag> GetOrCreateAdornmentTagsSpan(
            TagSpan<InlineHintDataTag<CachedAdornmentTagSpan>> dataTagSpan, bool classify, TextFormattingRunProperties format)
        {
            // If we've never computed the adornment info, or options have changed, then compute and cache the new information.
            var cachedTagInformation = dataTagSpan.Tag.AdditionalData;
            if (cachedTagInformation is null || cachedTagInformation.Classified != classify || cachedTagInformation.Format != format)
            {
                var adornmentSpan = dataTagSpan.Span;
                cachedTagInformation = new(classify, format, new TagSpan<IntraTextAdornmentTag>(adornmentSpan, InlineHintsTag.Create(
                    dataTagSpan.Tag.Hint, format, _textView, adornmentSpan, _taggerProvider, _formatMap, classify)));
                dataTagSpan.Tag.AdditionalData = cachedTagInformation;
            }

            return cachedTagInformation.AdornmentTagSpan;
        }
    }
}
