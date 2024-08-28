// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging;

/// <summary>
/// A tag span interval tree represents an ordered tree data structure to store tag spans in.  It
/// allows you to efficiently find all tag spans that intersect a provided span.  Tag spans are
/// tracked. That way you can query for intersecting/overlapping spans in a different snapshot
/// than the one for the tag spans that were added.
/// </summary>
internal sealed partial class TagSpanIntervalTree<TTag>(SpanTrackingMode spanTrackingMode) where TTag : ITag
{
    // Tracking mode passed in here doesn't matter (since the tree is empty).
    public static readonly TagSpanIntervalTree<TTag> Empty = new(SpanTrackingMode.EdgeInclusive);

    private readonly SpanTrackingMode _spanTrackingMode = spanTrackingMode;
    private readonly ImmutableIntervalTree<TagSpan<TTag>> _tree = ImmutableIntervalTree<TagSpan<TTag>>.Empty;

    public TagSpanIntervalTree(
        ITextSnapshot textSnapshot,
        SpanTrackingMode trackingMode,
        SegmentedList<TagSpan<TTag>> values)
        : this(trackingMode)
    {
        // Sort the values by their start position.  This is extremely fast (defer'ing to the runtime's sorting
        // routines), and allows us to build the balanced tree directly without having to do any additional work.
        values.Sort(static (t1, t2) => t1.Span.Start.Position - t2.Span.Start.Position);

        _tree = ImmutableIntervalTree<TagSpan<TTag>>.CreateFromSorted(
            new IntervalIntrospector(textSnapshot, trackingMode), values);
    }

    private static SnapshotSpan GetTranslatedSpan(TagSpan<TTag> originalTagSpan, ITextSnapshot textSnapshot, SpanTrackingMode trackingMode)
        // SnapshotSpan no-ops if you pass it the same snapshot that it is holding onto.
        => originalTagSpan.Span.TranslateTo(textSnapshot, trackingMode);

    private TagSpan<TTag> GetTranslatedTagSpan(TagSpan<TTag> originalTagSpan, ITextSnapshot textSnapshot)
        => GetTranslatedTagSpan(originalTagSpan, textSnapshot, _spanTrackingMode);

    private static TagSpan<TTag> GetTranslatedTagSpan(TagSpan<TTag> originalTagSpan, ITextSnapshot textSnapshot, SpanTrackingMode trackingMode)
        // Avoid reallocating in the case where we're on the same snapshot.
        => originalTagSpan.Span.Snapshot == textSnapshot
            ? originalTagSpan
            : new(GetTranslatedSpan(originalTagSpan, textSnapshot, trackingMode), originalTagSpan.Tag);

    public bool HasSpanThatContains(SnapshotPoint point)
        => _tree.Algorithms.HasIntervalThatContains(point.Position, length: 0, new IntervalIntrospector(point.Snapshot, _spanTrackingMode));

    public bool HasSpanThatIntersects(SnapshotPoint point)
        => _tree.Algorithms.HasIntervalThatIntersectsWith(point.Position, new IntervalIntrospector(point.Snapshot, _spanTrackingMode));

    /// <summary>
    /// Gets all the spans that intersect with <paramref name="snapshotSpan"/> in sorted order and adds them to
    /// <paramref name="result"/>.  Note the sorted chunk of items are appended to <paramref name="result"/>.  This
    /// means that <paramref name="result"/> may not be sorted if there were already items in them.
    /// </summary>
    public void AddIntersectingTagSpans(SnapshotSpan snapshotSpan, SegmentedList<TagSpan<TTag>> result)
    {
        var snapshot = snapshotSpan.Snapshot;

        using var intersectingIntervals = TemporaryArray<TagSpan<TTag>>.Empty;
        _tree.Algorithms.FillWithIntervalsThatIntersectWith(
            snapshotSpan.Start, snapshotSpan.Length,
            ref intersectingIntervals.AsRef(),
            new IntervalIntrospector(snapshot, _spanTrackingMode));

        foreach (var tagSpan in intersectingIntervals)
            result.Add(GetTranslatedTagSpan(tagSpan, snapshot, _spanTrackingMode));
    }

    /// <summary>
    /// Gets all the tag spans in this tree, remapped to <paramref name="snapshot"/>, and returns them as a <see
    /// cref="NormalizedSnapshotSpanCollection"/>.
    /// </summary>
    public NormalizedSnapshotSpanCollection GetSnapshotSpanCollection(ITextSnapshot snapshot)
    {
        if (this == Empty)
            return NormalizedSnapshotSpanCollection.Empty;

        using var _ = ArrayBuilder<SnapshotSpan>.GetInstance(out var spans);

        foreach (var tagSpan in _tree)
            spans.Add(GetTranslatedSpan(tagSpan, snapshot, _spanTrackingMode));

        return spans.Count == 0
            ? NormalizedSnapshotSpanCollection.Empty
            : new(spans);
    }

    /// <summary>
    /// Adds all the tag spans in <see langword="this"/> to <paramref name="tagSpans"/>, translating them to the given
    /// location <paramref name="textSnapshot"/> based on <see cref="_spanTrackingMode"/>.
    /// </summary>
    public void AddAllSpans(ITextSnapshot textSnapshot, HashSet<TagSpan<TTag>> tagSpans)
    {
        foreach (var tagSpan in _tree)
            tagSpans.Add(GetTranslatedTagSpan(tagSpan, textSnapshot));
    }

