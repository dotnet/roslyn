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
            List<ITagSpan<TTag>> values = null)
        {
            _textBuffer = textBuffer;
            _spanTrackingMode = trackingMode;

            _tree = IntervalTree.Create(
                new IntervalIntrospector(textBuffer.CurrentSnapshot),
                CreateTagNodes(trackingMode, values));
        }

        private static TagNode[] CreateTagNodes(SpanTrackingMode trackingMode, List<ITagSpan<TTag>> values)
        {
            if (values == null)
            {
                return null;
            }

            var length = values.Count;
            var tagNodes = new TagNode[length];
            for (var i = 0; i < length; i++)
            {
                tagNodes[i] = new TagNode(values[i], trackingMode);
            }

            return tagNodes;
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
            var intersectingIntervals = _tree.GetIntersectingInOrderIntervals(snapshotSpan.Start, snapshotSpan.Length, introspector);
            if (intersectingIntervals.Count == 0)
            {
                return SpecializedCollections.EmptyList<ITagSpan<TTag>>();
            }

            var result = new List<ITagSpan<TTag>>();
            foreach (var tagNode in intersectingIntervals)
            {
                result.Add(new TagSpan<TTag>(tagNode.Span.GetSpan(snapshot), tagNode.Tag));
            }

            return result;
        }

        public List<ITagSpan<TTag>> GetNonIntersectingSpans(SnapshotSpan snapshotSpan)
        {
            var snapshot = snapshotSpan.Snapshot;
            Contract.Requires(snapshot.TextBuffer == _textBuffer);

            var introspector = new IntervalIntrospector(snapshot);

            var beforeSpan = new SnapshotSpan(snapshot, 0, snapshotSpan.Start);
            var before = _tree.GetIntersectingIntervals(beforeSpan.Start, beforeSpan.Length, introspector)
                             .Where(n => beforeSpan.Contains(n.Span.GetSpan(snapshot)));

            var afterSpan = new SnapshotSpan(snapshot, snapshotSpan.End, snapshot.Length - snapshotSpan.End);
            var after = _tree.GetIntersectingIntervals(afterSpan.Start, afterSpan.Length, introspector)
                             .Where(n => afterSpan.Contains(n.Span.GetSpan(snapshot)));

            var result = new List<ITagSpan<TTag>>();
            foreach (var tagNode in before.Concat(after))
            {
                result.Add(new TagSpan<TTag>(tagNode.Span.GetSpan(snapshot), tagNode.Tag));
            }

            return result;
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
