// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Windows.Shapes;
using Microsoft.CodeAnalysis.Editor.Implementation.Adornments;
using Microsoft.CodeAnalysis.Editor.Implementation.StringIndentation;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.UI.Adornments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.StringIndentation
{
    internal class StringIndentationAdornmentManager : AbstractAdornmentManager<StringIndentationTag>
    {
        public StringIndentationAdornmentManager(
            IThreadingContext threadingContext,
            IWpfTextView textView,
            IViewTagAggregatorFactoryService tagAggregatorFactoryService,
            IAsynchronousOperationListener asyncListener,
            string adornmentLayerName)
            : base(threadingContext, textView, tagAggregatorFactoryService, asyncListener, adornmentLayerName)
        {
        }

        protected override void AddAdornmentsToAdornmentLayer_CallOnlyOnUIThread(NormalizedSnapshotSpanCollection changedSpanCollection)
        {
            // this method should only run on UI thread as we do WPF here.
            Contract.ThrowIfFalse(TextView.VisualElement.Dispatcher.CheckAccess());

            var viewSnapshot = TextView.TextSnapshot;
            var viewLines = TextView.TextViewLines;

            foreach (var changedSpan in changedSpanCollection)
            {
                // is there any effect on the view?
                if (!viewLines.IntersectsBufferSpan(changedSpan))
                {
                    continue;
                }

                var tagSpans = TagAggregator.GetTags(changedSpan);
                foreach (var tagMappingSpan in tagSpans)
                {
                    if (!ShouldDrawTag(changedSpan, tagMappingSpan, out _))
                        continue;

                    if (!TryMapToSingleSnapshotSpan(tagMappingSpan.Span, TextView.TextSnapshot, out var span))
                        continue;

                    var block = VisibleBlock.CreateVisibleBlock(span, TextView);
                    if (block == null)
                        continue;

                    var tag = tagMappingSpan.Tag;
                    tag.Initialize(TextView);

                    for (var i = 0; i < block.YSegments.Length; i++)
                    {
                        var ySegment = block.YSegments[i];

                        var line = new Line()
                        {
                            SnapsToDevicePixels = true,
                            StrokeThickness = 1.0,
                            X1 = block.X,
                            X2 = block.X,
                            Y1 = ySegment.Start,
                            Y2 = ySegment.End,
                            Stroke = tag.GraphicsTagBrush,
                        };

                        AdornmentLayer.AddAdornment(
                            AdornmentPositioningBehavior.TextRelative,
                            block.Extent,
                            tag: block,
                            line,
                            removedCallback: delegate { });
                    }
                }
            }
        }

        internal sealed class VisibleBlock
        {
            public readonly double X;
            public readonly ImmutableArray<VerticalBlockSpan> YSegments;
            public readonly SnapshotSpan Extent;

            private VisibleBlock(double x, SnapshotSpan extent, ImmutableArray<VerticalBlockSpan> ySegments)
            {
                X = x;
                Extent = extent;
                YSegments = ySegments;
            }

            #region Factory

            public static VisibleBlock? CreateVisibleBlock(SnapshotSpan span, IWpfTextView view)
            {
                // This method assumes that we've already been mapped to the view's snapshot.
                Debug.Assert(span.Snapshot == view.TextSnapshot);

                var guideLineSpanStart = span.Start;
                var guideLineSpanEnd = span.End;

                // We want to draw the line right before the quote character.  So -1 to get that character's position.
                // Horizontally position the adornment in the center of the character.
                var bufferPosition = span.End - 1;
                if (bufferPosition < 0)
                    return null;

                var anchorPointLine = view.GetTextViewLineContainingBufferPosition(bufferPosition);
                var bounds = anchorPointLine.GetCharacterBounds(bufferPosition);
                var x = Math.Floor((bounds.Left + bounds.Right) * 0.5);

                var firstLine = view.TextViewLines.FirstVisibleLine;
                var lastLine = view.TextViewLines.LastVisibleLine;

                // Bug #557472: When performing a layout while scrolling downwards, the editor can occasionally
                // invalidate spans that are not visible. When we ask for the start and end point for adornments,
                // if the TextViewLinesCollection doesn't contain the start or end point of the GuideLineSpan, we
                // usually assume that it's the top of the first visible line, or the bottom of the last visible
                // line, respectively. If the editor invalidates an invisible span, this can result in an erroneous
                // top to bottom adornment.
                if ((guideLineSpanStart > lastLine.End) ||
                    (guideLineSpanEnd < firstLine.Start))
                {
                    return null;
                }

                var guideLineTopLine = view.TextViewLines.GetTextViewLineContainingBufferPosition(guideLineSpanStart);
                var yTop = guideLineTopLine == null ? firstLine.Top : guideLineTopLine.Bottom;

                var guideLineBottomLine = view.TextViewLines.GetTextViewLineContainingBufferPosition(guideLineSpanEnd);
                var yBottom = guideLineBottomLine == null ? lastLine.Bottom : guideLineBottomLine.Top;

                var visibleSegments = CreateVisibleSegments(view.TextViewLines, span, x, yTop, yBottom);

                // This seemingly redundant check prevents a very rare case where a block is created
                // erroneously with no visible segments.
                if (visibleSegments.Length == 0)
                    return null;

                var extent = ContiguousMapUpToView(view, span);
                if (extent == null)
                    return null;

                return new VisibleBlock(x, extent.Value, visibleSegments);
            }

            private static SnapshotSpan? ContiguousMapUpToView(ITextView textView, SnapshotSpan span)
            {
                // Must be called from the UI thread.
                var start = textView.BufferGraph.MapUpToSnapshot(
                    span.Start,
                    PointTrackingMode.Positive,
                    PositionAffinity.Predecessor,
                    textView.TextSnapshot);

                if (start != null)
                {
                    var end = textView.BufferGraph.MapUpToSnapshot(
                        span.End,
                        PointTrackingMode.Negative,
                        PositionAffinity.Successor,
                        textView.TextSnapshot);

                    // Range check is required to guard against the end being
                    // mapped before the start, causing a 'negative' span length.
                    if ((end != null) && (start <= end))
                    {
                        return new SnapshotSpan(start.Value, end.Value);
                    }
                }

                return null;
            }

            private static ImmutableArray<VerticalBlockSpan> CreateVisibleSegments(
                ITextViewLineCollection linesCollection,
                SnapshotSpan extent,
                double x,
                double yTop,
                double yBottom)
            {
                using var _ = ArrayBuilder<VerticalBlockSpan>.GetInstance(out var segments);

                // MinLineHeight must always be larger than ContinuationPadding so that no segments
                // are created for vertical spans between lines.
                const double MinLineHeight = 2.1;
                const double ContinuationPadding = 2.0;

                // Find the lines in Block's extent that are currently visible.
                // TODO: can we eliminate or reuse the allocation for this list?
                var visibleSpanTextViewLinesCollection = linesCollection.GetTextViewLinesIntersectingSpan(extent);

                var currentSegmentTop = yTop;
                var currentSegmentBottom = 0.0;

                // Iterate the visible lines of the Block's extent.
                for (var i = 0; i < visibleSpanTextViewLinesCollection.Count; i++)
                {
                    var line = visibleSpanTextViewLinesCollection[i];
                    var intersectingCharSnapshotPoint = line.GetBufferPositionFromXCoordinate(x);

                    // Three main cases:
                    // A) SV intersects a non-whitespace character. In this case we terminate
                    //    the current segment and start the next segment at the top of the following
                    //    line so that the segment does not intersect with text.
                    // B) Current line is the last visible line and is not a non-whitespace character
                    //    so we terminate the current segment at the bottom of the last visible line
                    //    to ensure that lines with an end-point that is not visible are still drawn.
                    // C) Line is not last line and does not have non-whitespace intersecting the SV
                    //    so we continue the current segment.
                    if ((intersectingCharSnapshotPoint != null &&
                        !char.IsWhiteSpace(intersectingCharSnapshotPoint.Value.GetChar())))
                    {
                        currentSegmentBottom = line.Top;

                        // Only add the structure visualizer adornment line segment if it spans at least
                        // a few pixels in height so we don't have artifacts between lines of intersecting
                        // text.
                        if ((currentSegmentBottom - currentSegmentTop) >= MinLineHeight)
                            segments.Add(new VerticalBlockSpan(currentSegmentTop, currentSegmentBottom));

                        currentSegmentTop = line.Bottom + ContinuationPadding;
                    }
                }

                // Due to mapping between versions of the Snapshots, visibleSpanTextViewLinesCollection
                // may include more lines than are actually in the block, so end at yBottom.
                currentSegmentBottom = yBottom;

                // Only add the structure visualizer adornment line segment if it spans at least
                // an entire line in height so we don't have 1 to 3 pixel artifacts between lines
                // of intersecting text.
                if ((currentSegmentBottom - currentSegmentTop) >= MinLineHeight)
                    segments.Add(new VerticalBlockSpan(currentSegmentTop, currentSegmentBottom));

                return segments.ToImmutable();
            }

            #endregion
        }

        internal readonly struct VerticalBlockSpan
        {
            public readonly double Start;
            public readonly double End;

            public VerticalBlockSpan(double start, double end)
            {
                this.Start = start;
                this.End = end;
            }
        }
    }
}
