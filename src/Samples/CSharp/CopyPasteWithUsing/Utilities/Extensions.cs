// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Roslyn.Samples.CodeAction.CopyPasteWithUsing
{
    internal static class Extensions
    {
        public static ITextBuffer GetBufferContainingCaret(this ITextView textView)
        {
            var point = textView.Caret.Position.Point.GetInsertionPoint(b => b.ContentType.IsOfType("Roslyn C#"));
            return point.HasValue ? point.Value.Snapshot.TextBuffer : null;
        }

        public static IEnumerable<SnapshotSpan> GetSnapshotSpansOnBuffer(this ITextSelection selection, ITextBuffer subjectBuffer)
        {
            Contract.ThrowIfNull(selection);
            Contract.ThrowIfNull(subjectBuffer);

            if (selection.IsEmpty)
            {
                return Array.Empty<SnapshotSpan>();
            }

            var list = new List<SnapshotSpan>();
            foreach (var snapshotSpan in selection.SelectedSpans)
            {
                var bufferGraph = selection.TextView.BufferGraph;
                var snapshotSpansOnSubjectBuffer = bufferGraph.MapDownToBuffer(snapshotSpan, SpanTrackingMode.EdgeExclusive, subjectBuffer);
                foreach (var snapshotSpanOnSubjectBuffer in snapshotSpansOnSubjectBuffer)
                {
                    list.Add(snapshotSpanOnSubjectBuffer);
                }
            }

            return new NormalizedSnapshotSpanCollection(list);
        }

        public static IEnumerable<T> Do<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var value in source)
            {
                action(value);
            }

            return source;
        }
    }
}
