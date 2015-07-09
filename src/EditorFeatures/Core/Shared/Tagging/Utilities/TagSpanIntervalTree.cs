// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
    internal partial class TagSpanIntervalTree<TTag> : ITagSpanIntervalTree<TTag>
        where TTag : ITag
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

            var nodeValues = values == null
                ? null
                : values.Select(ts => new TagNode(ts, trackingMode));

            var introspector = new IntervalIntrospector(textBuffer.CurrentSnapshot);
            _tree = IntervalTree.Create(introspector, nodeValues);
        }

        public ITextBuffer Buffer
        {
            get
            {
                return _textBuffer;
            }
        }

        public SpanTrackingMode SpanTrackingMode
        {
            get
            {
                return _spanTrackingMode;
            }
        }

        public IList<ITagSpan<TTag>> GetIntersectingSpans(SnapshotSpan snapshotSpan)
        {
            var snapshot = snapshotSpan.Snapshot;
            Contract.Requires(snapshot.TextBuffer == _textBuffer);

            var introspector = new IntervalIntrospector(snapshot);
            var intersectingIntervals = _tree.GetIntersectingIntervals(snapshotSpan.Start, snapshotSpan.Length, introspector);

            List<ITagSpan<TTag>> result = null;
            foreach (var tagNode in intersectingIntervals)
            {
                result = result ?? new List<ITagSpan<TTag>>();
                result.Add(new TagSpan<TTag>(tagNode.Span.GetSpan(snapshot), tagNode.Tag));
            }

            return result ?? SpecializedCollections.EmptyList<ITagSpan<TTag>>();
        }

        public void GetNonIntersectingSpans(SnapshotSpan snapshotSpan, out IEnumerable<ITagSpan<TTag>> beforeSpans, out IEnumerable<ITagSpan<TTag>> afterSpans)
        {
            var snapshot = snapshotSpan.Snapshot;
            Contract.Requires(snapshot.TextBuffer == _textBuffer);

            var introspector = new IntervalIntrospector(snapshot);

            var beforeSpan = new SnapshotSpan(snapshot, 0, snapshotSpan.Start);
            beforeSpans = _tree.GetIntersectingIntervals(beforeSpan.Start, beforeSpan.Length, introspector)
                               .Where(n => beforeSpan.Contains(n.Span.GetSpan(snapshot)))
                               .Select(tn => new TagSpan<TTag>(tn.Span.GetSpan(snapshot), tn.Tag));

            var afterSpan = new SnapshotSpan(snapshot, snapshotSpan.End, snapshot.Length - snapshotSpan.End);
            afterSpans = _tree.GetIntersectingIntervals(afterSpan.Start, afterSpan.Length, introspector)
                              .Where(n => afterSpan.Contains(n.Span.GetSpan(snapshot)))
                              .Select(tn => new TagSpan<TTag>(tn.Span.GetSpan(snapshot), tn.Tag));
        }

        public IEnumerable<ITagSpan<TTag>> GetSpans(ITextSnapshot snapshot)
        {
            return _tree.Select(tn => new TagSpan<TTag>(tn.Span.GetSpan(snapshot), tn.Tag));
        }

        public bool IsEmpty()
        {
            return _tree.IsEmpty();
        }
    }
}
