// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TagSpanIntervalTree<TTag>
    {
        private class TagNode
        {
            public readonly TTag Tag;
            public readonly ITrackingSpan Span;
            private SnapshotSpan _snapshotSpan;

            public TagNode(ITagSpan<TTag> ts, SpanTrackingMode trackingMode)
            {
                _snapshotSpan = ts.Span;
                this.Span = ts.Span.CreateTrackingSpan(trackingMode);
                this.Tag = ts.Tag;
            }

            private SnapshotSpan GetSnapshotSpan(ITextSnapshot textSnapshot)
            {
                var localSpan = _snapshotSpan;
                if (localSpan.Snapshot == textSnapshot)
                {
                    return localSpan;
                }
                else if (localSpan.Snapshot != null)
                {
                    _snapshotSpan = default;
                }

                return default;
            }

            internal int GetStart(ITextSnapshot textSnapshot)
            {
                var localSpan = this.GetSnapshotSpan(textSnapshot);
                return localSpan.Snapshot == textSnapshot
                    ? localSpan.Start
                    : this.Span.GetStartPoint(textSnapshot);
            }

            internal int GetLength(ITextSnapshot textSnapshot)
            {
                var localSpan = this.GetSnapshotSpan(textSnapshot);
                return localSpan.Snapshot == textSnapshot
                    ? localSpan.Length
                    : this.Span.GetSpan(textSnapshot).Length;
            }
        }
    }
}
