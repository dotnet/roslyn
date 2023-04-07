// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Threading;
using EnvDTE80;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

/// <summary>
/// Base type for async taggers that perform their work based on what code is current visible in the user's
/// viewport.  These taggers will compute tags for what is visible at their specified <see
/// cref="AbstractAsynchronousTaggerProvider{TTag}.EventChangeDelay"/>, but will also compute tags for the regions
/// above and below that visible section at a delay of <see cref="DelayTimeSpan.NonFocus"/>.
/// </summary>
internal abstract class AsynchronousViewportTaggerProvider<TTag> : IViewTaggerProvider
    where TTag : ITag
{
    private enum ViewPortToTag
    {
        AboveAndBelow,
        InView,
    }

    private sealed class SingleViewportTaggerProvider : AsynchronousViewTaggerProvider<TTag>
    {
        private readonly AsynchronousViewportTaggerProvider<TTag> _callback;

        private readonly ViewPortToTag _viewPortToTag;

        public SingleViewportTaggerProvider(
            AsynchronousViewportTaggerProvider<TTag> callback,
            ViewPortToTag viewPortToTag,
            IThreadingContext threadingContext,
            IGlobalOptionService globalOptions,
            ITextBufferVisibilityTracker? visibilityTracker,
            IAsynchronousOperationListener asyncListener)
            : base(threadingContext, globalOptions, visibilityTracker, asyncListener)
        {
            _callback = callback;
            _viewPortToTag = viewPortToTag;
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
            => _callback.CreateEventSource(textView, subjectBuffer);

        protected override TaggerDelay EventChangeDelay
            => _viewPortToTag == ViewPortToTag.InView ? _callback.EventChangeDelay : TaggerDelay.NonFocus;

        protected override IEnumerable<SnapshotSpan> GetSpansToTag(ITextView? textView, ITextBuffer subjectBuffer)
        {
            this.ThreadingContext.ThrowIfNotOnUIThread();
            Contract.ThrowIfNull(textView);

            // if we're the current view, attempt to just get what's visible, plus 10 lines above and below.  This will
            // ensure that moving up/down a few lines tends to have immediate accurate results.
            var visibleSpanOpt = textView.GetVisibleLinesSpan(subjectBuffer, extraLines: 10);
            if (visibleSpanOpt is null)
            {
                // couldn't figure out the visible span.  So the InView tagger will need to tag everything, and the
                // above/below tagger should tag nothing.
                return _viewPortToTag == ViewPortToTag.InView
                    ? base.GetSpansToTag(textView, subjectBuffer)
                    : SpecializedCollections.EmptyEnumerable<SnapshotSpan>();
            }

            var visibleSpan = visibleSpanOpt.Value;

            // If we're the 'InView' tagger, tag what was visible. 
            if (_viewPortToTag is ViewPortToTag.InView)
                return SpecializedCollections.SingletonEnumerable(visibleSpan);

            // For the above/below tagger, broaden the span to to the requested portion above/below what's visible, then
            // subtract out the visible range.
            var widenedSpanOpt = textView.GetVisibleLinesSpan(subjectBuffer, extraLines: _callback._extraLinesAroundViewportToTag);
            Contract.ThrowIfNull(widenedSpanOpt, "Should not ever fail getting the widened span as we were able to get the normal span");

            var widenedSpan = widenedSpanOpt.Value;
            Contract.ThrowIfFalse(widenedSpan.Span.Contains(visibleSpan.Span), "The widened span must be at least as large as the visible one.");

            using var result = TemporaryArray<SnapshotSpan>.Empty;

            var aboveSpan = new SnapshotSpan(visibleSpan.Snapshot, Span.FromBounds(widenedSpan.Span.Start, visibleSpan.Span.Start));
            var belowSpan = new SnapshotSpan(visibleSpan.Snapshot, Span.FromBounds(visibleSpan.Span.End, widenedSpan.Span.End));

            if (!aboveSpan.IsEmpty)
                result.Add(aboveSpan);

            if (!belowSpan.IsEmpty)
                result.Add(belowSpan);

            return result.ToImmutableAndClear();
        }
    }

    /// <summary>
    /// An amount of lines above/below the viewport that we will always tag, just to ensure that scrolling up/down a few
    /// lines shows immediate/accurate tags.
    /// </summary>
    private const int s_standardLineCountAroundViewportToTag = 10;

    private readonly int _extraLinesAroundViewportToTag;
    private readonly ImmutableArray<SingleViewportTaggerProvider> _viewportTaggerProviders;

    protected readonly IThreadingContext ThreadingContext;

    protected AsynchronousViewportTaggerProvider(
        IThreadingContext threadingContext,
        IGlobalOptionService globalOptions,
        ITextBufferVisibilityTracker? visibilityTracker,
        IAsynchronousOperationListener asyncListener,
        int extraLinesAroundViewportToTag = 100)
    {
        ThreadingContext = threadingContext;
        _extraLinesAroundViewportToTag = extraLinesAroundViewportToTag;

        using var providers = TemporaryArray<SingleViewportTaggerProvider>.Empty;

        // Always tag what's in the current viewport.
        providers.Add(CreateSingleViewportTaggerProvider(ViewPortToTag.InView));

        // Also tag what's outside the viewport if requested and it's beyond what would be in the normal InView tagger.
        if (extraLinesAroundViewportToTag > s_standardLineCountAroundViewportToTag)
            providers.Add(CreateSingleViewportTaggerProvider(ViewPortToTag.AboveAndBelow));

        _viewportTaggerProviders = providers.ToImmutableAndClear();

        return;

        SingleViewportTaggerProvider CreateSingleViewportTaggerProvider(ViewPortToTag viewPortToTag)
            => new(this, viewPortToTag, threadingContext, globalOptions, visibilityTracker, asyncListener);
    }

    // Functionality for subclasses to control how this diagnostic tagging operates.  All the individual
    // SingleDiagnosticKindTaggerProvider will defer to these to do the work so that they otherwise operate
    // identically.

    //protected abstract ImmutableArray<IOption2> Options { get; }
    //protected virtual ImmutableArray<IOption2> FeatureOptions { get; } = ImmutableArray<IOption2>.Empty;

    //protected abstract bool TagEquals(TTag tag1, TTag tag2);
    // protected abstract ITagSpan<TTag>? CreateTagSpan(Workspace workspace, bool isLiveUpdate, SnapshotSpan span, DiagnosticData data);

    /// <inheritdoc cref="AbstractAsynchronousTaggerProvider{TTag}.CreateEventSource(ITextView?, ITextBuffer)"/>
    protected abstract ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer);

    /// <inheritdoc cref="AbstractAsynchronousTaggerProvider{TTag}.EventChangeDelay"/>
    protected abstract TaggerDelay EventChangeDelay { get; }

    /// <inheritdoc cref="AbstractAsynchronousTaggerProvider{TTag}.ProduceTagsAsync(TaggerContext{TTag}, CancellationToken)"/>
    protected abstract Task ProduceTagsAsync(TaggerContext<TTag> context, CancellationToken cancellationToken);

    /// <inheritdoc cref="AbstractAsynchronousTaggerProvider{TTag}.TagEquals(TTag, TTag)"/>
    protected abstract bool TagEquals(TTag tag1, TTag tag2);

    /// <inheritdoc cref="AbstractAsynchronousTaggerProvider{TTag}.SpanTrackingMode"/>
    protected virtual SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;

    public ITagger<T>? CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
    {
        using var taggers = TemporaryArray<ITagger<TTag>>.Empty;
        foreach (var taggerProvider in _viewportTaggerProviders)
        {
            var innerTagger = taggerProvider.CreateTagger<TTag>(textView, buffer);
            if (innerTagger != null)
                taggers.Add(innerTagger);
        }

        var tagger = new AggregateTagger<TTag>(taggers.ToImmutableAndClear());
        if (tagger is not ITagger<T> genericTagger)
        {
            tagger.Dispose();
            return null;
        }

        return genericTagger;
    }

    protected bool SpanEquals(SnapshotSpan? span1, SnapshotSpan? span2)
        => TaggerUtilities.SpanEquals(span1, span2, this.SpanTrackingMode);
}