    /// <inheritdoc cref="AddAllSpans(ITextSnapshot, HashSet{TagSpan{TTag}})"/>
    /// <remarks>Spans will be added in sorted order</remarks>
    public void AddAllSpans(ITextSnapshot textSnapshot, SegmentedList<TagSpan<TTag>> tagSpans)
    {
        foreach (var tagSpan in _tree)
            tagSpans.Add(GetTranslatedTagSpan(tagSpan, textSnapshot));
    }

    /// <summary>
    /// Removes from <paramref name="tagSpans"/> all the tags spans in <see langword="this"/> that intersect with any of
    /// the spans in <paramref name="snapshotSpansToRemove"/>.
    /// </summary>
    public void RemoveIntersectingTagSpans(
        ArrayBuilder<SnapshotSpan> snapshotSpansToRemove, HashSet<TagSpan<TTag>> tagSpans)
    {
        using var buffer = TemporaryArray<TagSpan<TTag>>.Empty;

        foreach (var snapshotSpan in snapshotSpansToRemove)
        {
            buffer.Clear();

            var textSnapshot = snapshotSpan.Snapshot;
            _tree.Algorithms.FillWithIntervalsThatIntersectWith(
                snapshotSpan.Span.Start,
                snapshotSpan.Span.Length,
                ref buffer.AsRef(),
                new IntervalIntrospector(textSnapshot, _spanTrackingMode));

            foreach (var tagSpan in buffer)
                tagSpans.Remove(GetTranslatedTagSpan(tagSpan, textSnapshot));
        }
    }

    public void AddIntersectingTagSpans(NormalizedSnapshotSpanCollection requestedSpans, SegmentedList<TagSpan<TTag>> tags)
    {
        const int MaxNumberOfRequestedSpans = 100;

        // Special case the case where there is only one requested span.  In that case, we don't
        // need to allocate any intermediate collections
        if (requestedSpans.Count == 1)
        {
            AddIntersectingTagSpans(requestedSpans[0], tags);
        }
        else if (requestedSpans.Count < MaxNumberOfRequestedSpans)
        {
            foreach (var span in requestedSpans)
                AddIntersectingTagSpans(span, tags);
        }
        else
        {
            AddTagsForLargeNumberOfSpans(requestedSpans, tags);
        }

        DebugVerifyTags(requestedSpans, tags);
        return;

        void AddTagsForLargeNumberOfSpans(NormalizedSnapshotSpanCollection requestedSpans, SegmentedList<TagSpan<TTag>> tags)
        {
            // we are asked with bunch of spans. rather than asking same question again and again, ask once with big span
            // which will return superset of what we want. and then filter them out in O(m+n) cost. 
            // m == number of requested spans, n = number of returned spans
            var mergedSpan = new SnapshotSpan(requestedSpans[0].Start, requestedSpans[^1].End);

            using var _1 = SegmentedListPool.GetPooledList<TagSpan<TTag>>(out var tempList);

            AddIntersectingTagSpans(mergedSpan, tempList);
            if (tempList.Count == 0)
                return;

            // Note: both 'requestedSpans' and 'tempList' are in sorted order.

            using var enumerator = tempList.GetEnumerator();

            if (!enumerator.MoveNext())
                return;

            using var _2 = PooledHashSet<TagSpan<TTag>>.GetInstance(out var hashSet);

            var requestIndex = 0;
            while (true)
            {
                var currentTag = enumerator.Current;

                var currentRequestSpan = requestedSpans[requestIndex];
                var currentTagSpan = currentTag.Span;

                // The current tag is *before* the current span we're trying to intersect with.  Move to the next tag to
                // see if it intersects with the current span.
                if (currentTagSpan.End < currentRequestSpan.Start)
                {
                    // If there are no more tags, then we're done.
                    if (!enumerator.MoveNext())
                        return;

                    continue;
                }

                // The current tag is *after* teh current span we're trying to intersect with.  Move to the next span to
                // see if it intersects with the current tag.
                if (currentTagSpan.Start > currentRequestSpan.End)
                {
                    requestIndex++;

                    // If there are no more spans to intersect with, then we're done.
                    if (requestIndex >= requestedSpans.Count)
                        return;

                    continue;
                }

                // This tag intersects the current span we're trying to intersect with.  Ensure we only see and add a
                // particular tag once. 

                if (currentTagSpan.Length > 0 &&
                    hashSet.Add(currentTag))
                {
                    tags.Add(currentTag);
                }

                if (!enumerator.MoveNext())
                    break;
            }
        }
    }

    [Conditional("DEBUG")]
    private static void DebugVerifyTags(NormalizedSnapshotSpanCollection requestedSpans, SegmentedList<TagSpan<TTag>> tags)
    {
        if (tags == null)
        {
            return;
        }

        foreach (var tag in tags)
        {
            var span = tag.Span;

            if (!requestedSpans.Any(s => s.IntersectsWith(span)))
            {
                Contract.Fail(tag + " doesn't intersects with any requested span");
            }
        }
    }
}
