﻿// Licensed to the .NET Foundation under one or more agreements.
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
internal sealed partial class TagSpanIntervalTree<TTag>(
    ITextBuffer textBuffer,
    SpanTrackingMode trackingMode,
    IEnumerable<ITagSpan<TTag>>? values1 = null,
    IEnumerable<ITagSpan<TTag>>? values2 = null) where TTag : ITag
{
    private readonly ITextBuffer _textBuffer = textBuffer;
    private readonly SpanTrackingMode _spanTrackingMode = trackingMode;
    private readonly IntervalTree<ITagSpan<TTag>> _tree = IntervalTree.Create(
        new IntervalIntrospector(textBuffer.CurrentSnapshot, trackingMode),
        values1, values2);

    private static SnapshotSpan GetTranslatedSpan(
        ITagSpan<TTag> originalTagSpan, ITextSnapshot textSnapshot, SpanTrackingMode trackingMode)
    {
        var localSpan = originalTagSpan.Span;

        return localSpan.Snapshot == textSnapshot
            ? localSpan
            : localSpan.TranslateTo(textSnapshot, trackingMode);
    }

    private static TagSpan<TTag> GetTranslatedTagSpan(ITagSpan<TTag> originalTagSpan, ITextSnapshot textSnapshot, SpanTrackingMode trackingMode)
        => new(GetTranslatedSpan(originalTagSpan, textSnapshot, trackingMode), originalTagSpan.Tag);

    public ITextBuffer Buffer => _textBuffer;

    public SpanTrackingMode SpanTrackingMode => _spanTrackingMode;

    public bool HasSpanThatContains(SnapshotPoint point)
    {
        var snapshot = point.Snapshot;
        Debug.Assert(snapshot.TextBuffer == _textBuffer);

        return _tree.HasIntervalThatContains(point.Position, length: 0, new IntervalIntrospector(snapshot, _spanTrackingMode));
    }

    public IList<TagSpan<TTag>> GetIntersectingSpans(SnapshotSpan snapshotSpan)
        => SegmentedListPool<TagSpan<TTag>>.ComputeList(
            static (args, tags) => args.@this.AppendIntersectingSpansInSortedOrder(args.snapshotSpan, tags),
            (@this: this, snapshotSpan));

    /// <summary>
    /// Gets all the spans that intersect with <paramref name="snapshotSpan"/> in sorted order and adds them to
    /// <paramref name="result"/>.  Note the sorted chunk of items are appended to <paramref name="result"/>.  This
    /// means that <paramref name="result"/> may not be sorted if there were already items in them.
    /// </summary>
    private void AppendIntersectingSpansInSortedOrder(SnapshotSpan snapshotSpan, SegmentedList<TagSpan<TTag>> result)
    {
        var snapshot = snapshotSpan.Snapshot;
        Debug.Assert(snapshot.TextBuffer == _textBuffer);

        using var intersectingIntervals = TemporaryArray<ITagSpan<TTag>>.Empty;
        _tree.FillWithIntervalsThatIntersectWith(
            snapshotSpan.Start, snapshotSpan.Length,
            ref intersectingIntervals.AsRef(),
            new IntervalIntrospector(snapshot, _spanTrackingMode));

        foreach (var tagSpan in intersectingIntervals)
            result.Add(GetTranslatedTagSpan(tagSpan, snapshot, _spanTrackingMode));
    }

    public IEnumerable<ITagSpan<TTag>> GetSpans(ITextSnapshot snapshot)
        => _tree.Select(tn => GetTranslatedTagSpan(tn, snapshot, _spanTrackingMode));

    public void AddAllSpans(ITextSnapshot textSnapshot, HashSet<ITagSpan<TTag>> tagSpans)
    {
        foreach (var tagSpan in _tree)
        {
            // Avoid reallocating in the case where we're on the same snapshot.
            tagSpans.Add(tagSpan.Span.Snapshot == textSnapshot
                ? tagSpan
                : GetTranslatedTagSpan(tagSpan, textSnapshot, _spanTrackingMode));
        }
    }

    public void RemoveIntersectingTagSpans(SnapshotSpan snapshotSpan, HashSet<ITagSpan<TTag>> tagSpans)
    {
        using var buffer = TemporaryArray<ITagSpan<TTag>>.Empty;

        var textSnapshot = snapshotSpan.Snapshot;
        _tree.FillWithIntervalsThatIntersectWith(
            snapshotSpan.Span.Start,
            snapshotSpan.Span.Length,
            ref buffer.AsRef(),
            new IntervalIntrospector(textSnapshot, _spanTrackingMode));

        foreach (var tagSpan in buffer)
        {
            // Avoid reallocating in the case where we're on the same snapshot.
            tagSpans.Remove(tagSpan.Span.Snapshot == textSnapshot
                ? tagSpan
                : GetTranslatedTagSpan(tagSpan, textSnapshot, _spanTrackingMode));
        }
    }

    public bool IsEmpty()
        => _tree.IsEmpty();

    public void AddIntersectingTagSpans(NormalizedSnapshotSpanCollection requestedSpans, SegmentedList<TagSpan<TTag>> tags)
    {
        AddIntersectingTagSpansWorker(requestedSpans, tags);
        DebugVerifyTags(requestedSpans, tags);
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

    private void AddIntersectingTagSpansWorker(
        NormalizedSnapshotSpanCollection requestedSpans,
        SegmentedList<TagSpan<TTag>> tags)
    {
        const int MaxNumberOfRequestedSpans = 100;

        // Special case the case where there is only one requested span.  In that case, we don't
        // need to allocate any intermediate collections
        if (requestedSpans.Count == 1)
            AppendIntersectingSpansInSortedOrder(requestedSpans[0], tags);
        else if (requestedSpans.Count < MaxNumberOfRequestedSpans)
            AddTagsForSmallNumberOfSpans(requestedSpans, tags);
        else
            AddTagsForLargeNumberOfSpans(requestedSpans, tags);
    }

    private void AddTagsForSmallNumberOfSpans(
        NormalizedSnapshotSpanCollection requestedSpans,
        SegmentedList<TagSpan<TTag>> tags)
    {
        foreach (var span in requestedSpans)
            AppendIntersectingSpansInSortedOrder(span, tags);
    }

    private void AddTagsForLargeNumberOfSpans(NormalizedSnapshotSpanCollection requestedSpans, SegmentedList<TagSpan<TTag>> tags)
    {
        // we are asked with bunch of spans. rather than asking same question again and again, ask once with big span
        // which will return superset of what we want. and then filter them out in O(m+n) cost. 
        // m == number of requested spans, n = number of returned spans
        var mergedSpan = new SnapshotSpan(requestedSpans[0].Start, requestedSpans[^1].End);

        using var _1 = SegmentedListPool.GetPooledList<TagSpan<TTag>>(out var tempList);

        AppendIntersectingSpansInSortedOrder(mergedSpan, tempList);
        if (tempList.Count == 0)
            return;

        // Note: both 'requstedSpans' and 'tempList' are in sorted order.

        using var enumerator = tempList.GetEnumerator();

        if (!enumerator.MoveNext())
            return;

        using var _2 = PooledHashSet<ITagSpan<TTag>>.GetInstance(out var hashSet);

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
