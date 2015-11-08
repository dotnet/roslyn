// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LineSeparators
{
    /// <summary>
    /// UI manager for graphic overlay tags. These tags will simply paint something related to the text.
    /// </summary>
    internal class AdornmentManager<T> where T : GraphicsTag
    {
        private readonly object _invalidatedSpansLock = new object();

        /// <summary>View that created us.</summary>
        private readonly IWpfTextView _textView;

        /// <summary>Layer where we draw adornments.</summary>
        private readonly IAdornmentLayer _adornmentLayer;

        /// <summary>Aggregator that tells us where to draw.</summary>
        private readonly ITagAggregator<T> _tagAggregator;

        /// <summary>Notification system about operations we do</summary>
        private readonly IAsynchronousOperationListener _asyncListener;

        /// <summary>Spans that are invalidated, and need to be removed from the layer..</summary>
        private List<IMappingSpan> _invalidatedSpans;

        public static AdornmentManager<T> Create(
            IWpfTextView textView,
            IViewTagAggregatorFactoryService aggregatorService,
            IAsynchronousOperationListener asyncListener,
            string adornmentLayerName)
        {
            Contract.ThrowIfNull(textView);
            Contract.ThrowIfNull(aggregatorService);
            Contract.ThrowIfNull(adornmentLayerName);
            Contract.ThrowIfNull(asyncListener);

            return new AdornmentManager<T>(textView, aggregatorService, asyncListener, adornmentLayerName);
        }

        internal AdornmentManager(
            IWpfTextView textView,
            IViewTagAggregatorFactoryService tagAggregatorFactoryService,
            IAsynchronousOperationListener asyncListener,
            string adornmentLayerName)
        {
            Contract.ThrowIfNull(textView);
            Contract.ThrowIfNull(tagAggregatorFactoryService);
            Contract.ThrowIfNull(adornmentLayerName);
            Contract.ThrowIfNull(asyncListener);

            _textView = textView;
            _adornmentLayer = textView.GetAdornmentLayer(adornmentLayerName);
            textView.LayoutChanged += OnLayoutChanged;
            _asyncListener = asyncListener;

            // If we are not on the UI thread, we are at race with Close, but we should be on UI thread
            Contract.ThrowIfFalse(textView.VisualElement.Dispatcher.CheckAccess());
            textView.Closed += OnTextViewClosed;

            _tagAggregator = tagAggregatorFactoryService.CreateTagAggregator<T>(textView);

            _tagAggregator.TagsChanged += OnTagsChanged;
        }

        private void OnTextViewClosed(object sender, System.EventArgs e)
        {
            // release the aggregator
            _tagAggregator.TagsChanged -= OnTagsChanged;
            _tagAggregator.Dispose();

            // unhook from view
            _textView.Closed -= OnTextViewClosed;
            _textView.LayoutChanged -= OnLayoutChanged;

            // At this point, this object should be available for garbage collection.
        }

        /// <summary>
        /// This handler gets called whenever there is a visual change in the view.
        /// Example: edit or a scroll.
        /// </summary>
        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            using (Logger.LogBlock(FunctionId.Tagger_AdornmentManager_OnLayoutChanged, CancellationToken.None))
            using (_asyncListener.BeginAsyncOperation(GetType() + ".OnLayoutChanged"))
            {
                // Make sure we're on the UI thread.
                Contract.ThrowIfFalse(_textView.VisualElement.Dispatcher.CheckAccess());

                var reformattedSpans = e.NewOrReformattedSpans;
                var viewSnapshot = _textView.TextSnapshot;

                // No need to remove tags as these spans are reformatted anyways.
                UpdateSpans_CallOnlyOnUIThread(reformattedSpans, removeOldTags: false);

                // Compute any spans that had been invalidated but were not affected by layout.
                List<IMappingSpan> invalidated;
                lock (_invalidatedSpansLock)
                {
                    invalidated = _invalidatedSpans;
                    _invalidatedSpans = null;
                }

                if (invalidated != null)
                {
                    var invalidatedAndNormalized = TranslateAndNormalize(invalidated, viewSnapshot);
                    var invalidatedButNotReformatted = NormalizedSnapshotSpanCollection.Difference(
                        invalidatedAndNormalized,
                        e.NewOrReformattedSpans);

                    UpdateSpans_CallOnlyOnUIThread(invalidatedButNotReformatted, removeOldTags: true);
                }
            }
        }

        private static NormalizedSnapshotSpanCollection TranslateAndNormalize(
            IEnumerable<IMappingSpan> spans,
            ITextSnapshot targetSnapshot)
        {
            Contract.ThrowIfNull(spans);

            var translated = spans.SelectMany(span => span.GetSpans(targetSnapshot));
            return new NormalizedSnapshotSpanCollection(translated);
        }

        /// <summary>
        /// This handler is called when tag aggregator notifies us about tag changes.
        /// </summary>
        private void OnTagsChanged(object sender, TagsChangedEventArgs e)
        {
            using (_asyncListener.BeginAsyncOperation(GetType().Name + ".OnTagsChanged.1"))
            {
                var changedSpan = e.Span;

                if (changedSpan == null)
                {
                    return; // nothing changed
                }

                var needToScheduleUpdate = false;
                lock (_invalidatedSpansLock)
                {
                    if (_invalidatedSpans == null)
                    {
                        // set invalidated spans
                        var newInvalidatedSpans = new List<IMappingSpan>();
                        newInvalidatedSpans.Add(changedSpan);
                        _invalidatedSpans = newInvalidatedSpans;

                        needToScheduleUpdate = true;
                    }
                    else
                    {
                        // add to existing invalidated spans
                        _invalidatedSpans.Add(changedSpan);
                    }
                }

                if (needToScheduleUpdate)
                {
                    // schedule an update
                    var asyncToken = _asyncListener.BeginAsyncOperation(GetType() + ".OnTagsChanged.2");
                    _textView.VisualElement.Dispatcher.BeginInvoke(
                        new System.Action(() =>
                    {
                        try
                        {
                            UpdateInvalidSpans();
                        }
                        finally
                        {
                            asyncToken.Dispose();
                        }
                    }), DispatcherPriority.Render);
                }
            }
        }

        /// <summary>
        /// MUST BE CALLED ON UI THREAD!!!!   This method touches WPF.
        ///  
        /// This function is used to update invalidates spans.
        /// </summary>
        private void UpdateInvalidSpans()
        {
            using (_asyncListener.BeginAsyncOperation(GetType().Name + ".UpdateInvalidSpans.1"))
            using (Logger.LogBlock(FunctionId.Tagger_AdornmentManager_UpdateInvalidSpans, CancellationToken.None))
            {
                // this method should only run on UI thread as we do WPF here.
                Contract.ThrowIfFalse(_textView.VisualElement.Dispatcher.CheckAccess());

                List<IMappingSpan> invalidated;
                lock (_invalidatedSpansLock)
                {
                    invalidated = _invalidatedSpans;
                    _invalidatedSpans = null;
                }

                if (_textView.IsClosed)
                {
                    return; // already closed
                }

                if (invalidated != null)
                {
                    var viewSnapshot = _textView.TextSnapshot;
                    var invalidatedNormalized = TranslateAndNormalize(invalidated, viewSnapshot);
                    UpdateSpans_CallOnlyOnUIThread(invalidatedNormalized, removeOldTags: true);
                }
            }
        }

        /// <summary>
        /// MUST BE CALLED ON UI THREAD!!!!   This method touches WPF.
        /// 
        /// This is where we apply visuals to the text. 
        /// 
        /// It happens when another region of the view becomes visible or there is a change in tags.
        /// For us the end result is the same - get tags from tagger and update visuals correspondingly.
        /// </summary>        
        private void UpdateSpans_CallOnlyOnUIThread(NormalizedSnapshotSpanCollection changedSpanCollection, bool removeOldTags)
        {
            Contract.ThrowIfNull(changedSpanCollection);

            // this method should only run on UI thread as we do WPF here.
            Contract.ThrowIfFalse(_textView.VisualElement.Dispatcher.CheckAccess());

            var viewSnapshot = _textView.TextSnapshot;
            var visualSnapshot = _textView.VisualSnapshot;

            var viewLines = _textView.TextViewLines;
            if (viewLines == null || viewLines.Count == 0)
            {
                return; // nothing to draw on
            }

            // removing is a separate pass from adding so that new stuff is not removed.
            if (removeOldTags)
            {
                foreach (var changedSpan in changedSpanCollection)
                {
                    // is there any effect on the view?
                    if (viewLines.IntersectsBufferSpan(changedSpan))
                    {
                        _adornmentLayer.RemoveAdornmentsByVisualSpan(changedSpan);
                    }
                }
            }

            foreach (var changedSpan in changedSpanCollection)
            {
                // is there any effect on the view?
                if (!viewLines.IntersectsBufferSpan(changedSpan))
                {
                    continue;
                }

                var tagSpans = _tagAggregator.GetTags(changedSpan);
                foreach (var tagMappingSpan in tagSpans)
                {
                    // We don't want to draw line separators if they would intersect a collapsed outlining
                    // region.  So we test if we can map the start of the line separator up to our visual 
                    // snapshot. If we can't, then we just skip it.
                    var point = tagMappingSpan.Span.Start.GetPoint(changedSpan.Snapshot, PositionAffinity.Predecessor);
                    if (point == null)
                    {
                        continue;
                    }

                    var mappedPoint = _textView.BufferGraph.MapUpToSnapshot(
                        point.Value, PointTrackingMode.Negative, PositionAffinity.Predecessor, _textView.VisualSnapshot);
                    if (mappedPoint == null)
                    {
                        continue;
                    }

                    SnapshotSpan span;
                    if (!TryMapToSingleSnapshotSpan(tagMappingSpan.Span, viewSnapshot, out span))
                    {
                        continue;
                    }

                    if (!viewLines.IntersectsBufferSpan(span))
                    {
                        // span is outside of the view so we will not get geometry for it, but may 
                        // spent a lot of time trying.
                        continue;
                    }

                    // add the visual to the adornment layer.
                    var geometry = viewLines.GetMarkerGeometry(span);
                    if (geometry != null)
                    {
                        var tag = tagMappingSpan.Tag;
                        var graphicsResult = tag.GetGraphics(_textView, geometry);
                        _adornmentLayer.AddAdornment(
                            behavior: AdornmentPositioningBehavior.TextRelative,
                            visualSpan: span,
                            tag: tag,
                            adornment: graphicsResult.VisualElement,
                            removedCallback: delegate { graphicsResult.Dispose(); });
                    }
                }
            }
        }

        // Map the mapping span to the visual snapshot. note that as a result of projection
        // topology, originally single span may be mapped into several spans. Visual adornments do
        // not make much sense on disjoint spans. We will not decorate spans that could not make it
        // in one piece.
        private bool TryMapToSingleSnapshotSpan(IMappingSpan mappingSpan, ITextSnapshot viewSnapshot, out SnapshotSpan span)
        {
            // IMappingSpan.GetSpans is a surprisingly expensive function that allocates multiple
            // lists and collection if the view buffer is same as anchor we could just map the
            // anchor to the viewSnapshot however, since the _anchor is not available, we have to
            // map start and end TODO: verify that affinity is correct. If it does not matter we
            // should use the cheapest.
            if (viewSnapshot != null && mappingSpan.AnchorBuffer == viewSnapshot.TextBuffer)
            {
                var mappedStart = mappingSpan.Start.GetPoint(viewSnapshot, PositionAffinity.Predecessor).Value;
                var mappedEnd = mappingSpan.End.GetPoint(viewSnapshot, PositionAffinity.Successor).Value;
                span = new SnapshotSpan(mappedStart, mappedEnd);
                return true;
            }

            // TODO: actually adornments do not make much sense on "cropped" spans either - Consider line separator on "nd Su"
            // is it possible to cheaply detect cropping?  
            var spans = mappingSpan.GetSpans(viewSnapshot);
            if (spans.Count != 1)
            {
                span = default(SnapshotSpan);
                return false; // span is unmapped or disjoint.
            }

            span = spans[0];
            return true;
        }
    }
}
