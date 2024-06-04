// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

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

        protected override void AddSpansToTag(ITextView? textView, ITextBuffer subjectBuffer, ref TemporaryArray<SnapshotSpan> result)
        {
            this.ThreadingContext.ThrowIfNotOnUIThread();
            Contract.ThrowIfNull(textView);

            // if we're the current view, attempt to just get what's visible, plus 10 lines above and below.  This will
            // ensure that moving up/down a few lines tends to have immediate accurate results.
            var visibleSpanOpt = textView.GetVisibleLinesSpan(subjectBuffer, extraLines: s_standardLineCountAroundViewportToTag);
            if (visibleSpanOpt is null)
            {
                // couldn't figure out the visible span.  So the InView tagger will need to tag everything, and the
                // above/below tagger should tag nothing.
                if (_viewPortToTag == ViewPortToTag.InView)
                    base.AddSpansToTag(textView, subjectBuffer, ref result);

                return;
            }

            var visibleSpan = visibleSpanOpt.Value;

            // If we're the 'InView' tagger, tag what was visible. 
            if (_viewPortToTag is ViewPortToTag.InView)
            {
                result.Add(visibleSpan);
                return;
            }

            // For the above/below tagger, broaden the span to to the requested portion above/below what's visible, then
            // subtract out the visible range.
            var widenedSpanOpt = textView.GetVisibleLinesSpan(subjectBuffer, extraLines: _callback._extraLinesAroundViewportToTag);
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
