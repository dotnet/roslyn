// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging;

internal class TaggerContext<TTag> where TTag : ITag
{
    private readonly ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> _existingTags;

    internal ImmutableArray<SnapshotSpan> _spansTagged;
    public readonly SegmentedList<ITagSpan<TTag>> TagSpans = [];

    /// <summary>
    /// If the client should compute tags using frozen partial semantics.  This generally should have no effect if tags
    /// are computed within this process as the provided <see cref="SpansToTag"/> will be given the right frozen or
    /// unfrozen documents.  However, this is relevant when making calls to our external OOP server to ensure that it
    /// also does the same when processing the request on its side.
    /// </summary>
    public bool FrozenPartialSemantics { get; }

    public ImmutableArray<DocumentSnapshotSpan> SpansToTag { get; }
    public SnapshotPoint? CaretPosition { get; }

    /// <summary>
    /// The text that has changed between the last successful tagging and this new request to
    /// produce tags.  In order to be passed this value, <see cref="TaggerTextChangeBehavior.TrackTextChanges"/> 
    /// must be specified in <see cref="AbstractAsynchronousTaggerProvider{TTag}.TextChangeBehavior"/>.
    /// </summary>
    public TextChangeRange? TextChangeRange { get; }

    /// <summary>
    /// The state of the tagger.  Taggers can use this to keep track of information across calls
    /// to <see cref="AbstractAsynchronousTaggerProvider{TTag}.ProduceTagsAsync(TaggerContext{TTag}, CancellationToken)"/>.  Note: state will
    /// only be preserved if the tagger infrastructure fully updates itself with the tags that 
    /// were produced.  i.e. if that tagging pass is canceled, then the state set here will not
    /// be preserved and the previous preserved state will be used the next time ProduceTagsAsync
    /// is called.
    /// </summary>
    public object State { get; set; }

    // For testing only.
    internal TaggerContext(
        Document document,
        ITextSnapshot snapshot,
        bool frozenPartialSemantics,
        SnapshotPoint? caretPosition = null,
        TextChangeRange? textChangeRange = null)
        : this(
              state: null,
              frozenPartialSemantics,
              [new DocumentSnapshotSpan(document, snapshot.GetFullSpan())],
              caretPosition,
              textChangeRange,
              existingTags: null)
    {
    }

    internal TaggerContext(
        object state,
        bool frozenPartialSemantics,
        ImmutableArray<DocumentSnapshotSpan> spansToTag,
        SnapshotPoint? caretPosition,
        TextChangeRange? textChangeRange,
        ImmutableDictionary<ITextBuffer, TagSpanIntervalTree<TTag>> existingTags)
    {
        this.State = state;
        this.FrozenPartialSemantics = frozenPartialSemantics;
        this.SpansToTag = spansToTag;
        this.CaretPosition = caretPosition;
        this.TextChangeRange = textChangeRange;

        _spansTagged = spansToTag.SelectAsArray(ds => ds.SnapshotSpan);
        _existingTags = existingTags;
    }

    public void AddTag(ITagSpan<TTag> tag)
        => TagSpans.Add(tag);

    public void ClearTags()
        => TagSpans.Clear();

    /// <summary>
    /// Used to allow taggers to indicate what spans were actually tagged.  This is useful when the tagger decides
    /// to tag a different span than the entire file.  If a sub-span of a document is tagged then the tagger
    /// infrastructure will keep previously computed tags from before and after the sub-span and merge them with the
    /// newly produced tags.
    /// </summary>
    public void SetSpansTagged(ImmutableArray<SnapshotSpan> spansTagged)
        => _spansTagged = spansTagged;

    public bool HasExistingContainingTags(SnapshotPoint point)
        => _existingTags != null &&
           _existingTags.TryGetValue(point.Snapshot.TextBuffer, out var tree) &&
           tree.HasSpanThatContains(point);
}
