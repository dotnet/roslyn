// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
{
    /// <summary>
    /// Helper class to use type-safety to enforce we're using TextSpans from the
    /// TextView's buffer.  Intellisense primarily uses spans from the SubjectBuffer
    /// which need to be mapped to ViewTextSpans before comparing to view positions
    /// such as the current caret location.
    /// </summary>
    internal struct ViewTextSpan
    {
        public readonly TextSpan TextSpan;

        public ViewTextSpan(TextSpan textSpan)
            => this.TextSpan = textSpan;
    }

    internal struct DisconnectedBufferGraph
    {
        // The subject buffer's snapshot at the point of the initial model's creation
        public readonly ITextSnapshot SubjectBufferSnapshot;

        // The TextView's snapshot at the point of the initial model's creation
        public readonly ITextSnapshot ViewSnapshot;

        // The relation of the subject buffer to the TextView's top buffer.  This information
        // is used with the subjectBufferSnapshot and viewSnapshot to map spans even when 
        // the buffers might be temporarily disconnected during Razor/Venus remappings.
        public readonly BufferMapDirection SubjectBufferToTextViewDirection;

        public DisconnectedBufferGraph(ITextBuffer subjectBuffer, ITextBuffer viewBuffer)
        {
            this.SubjectBufferSnapshot = subjectBuffer.CurrentSnapshot;
            this.ViewSnapshot = viewBuffer.CurrentSnapshot;

            this.SubjectBufferToTextViewDirection = IBufferGraphExtensions.ClassifyBufferMapDirection(
                subjectBuffer,
                viewBuffer);
        }

        // Normally, we could just use a BufferGraph to do the mapping, but our subjectBuffer may be 
        // disconnected from the view when we are asked to do this mapping.
        public ViewTextSpan GetSubjectBufferTextSpanInViewBuffer(TextSpan textSpan)
        {
            switch (SubjectBufferToTextViewDirection)
            {
                // The view and subject buffer are the same
                case BufferMapDirection.Identity:
                    return new ViewTextSpan(textSpan);

                // The subject buffer contains the view buffer.  This happens in debugger intellisense.
                case BufferMapDirection.Down:
                    {
                        var projection = SubjectBufferSnapshot as IProjectionSnapshot;
                        var span = MapDownToSnapshot(textSpan.ToSpan(), projection, ViewSnapshot);
                        return new ViewTextSpan(span.ToTextSpan());
                    }

                // The view buffer contains the subject buffer.  This is the typical Razor setup.
                case BufferMapDirection.Up:
                    {
                        var projection = ViewSnapshot as IProjectionSnapshot;
                        var span = MapUpToSnapshot(textSpan.ToSpan(), SubjectBufferSnapshot, projection);
                        return new ViewTextSpan(span.ToTextSpan());
                    }

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        private static Span MapUpToSnapshot(Span span, ITextSnapshot start, IProjectionSnapshot target)
        {
            var spans = MapUpToSnapshotRecursive(new SnapshotSpan(start, span), target);
            return spans.First();
        }

        // Do a depth first search through the projection graph to find the first mapping
        private static IEnumerable<Span> MapUpToSnapshotRecursive(SnapshotSpan start, IProjectionSnapshot target)
        {
            foreach (var source in target.SourceSnapshots)
            {
                if (source == start.Snapshot)
                {
                    foreach (var result in target.MapFromSourceSnapshot(start))
                    {
                        yield return result;
                    }
                }
                else if (source is IProjectionSnapshot sourceProjection)
                {
                    foreach (var span in MapUpToSnapshotRecursive(start, sourceProjection))
                    {
                        foreach (var result in target.MapFromSourceSnapshot(new SnapshotSpan(source, span)))
                        {
                            yield return result;
                        }
                    }
                }
            }

            yield break;
        }

        private static Span MapDownToSnapshot(Span span, IProjectionSnapshot start, ITextSnapshot target)
        {
            var sourceSpans = new Queue<SnapshotSpan>(start.MapToSourceSnapshots(span));
            while (true)
            {
                var sourceSpan = sourceSpans.Dequeue();
                if (sourceSpan.Snapshot == target)
                {
                    return sourceSpan.Span;
                }
                else if (sourceSpan.Snapshot is IProjectionSnapshot)
                {
                    foreach (var s in (sourceSpan.Snapshot as IProjectionSnapshot).MapToSourceSnapshots(sourceSpan.Span))
                    {
                        sourceSpans.Enqueue(s);
                    }
                }
            }
        }
    }
}
