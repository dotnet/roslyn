// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    using InlineHintTagCache = ImmutableDictionary<int, InlineHintTags>;

    internal class InlineHintTags(TagSpan<InlineHintDataTag> dataTagSpan)
    {
        /// <summary>
        /// Provided at creation time.  Never changes.
        /// </summary>
        public readonly TagSpan<InlineHintDataTag> DataTagSpan = dataTagSpan;

        /// <summary>
        /// Created on demand when the adornment is needed.
        /// </summary>
        public TagSpan<IntraTextAdornmentTag>? AdornmentTagSpan;
    }

    /// <summary>
    /// The purpose of this tagger is to convert the <see cref="InlineHintDataTag"/> to the <see
    /// cref="InlineHintsTag"/>, which actually creates the UIElement. It reacts to tags changing and updates the
    /// adornments accordingly.
    /// </summary>
    internal sealed class InlineHintsTagger : EfficientTagger<IntraTextAdornmentTag>
    {
        private readonly EfficientTagger<InlineHintDataTag> _underlyingTagger;

        private readonly IClassificationFormatMap _formatMap;

        /// <summary>
        /// Lazy initialized, use <see cref="Format"/> for access
        /// </summary>
        private TextFormattingRunProperties? _format;
        private readonly IClassificationType _hintClassification;

        private readonly InlineHintsTaggerProvider _taggerProvider;

        private readonly IWpfTextView _textView;

        private readonly object _gate = new();
        /// <summary>
        /// Stores the snapshot associated with the cached tags in <see cref="_cache_doNotAccessOutsideOfGate"/>.
        /// Locked by <see cref="_gate"/>.
        /// </summary>
        private ITextSnapshot? _cacheSnapshot_doNotAccessOutsideOfGate;

        /// <summary>
        /// Mapping from position to the data tag computed for it, and the adornment tag (once we've computed that).
        /// Locked by <see cref="_gate"/>.
        /// </summary>
        private InlineHintTagCache _cache_doNotAccessOutsideOfGate = InlineHintTagCache.Empty;

        public InlineHintsTagger(
            InlineHintsTaggerProvider taggerProvider,
            IWpfTextView textView,
            EfficientTagger<InlineHintDataTag> tagger)
        {
            _taggerProvider = taggerProvider;

            _textView = textView;

            _underlyingTagger = tagger;
            _underlyingTagger.TagsChanged += OnUnderlyingTagger_TagsChanged;

            _formatMap = taggerProvider.ClassificationFormatMapService.GetClassificationFormatMap(textView);
            _hintClassification = taggerProvider.ClassificationTypeRegistryService.GetClassificationType(InlineHintsTag.TagId);
            _formatMap.ClassificationFormatMappingChanged += this.OnClassificationFormatMappingChanged;
        }

        public override void Dispose()
        {
            _underlyingTagger.TagsChanged -= OnUnderlyingTagger_TagsChanged;
            _underlyingTagger.Dispose();
            _formatMap.ClassificationFormatMappingChanged -= OnClassificationFormatMappingChanged;
        }

        private void OnUnderlyingTagger_TagsChanged(object sender, SnapshotSpanEventArgs e)
        {
            InvalidateCache();
            OnTagsChanged(this, e);
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
                    OnTagsChanged(this, new SnapshotSpanEventArgs(tag.Span));
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
            lock (_gate)
            {
                _cacheSnapshot_doNotAccessOutsideOfGate = null;
                _cache_doNotAccessOutsideOfGate = InlineHintTagCache.Empty;
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

                ITextSnapshot? cacheSnapshot;
                InlineHintTagCache cache;

                lock (_gate)
                {
                    cacheSnapshot = _cacheSnapshot_doNotAccessOutsideOfGate;
                    cache = _cache_doNotAccessOutsideOfGate;
                }

                var cacheBuilder = cache.ToBuilder();

                // If the snapshot has changed, we can't use any of the cached data, as it is associated with the
                // original snapshot they were created against.
                var snapshot = spans[0].Snapshot;
                if (snapshot != cacheSnapshot)
                    cacheBuilder.Clear();

                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                var classify = document != null && _taggerProvider.EditorOptionsService.GlobalOptions.GetOption(InlineHintsViewOptionsStorage.ColorHints, document.Project.Language);

                using var _1 = SegmentedListPool.GetPooledList<TagSpan<InlineHintDataTag>>(out var dataTagSpans);
                _underlyingTagger.AddTags(spans, dataTagSpans);

                // Presize so we can add the elements below without continually resizing.
                adornmentTagSpans.Capacity += dataTagSpans.Count;

                using var _2 = PooledHashSet<int>.GetInstance(out var seenPositions);

                foreach (var dataTagSpan in dataTagSpans)
                {
                    // Check if we already have a tag at this position.  If not, initialize the cache to just point at
                    // the new data tag.
                    var position = dataTagSpan.Span.Start;
                    if (!cache.TryGetValue(position, out var inlineHintTags))
                    {
                        inlineHintTags = new(dataTagSpan);
                        cacheBuilder[position] = inlineHintTags;
                    }

                    if (seenPositions.Add(position))
                    {
                        // Now check if this is the first time we've been asked to compute the adornment for this particular
                        // data tag.  If so, create and cache it so we don't recreate the adornments in the future for the
                        // same text snapshot.
                        //
                        // Note: creating the adornment doesn't change the cache itself.  It just updates one of the values
                        // the cache is already pointing to.  We only need to change the cache if we've added a new
                        // key/value mapping to it.
                        inlineHintTags.AdornmentTagSpan ??= CreateAdornmentTagSpan(inlineHintTags.DataTagSpan, classify);
                        adornmentTagSpans.Add(inlineHintTags.AdornmentTagSpan);
                    }
                }

                cache = cacheBuilder.ToImmutable();
                lock (_gate)
                {
                    _cacheSnapshot_doNotAccessOutsideOfGate = snapshot;
                    _cache_doNotAccessOutsideOfGate = cache;
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.General))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private TagSpan<IntraTextAdornmentTag> CreateAdornmentTagSpan(
            TagSpan<InlineHintDataTag> dataTagSpan, bool classify)
        {
            var adornmentSpan = dataTagSpan.Span;

            var hintUITag = InlineHintsTag.Create(
                dataTagSpan.Tag.Hint, Format, _textView, adornmentSpan, _taggerProvider, _formatMap, classify);

            return new TagSpan<IntraTextAdornmentTag>(adornmentSpan, hintUITag);
        }
    }
}
