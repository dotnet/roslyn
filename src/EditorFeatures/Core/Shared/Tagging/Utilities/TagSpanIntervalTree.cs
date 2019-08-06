// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    /// <summary>
    /// A tag span interval tree represents an ordered tree data structure to store tag spans in.  It
    /// allows you to efficiently find all tag spans that intersect a provided span.  Tag spans are
    /// tracked. That way you can query for intersecting/overlapping spans in a different snapshot
    /// than the one for the tag spans that were added.
    /// </summary>
    internal partial class TagSpanIntervalTree<TTag> where TTag : ITag
    {
        private readonly IntervalTree<TagNode> _tree;
        private readonly ITextBuffer _textBuffer;
        private readonly SpanTrackingMode _spanTrackingMode;

        public TagSpanIntervalTree(ITextBuffer textBuffer,
            SpanTrackingMode trackingMode,
            IEnumerable<ITagSpan<TTag>> values = null)
        {
            _textBuffer = textBuffer;
            _spanTrackingMode = trackingMode;

            var nodeValues = values?.Select(ts => new TagNode(ts, trackingMode));

            var introspector = new IntervalIntrospector(textBuffer.CurrentSnapshot);
            _tree = IntervalTree.Create(introspector, nodeValues);
        }

        public ITextBuffer Buffer => _textBuffer;

        public SpanTrackingMode SpanTrackingMode => _spanTrackingMode;

        public IList<ITagSpan<TTag>> GetIntersectingSpans(SnapshotSpan snapshotSpan)
        {
            var snapshot = snapshotSpan.Snapshot;
            Debug.Assert(snapshot.TextBuffer == _textBuffer);

            var introspector = new IntervalIntrospector(snapshot);
            var intersectingIntervals = _tree.GetIntervalsThatIntersectWith(snapshotSpan.Start, snapshotSpan.Length, introspector);

            List<ITagSpan<TTag>> result = null;
            foreach (var tagNode in intersectingIntervals)
            {
                result ??= new List<ITagSpan<TTag>>();
                result.Add(new TagSpan<TTag>(tagNode.Span.GetSpan(snapshot), tagNode.Tag));
            }

            return result ?? SpecializedCollections.EmptyList<ITagSpan<TTag>>();
        }

        public IEnumerable<ITagSpan<TTag>> GetSpans(ITextSnapshot snapshot)
        {
            return _tree.Select(tn => new TagSpan<TTag>(tn.Span.GetSpan(snapshot), tn.Tag));
        }

        public bool IsEmpty()
        {
            return _tree.IsEmpty();
        }

        public IEnumerable<ITagSpan<TTag>> GetIntersectingTagSpans(NormalizedSnapshotSpanCollection requestedSpans)
        {
            var result = GetIntersectingTagSpansWorker(requestedSpans);
            DebugVerifyTags(requestedSpans, result);
            return result;
        }

        [Conditional("DEBUG")]
        private static void DebugVerifyTags(NormalizedSnapshotSpanCollection requestedSpans, IEnumerable<ITagSpan<TTag>> tags)
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

        private IEnumerable<ITagSpan<TTag>> GetIntersectingTagSpansWorker(NormalizedSnapshotSpanCollection requestedSpans)
        {
            const int MaxNumberOfRequestedSpans = 100;

            // Special case the case where there is only one requested span.  In that case, we don't
            // need to allocate any intermediate collections
            return requestedSpans.Count == 1
                ? GetIntersectingSpans(requestedSpans[0])
                : requestedSpans.Count < MaxNumberOfRequestedSpans
                    ? GetTagsForSmallNumberOfSpans(requestedSpans)
                    : GetTagsForLargeNumberOfSpans(requestedSpans);
        }

        private IEnumerable<ITagSpan<TTag>> GetTagsForSmallNumberOfSpans(NormalizedSnapshotSpanCollection requestedSpans)
        {
            var result = new List<ITagSpan<TTag>>();

            foreach (var s in requestedSpans)
            {
                result.AddRange(GetIntersectingSpans(s));
            }

            return result;
        }

        private IEnumerable<ITagSpan<TTag>> GetTagsForLargeNumberOfSpans(NormalizedSnapshotSpanCollection requestedSpans)
        {
            // we are asked with bunch of spans. rather than asking same question again and again, ask once with big span
            // which will return superset of what we want. and then filter them out in O(m+n) cost. 
            // m == number of requested spans, n = number of returned spans
            var mergedSpan = new SnapshotSpan(requestedSpans[0].Start, requestedSpans[requestedSpans.Count - 1].End);
            var result = GetIntersectingSpans(mergedSpan);

            var requestIndex = 0;

            var enumerator = result.GetEnumerator();

            try
            {
                if (!enumerator.MoveNext())
                {
                    return SpecializedCollections.EmptyEnumerable<ITagSpan<TTag>>();
                }

                var hashSet = new HashSet<ITagSpan<TTag>>();
                while (true)
                {
                    var currentTag = enumerator.Current;

                    var currentRequestSpan = requestedSpans[requestIndex];
                    var currentTagSpan = currentTag.Span;

                    if (currentRequestSpan.Start > currentTagSpan.End)
                    {
                        if (!enumerator.MoveNext())
                        {
                            break;
                        }
                    }
                    else if (currentTagSpan.Start > currentRequestSpan.End)
                    {
                        requestIndex++;

                        if (requestIndex >= requestedSpans.Count)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (currentTagSpan.Length > 0)
                        {
                            hashSet.Add(currentTag);
                        }

                        if (!enumerator.MoveNext())
                        {
                            break;
                        }
                    }
                }

                return hashSet;
            }
            finally
            {
                enumerator.Dispose();
            }
        }
    }
}
