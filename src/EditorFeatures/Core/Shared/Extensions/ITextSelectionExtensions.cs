// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
