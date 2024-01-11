// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Tagging
{
    internal static class TaggerUtilities
    {
        public static bool SpanEquals(SnapshotSpan? span1, SnapshotSpan? span2, SpanTrackingMode spanTrackingMode)
        {
            if (span1 is null && span2 is null)
                return true;

            if (span1 is null || span2 is null)
                return false;

            // map one span to the snapshot of the other and see if they match.
            span1 = span1.Value.TranslateTo(span2.Value.Snapshot, spanTrackingMode);
            return span1.Value.Span == span2.Value.Span;
        }
    }
}
