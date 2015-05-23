// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal enum BufferMapDirection
    {
        Identity,
        Down,
        Up,
        Unrelated
    }

    internal static class IBufferGraphExtensions
    {
        public static SnapshotSpan? MapUpOrDownToFirstMatch(this IBufferGraph bufferGraph, SnapshotSpan span, Predicate<ITextSnapshot> match)
        {
            var spans = bufferGraph.MapDownToFirstMatch(span, SpanTrackingMode.EdgeExclusive, match);
            if (!spans.Any())
            {
                spans = bufferGraph.MapUpToFirstMatch(span, SpanTrackingMode.EdgeExclusive, match);
            }

            return spans.Select(s => (SnapshotSpan?)s).FirstOrDefault();
        }

        public static SnapshotSpan? MapUpOrDownToBuffer(this IBufferGraph bufferGraph, SnapshotSpan span, ITextBuffer targetBuffer)
        {
            var direction = ClassifyBufferMapDirection(span.Snapshot.TextBuffer, targetBuffer);
            switch (direction)
            {
                case BufferMapDirection.Identity:
                    return span;

                case BufferMapDirection.Down:
                    {
                        var spans = bufferGraph.Hack_WorkaroundElisionBuffers_MapDownToBuffer(span, SpanTrackingMode.EdgeExclusive, targetBuffer);
                        return spans.Select(s => (SnapshotSpan?)s).FirstOrDefault();
                    }

                case BufferMapDirection.Up:
                    {
                        var spans = bufferGraph.MapUpToBuffer(span, SpanTrackingMode.EdgeExclusive, targetBuffer);
                        return spans.Select(s => (SnapshotSpan?)s).FirstOrDefault();
                    }

                default:
                    return null;
            }
        }

        public static SnapshotPoint? MapUpOrDownToBuffer(this IBufferGraph bufferGraph, SnapshotPoint point, ITextBuffer targetBuffer)
        {
            var direction = ClassifyBufferMapDirection(point.Snapshot.TextBuffer, targetBuffer);
            switch (direction)
            {
                case BufferMapDirection.Identity:
                    return point;

                case BufferMapDirection.Down:
                    {
                        return bufferGraph.MapDownToBuffer(point, PointTrackingMode.Positive, targetBuffer, PositionAffinity.Predecessor);
                    }

                case BufferMapDirection.Up:
                    {
                        return bufferGraph.MapUpToBuffer(point, PointTrackingMode.Positive, PositionAffinity.Predecessor, targetBuffer);
                    }

                default:
                    return null;
            }
        }

        public static BufferMapDirection ClassifyBufferMapDirection(ITextBuffer startBuffer, ITextBuffer destinationBuffer)
        {
            if (startBuffer == destinationBuffer)
            {
                return BufferMapDirection.Identity;
            }

            // Are we trying to map down or up?
            var startProjBuffer = startBuffer as IProjectionBufferBase;
            if (startProjBuffer != null && IsSourceBuffer(startProjBuffer, destinationBuffer))
            {
                return BufferMapDirection.Down;
            }

            var destProjBuffer = destinationBuffer as IProjectionBufferBase;
            if (destProjBuffer != null && IsSourceBuffer(destProjBuffer, startBuffer))
            {
                return BufferMapDirection.Up;
            }

            return BufferMapDirection.Unrelated;
        }

        private static bool IsSourceBuffer(IProjectionBufferBase top, ITextBuffer bottom)
        {
            return top.SourceBuffers.Contains(bottom) ||
                top.SourceBuffers.OfType<IProjectionBufferBase>().Any(b => IsSourceBuffer(b, bottom));
        }

        public static NormalizedSnapshotSpanCollection Hack_WorkaroundElisionBuffers_MapDownToBuffer(this IBufferGraph bufferGraph, SnapshotSpan span, SpanTrackingMode spanTrackingMode, ITextBuffer targetBuffer)
        {
            var spans = bufferGraph.MapDownToBuffer(span, SpanTrackingMode.EdgeExclusive, targetBuffer);

            // Workaround:
            // An elision buffer that elides an entire buffer (the length of the elision is 0)
            // will return two spans: the null span starting at 0, and a second span that's probably
            // the "real" span. 
            // If we need to map down from an elision buffer and get back multiple spans, and the
            // first one [0,0], ignore it.
            if (span.Snapshot.TextBuffer is IElisionBuffer && spans.Count > 1 && spans[0].Start == 0 && spans[0].Length == 0)
            {
                return new NormalizedSnapshotSpanCollection(spans.Skip(1));
            }

            return spans;
        }
    }
}
