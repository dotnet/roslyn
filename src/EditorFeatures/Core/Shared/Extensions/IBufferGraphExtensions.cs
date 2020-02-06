// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                        var spans = bufferGraph.MapDownToBuffer(span, SpanTrackingMode.EdgeExclusive, targetBuffer);
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
                        // TODO (https://github.com/dotnet/roslyn/issues/5281): Remove try-catch.
                        try
                        {
                            return bufferGraph.MapDownToInsertionPoint(point, PointTrackingMode.Positive, s => s == targetBuffer.CurrentSnapshot);
                        }
                        catch (ArgumentOutOfRangeException) when (bufferGraph.TopBuffer.ContentType.TypeName == "Interactive Content")
                        {
                            // Suppress this to work around DevDiv #144964.
                            // Note: Other callers might be affected, but this is the narrowest workaround for the observed problems.
                            // A fix is already being reviewed, so a broader change is not required.
                            return null;
                        }
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
            if (startBuffer is IProjectionBufferBase startProjBuffer && IsSourceBuffer(startProjBuffer, destinationBuffer))
            {
                return BufferMapDirection.Down;
            }

            if (destinationBuffer is IProjectionBufferBase destProjBuffer && IsSourceBuffer(destProjBuffer, startBuffer))
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
    }
}
