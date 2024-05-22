// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Tagging;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

/// <summary>
/// Base type for async taggers that perform their work based on what code is current visible in the user's
/// viewport.  These taggers will compute tags for what is visible at their specified <see
/// cref="AbstractAsynchronousTaggerProvider{TTag}.EventChangeDelay"/>, but will also compute tags for the regions
/// above and below that visible section at a delay of <see cref="DelayTimeSpan.NonFocus"/>.
/// </summary>
internal abstract partial class AsynchronousViewportTaggerProvider<TTag> : IViewTaggerProvider
    where TTag : ITag
{
    private enum ViewPortToTag
    {
        InView,
        Above,
        Below,
    }

    /// <summary>
    /// An amount of lines above/below the viewport that we will always tag, just to ensure that scrolling up/down a few
    /// lines shows immediate/accurate tags.
    /// </summary>
    private const int s_standardLineCountAroundViewportToTag = 10;

    private readonly int _extraLinesAroundViewportToTag;
    private readonly ImmutableArray<SingleViewportTaggerProvider> _viewportTaggerProviders;

    protected readonly IThreadingContext ThreadingContext;
    protected readonly IGlobalOptionService GlobalOptions;
    protected readonly IAsynchronousOperationListener AsyncListener;

    protected AsynchronousViewportTaggerProvider(
        IThreadingContext threadingContext,
        IGlobalOptionService globalOptions,
        ITextBufferVisibilityTracker? visibilityTracker,
        IAsynchronousOperationListener asyncListener,
        int extraLinesAroundViewportToTag = 100)
    {
        ThreadingContext = threadingContext;
        GlobalOptions = globalOptions;
        AsyncListener = asyncListener;
        _extraLinesAroundViewportToTag = extraLinesAroundViewportToTag;

        using var providers = TemporaryArray<SingleViewportTaggerProvider>.Empty;

        // Always tag what's in the current viewport.
        providers.Add(CreateSingleViewportTaggerProvider(ViewPortToTag.InView));

        // Also tag what's outside the viewport if requested and it's beyond what would be in the normal InView tagger.
        if (extraLinesAroundViewportToTag > s_standardLineCountAroundViewportToTag)
        {
            providers.Add(CreateSingleViewportTaggerProvider(ViewPortToTag.Above));
            providers.Add(CreateSingleViewportTaggerProvider(ViewPortToTag.Below));
        }

        _viewportTaggerProviders = providers.ToImmutableAndClear();

        return;

        SingleViewportTaggerProvider CreateSingleViewportTaggerProvider(ViewPortToTag viewPortToTag)
            => new(this, viewPortToTag, threadingContext, globalOptions, visibilityTracker, asyncListener);
    }

    // Functionality for subclasses to control how this diagnostic tagging operates.  All the individual
    // SingleViewportTaggerProvider will defer to these to do the work so that they otherwise operate
    // identically.

    /// <inheritdoc cref="AbstractAsynchronousTaggerProvider{TTag}.Options"/>
    protected virtual ImmutableArray<IOption2> Options => [];

    /// <inheritdoc cref="AbstractAsynchronousTaggerProvider{TTag}.TextChangeBehavior"/>
    protected virtual TaggerTextChangeBehavior TextChangeBehavior => TaggerTextChangeBehavior.None;

    /// <inheritdoc cref="AbstractAsynchronousTaggerProvider{TTag}.CreateEventSource(ITextView?, ITextBuffer)"/>
    protected abstract ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer);

    /// <inheritdoc cref="AbstractAsynchronousTaggerProvider{TTag}.EventChangeDelay"/>
    protected abstract TaggerDelay EventChangeDelay { get; }

    /// <inheritdoc cref="AbstractAsynchronousTaggerProvider{TTag}.ProduceTagsAsync(TaggerContext{TTag}, CancellationToken)"/>
    protected abstract Task ProduceTagsAsync(TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag, CancellationToken cancellationToken);

    /// <inheritdoc cref="AbstractAsynchronousTaggerProvider{TTag}.TagEquals(TTag, TTag)"/>
    protected abstract bool TagEquals(TTag tag1, TTag tag2);

    /// <inheritdoc cref="AbstractAsynchronousTaggerProvider{TTag}.SpanTrackingMode"/>
    protected virtual SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;

    /// <summary>
    /// Indicates whether a tagger should be created for this text view and buffer.
    /// </summary>
    /// <param name="textView">The text view for which a tagger is attempting to be created</param>
    /// <param name="buffer">The text buffer for which a tagger is attempting to be created</param>
    /// <returns>Whether a tagger should be created</returns>
    protected virtual bool CanCreateTagger(ITextView textView, ITextBuffer buffer) => true;

    ITagger<T>? IViewTaggerProvider.CreateTagger<T>(ITextView textView, ITextBuffer buffer)
    {
        if (!CanCreateTagger(textView, buffer))
            return null;

        var tagger = CreateTagger(textView, buffer);
        if (tagger is not ITagger<T> genericTagger)
        {
            tagger.Dispose();
            return null;
        }

        return genericTagger;
    }

    public EfficientTagger<TTag> CreateTagger(ITextView textView, ITextBuffer buffer)
    {
        using var taggers = TemporaryArray<EfficientTagger<TTag>>.Empty;
        foreach (var taggerProvider in _viewportTaggerProviders)
        {
            var innerTagger = taggerProvider.CreateTagger(textView, buffer);
            if (innerTagger != null)
                taggers.Add(innerTagger);
        }

        return new SimpleAggregateTagger<TTag>(taggers.ToImmutableAndClear());
    }

    public bool SpanEquals(SnapshotSpan? span1, SnapshotSpan? span2)
        => TaggerUtilities.SpanEquals(span1, span2, this.SpanTrackingMode);
}
