// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
