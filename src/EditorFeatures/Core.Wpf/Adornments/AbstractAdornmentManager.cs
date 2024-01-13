// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Adornments
{
    /// <summary>
    /// UI manager for graphic overlay tags. These tags will simply paint something related to the text.
    /// </summary>
    internal abstract class AbstractAdornmentManager<T> where T : BrushTag
    {
        private readonly object _invalidatedSpansLock = new();

        private readonly IThreadingContext _threadingContext;

        /// <summary>Notification system about operations we do</summary>
        private readonly IAsynchronousOperationListener _asyncListener;

        /// <summary>Spans that are invalidated, and need to be removed from the layer..</summary>
        private List<IMappingSpan> _invalidatedSpans;

        /// <summary>View that created us.</summary>
        protected readonly IWpfTextView TextView;

        /// <summary>Layer where we draw adornments.</summary>
        protected readonly IAdornmentLayer AdornmentLayer;

        /// <summary>Aggregator that tells us where to draw.</summary>
        protected readonly ITagAggregator<T> TagAggregator;

        /// <summary>
        /// MUST BE CALLED ON UI THREAD!!!!   This method touches WPF.
        /// 
        /// This is where we apply visuals to the text. 
        /// 
        /// It happens when another region of the view becomes visible or there is a change in tags.
        /// For us the end result is the same - get tags from tagger and update visuals correspondingly.
        /// </summary>        
        protected abstract void AddAdornmentsToAdornmentLayer_CallOnlyOnUIThread(NormalizedSnapshotSpanCollection changedSpanCollection);

        protected abstract void RemoveAdornmentFromAdornmentLayer_CallOnlyOnUIThread(SnapshotSpan span);

        internal AbstractAdornmentManager(
            IThreadingContext threadingContext,
            IWpfTextView textView,
            IViewTagAggregatorFactoryService tagAggregatorFactoryService,
            IAsynchronousOperationListener asyncListener,
            string adornmentLayerName)
        {
            Contract.ThrowIfNull(threadingContext);
            Contract.ThrowIfNull(textView);
            Contract.ThrowIfNull(tagAggregatorFactoryService);
            Contract.ThrowIfNull(adornmentLayerName);
            Contract.ThrowIfNull(asyncListener);

            _threadingContext = threadingContext;
            TextView = textView;
            AdornmentLayer = textView.GetAdornmentLayer(adornmentLayerName);
            textView.LayoutChanged += OnLayoutChanged;
            _asyncListener = asyncListener;

            // If we are not on the UI thread, we are at race with Close, but we should be on UI thread
            Contract.ThrowIfFalse(textView.VisualElement.Dispatcher.CheckAccess());
            textView.Closed += OnTextViewClosed;

            TagAggregator = tagAggregatorFactoryService.CreateTagAggregator<T>(textView);

            TagAggregator.TagsChanged += OnTagsChanged;
        }

        private void OnTextViewClosed(object sender, System.EventArgs e)
        {
            // release the aggregator
            TagAggregator.TagsChanged -= OnTagsChanged;
            TagAggregator.Dispose();

            // unhook from view
            TextView.Closed -= OnTextViewClosed;
            TextView.LayoutChanged -= OnLayoutChanged;

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
                Contract.ThrowIfFalse(TextView.VisualElement.Dispatcher.CheckAccess());

                var reformattedSpans = e.NewOrReformattedSpans;
                var viewSnapshot = TextView.TextSnapshot;

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
                        _invalidatedSpans = new List<IMappingSpan> { changedSpan };

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
                    _threadingContext.JoinableTaskFactory.WithPriority(TextView.VisualElement.Dispatcher, DispatcherPriority.Render).RunAsync(async () =>
                    {
                        using (_asyncListener.BeginAsyncOperation(GetType() + ".OnTagsChanged.2"))
                        {
                            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true);
                            UpdateInvalidSpans();
                        }
                    });
                }
            }
        }

        /// <summary>
        /// MUST BE CALLED ON UI THREAD!!!!   This method touches WPF.
        ///  
        /// This function is used to update invalidates spans.
        /// </summary>
        protected void UpdateInvalidSpans()
        {
            using (_asyncListener.BeginAsyncOperation(GetType().Name + ".UpdateInvalidSpans.1"))
            using (Logger.LogBlock(FunctionId.Tagger_AdornmentManager_UpdateInvalidSpans, CancellationToken.None))
            {
                // this method should only run on UI thread as we do WPF here.
                Contract.ThrowIfFalse(TextView.VisualElement.Dispatcher.CheckAccess());

                List<IMappingSpan> invalidated;
                lock (_invalidatedSpansLock)
                {
                    invalidated = _invalidatedSpans;
                    _invalidatedSpans = null;
                }

                if (TextView.IsClosed)
                {
                    return; // already closed
                }

                if (invalidated != null)
                {
                    var viewSnapshot = TextView.TextSnapshot;
                    var invalidatedNormalized = TranslateAndNormalize(invalidated, viewSnapshot);
                    UpdateSpans_CallOnlyOnUIThread(invalidatedNormalized, removeOldTags: true);
                }
            }
        }

        protected void UpdateSpans_CallOnlyOnUIThread(NormalizedSnapshotSpanCollection changedSpanCollection, bool removeOldTags)
        {
            Contract.ThrowIfNull(changedSpanCollection);

            // this method should only run on UI thread as we do WPF here.
            Contract.ThrowIfFalse(TextView.VisualElement.Dispatcher.CheckAccess());

            var viewLines = TextView.TextViewLines;
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
                        RemoveAdornmentFromAdornmentLayer_CallOnlyOnUIThread(changedSpan);
                    }
                }
            }

            AddAdornmentsToAdornmentLayer_CallOnlyOnUIThread(changedSpanCollection);
        }

        protected bool TryGetMappedPoint(
            SnapshotSpan snapshotSpan,
            IMappingTagSpan<T> mappingTagSpan,
            out SnapshotPoint mappedPoint)
        {
            var mappedPointOpt = GetMappedPoint(snapshotSpan, mappingTagSpan);
            mappedPoint = mappedPointOpt is null ? default : mappedPointOpt.Value;
            return mappedPointOpt != null;
        }

        protected bool TryGetViewLine(SnapshotPoint mappedPoint, [NotNullWhen(true)] out IWpfTextViewLine viewLine)
        {
            viewLine = TextView.TextViewLines.GetTextViewLineContainingBufferPosition(mappedPoint);
            return viewLine != null;
        }

        protected bool ShouldDrawTag(IMappingTagSpan<T> mappingTagSpan)
        {
            if (!TryMapToSingleSnapshotSpan(mappingTagSpan.Span, TextView.TextSnapshot, out var span))
                return false;

            if (!TextView.TextViewLines.IntersectsBufferSpan(span))
                return false;

            return true;
        }

        protected SnapshotPoint? GetMappedPoint(SnapshotSpan snapshotSpan, IMappingTagSpan<T> mappingTagSpan)
        {
            var point = mappingTagSpan.Span.End.GetPoint(snapshotSpan.Snapshot, PositionAffinity.Predecessor);
            if (point == null)
            {
                return null;
            }

            var mappedPoint = TextView.BufferGraph.MapUpToSnapshot(
                point.Value, PointTrackingMode.Negative, PositionAffinity.Predecessor, TextView.TextSnapshot);
            if (mappedPoint == null)
            {
                return null;
            }

            return mappedPoint.Value;
        }

        // Map the mapping span to the visual snapshot. note that as a result of projection
        // topology, originally single span may be mapped into several spans. Visual adornments do
        // not make much sense on disjoint spans. We will not decorate spans that could not make it
        // in one piece.
        protected static bool TryMapToSingleSnapshotSpan(IMappingSpan mappingSpan, ITextSnapshot viewSnapshot, out SnapshotSpan span)
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
                span = default;
                return false; // span is unmapped or disjoint.
            }

            span = spans[0];
            return true;
        }
    }
}
