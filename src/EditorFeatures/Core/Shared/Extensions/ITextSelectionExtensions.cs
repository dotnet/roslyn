// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class ITextSelectionExtensions
    {
        public static NormalizedSnapshotSpanCollection GetSnapshotSpansOnBuffer(this ITextSelection selection, ITextBuffer subjectBuffer)
        {
            Contract.ThrowIfNull(selection);
            Contract.ThrowIfNull(subjectBuffer);

            var list = new List<SnapshotSpan>();
            foreach (var snapshotSpan in selection.SelectedSpans)
            {
                list.AddRange(selection.TextView.BufferGraph.MapDownToBuffer(snapshotSpan, SpanTrackingMode.EdgeExclusive, subjectBuffer));
            }

            return new NormalizedSnapshotSpanCollection(list);
        }
    }
}
