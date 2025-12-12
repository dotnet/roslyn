// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

internal abstract partial class AsynchronousViewportTaggerProvider<TTag> where TTag : ITag
{
    /// <summary>
    /// Actual <see cref="AsynchronousViewTaggerProvider{TTag}"/> responsible for tagging a particular span (or spans)
    /// of the view.  Inherits all behavior of a normal view tagger, except for determining what spans to tag and what
    /// cadence to tag them at.
    /// </summary>
    private sealed class SingleViewportTaggerProvider(
        AsynchronousViewportTaggerProvider<TTag> callback,
        ViewPortToTag viewPortToTag,
        string featureName)
        : AsynchronousViewTaggerProvider<TTag>(callback._taggerHost, featureName)
    {
        private readonly AsynchronousViewportTaggerProvider<TTag> _callback = callback;

        private readonly ViewPortToTag _viewPortToTag = viewPortToTag;

        protected override ImmutableArray<IOption2> Options
            => _callback.Options;

        protected override TaggerTextChangeBehavior TextChangeBehavior
            => _callback.TextChangeBehavior;

        protected override SpanTrackingMode SpanTrackingMode
            => _callback.SpanTrackingMode;

        protected override bool SupportsFrozenPartialSemantics
            => _callback.SupportsFrozenPartialSemantics;

        protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
            => _callback.CreateEventSource(textView, subjectBuffer);

        protected override TaggerDelay EventChangeDelay
            // If we're in view, tag at the cadence the feature wants for visible code.  Otherwise, if we're out of
            // view, tag at the slowest cadence to reduce the amount of resources used for things that may never even be
            // looked at.
            => _viewPortToTag == ViewPortToTag.InView ? _callback.EventChangeDelay : TaggerDelay.NonFocus;

        protected override bool CancelOnNewWork
            // For what's in view, we don't want to cancel work when changes come in.  That way we still finish
            // computing whatever was in progress, which we can map forward to the latest snapshot.  This helps ensure
            // that colors come in quickly, even as the user is typing fast.  For what's above/below, we don't want to
            // do the same.  We can just cancel that work entirely, pushing things out until the next lull in typing. 
            // This can save a lot of CPU time for things that may never even be looked at.
            => _viewPortToTag != ViewPortToTag.InView;

        /// <summary>
        /// Returns the span of the lines in subjectBuffer that is currently visible in the provided
        /// view.  "extraLines" can be provided to get a span that encompasses some number of lines
        /// before and after the actual visible lines.
        /// </summary>
        private static SnapshotSpan? GetVisibleLinesSpan(ITextView textView, ITextBuffer subjectBuffer, int extraLines)
        {
            // Determine the range of text that is visible in the view.  Then map this down to the buffer passed in.  From
            // that, determine the start/end line for the buffer that is in view.
            var visibleSpan = textView.TextViewLines.FormattedSpan;
            var visibleSpansInBuffer = textView.BufferGraph.MapDownToBuffer(visibleSpan, SpanTrackingMode.EdgeInclusive, subjectBuffer);
            if (visibleSpansInBuffer is not ([var firstVisibleSpan, ..] and [.., var lastVisibleSpan]))
                return null;

            var visibleStart = firstVisibleSpan.Start;
            var visibleEnd = lastVisibleSpan.End;
            var snapshot = subjectBuffer.CurrentSnapshot;
            var startLine = visibleStart.GetContainingLineNumber();
            var endLine = visibleEnd.GetContainingLineNumber();

            startLine = Math.Max(startLine - extraLines, 0);
            endLine = Math.Min(endLine + extraLines, snapshot.LineCount - 1);

            var start = snapshot.GetLineFromLineNumber(startLine).Start;
            var end = snapshot.GetLineFromLineNumber(endLine).EndIncludingLineBreak;

            var span = new SnapshotSpan(snapshot, Span.FromBounds(start, end));

            return span;
        }

        protected override bool TryAddSpansToTag(ITextView? textView, ITextBuffer subjectBuffer, ref TemporaryArray<SnapshotSpan> result)
        {
            this.ThreadingContext.ThrowIfNotOnUIThread();
            Contract.ThrowIfNull(textView);

            // View is closed.  Return no spans so we can remove all tags.
            if (textView.IsClosed)
                return true;

            // If we're in a layout, then we can't even determine what our visible span is. Bail out immediately as qe
            // don't want to suddenly flip to tagging everything, then go back to tagging a small subset of the view
            // afterwards.
            //
            // In this case we literally do not know what is visible, so we want to bail and try again later.
            if (textView.InLayout)
                return false;

            // During text view initialization the TextViewLines may be null.  In that case nothing is really visible.
            // So return no spans so we can remove all tags.
            if (textView.TextViewLines == null)
                return true;

            // if we're the current view, attempt to just get what's visible, plus 10 lines above and below.  This will
            // ensure that moving up/down a few lines tends to have immediate accurate results.
            var visibleSpanOpt = GetVisibleLinesSpan(textView, subjectBuffer, extraLines: s_standardLineCountAroundViewportToTag);

            // Nothing was visible at all.  Return no spans so we can remove all tags.
            if (visibleSpanOpt is null)
                return true;

            var visibleSpan = visibleSpanOpt.Value;

            // If we're the 'InView' tagger, tag what was visible. 
            if (_viewPortToTag is ViewPortToTag.InView)
            {
                result.Add(visibleSpan);
            }
            else
            {
                // For the above/below tagger, broaden the span to to the requested portion above/below what's visible, then
                // subtract out the visible range.
                var widenedSpanOpt = GetVisibleLinesSpan(textView, subjectBuffer, extraLines: _callback._extraLinesAroundViewportToTag);
                Contract.ThrowIfNull(widenedSpanOpt, "Should not ever fail getting the widened span as we were able to get the normal visible span");

                var widenedSpan = widenedSpanOpt.Value;
                Contract.ThrowIfFalse(widenedSpan.Span.Contains(visibleSpan.Span), "The widened span must be at least as large as the visible one.");

                if (_viewPortToTag is ViewPortToTag.Above)
                {
                    var aboveSpan = new SnapshotSpan(visibleSpan.Snapshot, Span.FromBounds(widenedSpan.Span.Start, visibleSpan.Span.Start));
                    if (!aboveSpan.IsEmpty)
                        result.Add(aboveSpan);
                }
                else if (_viewPortToTag is ViewPortToTag.Below)
                {
                    var belowSpan = new SnapshotSpan(visibleSpan.Snapshot, Span.FromBounds(visibleSpan.Span.End, widenedSpan.Span.End));
                    if (!belowSpan.IsEmpty)
                        result.Add(belowSpan);
                }
            }

            // Unilaterally return true here, even if we determine we don't have a span to tag.  In this case, we've
            // computed that there really is nothing visible, in which case we *do* want to move to having no tags.
            return true;
        }

        protected override async Task ProduceTagsAsync(
            TaggerContext<TTag> context, CancellationToken cancellationToken)
        {
            foreach (var spanToTag in context.SpansToTag)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _callback.ProduceTagsAsync(
                    context, spanToTag, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
