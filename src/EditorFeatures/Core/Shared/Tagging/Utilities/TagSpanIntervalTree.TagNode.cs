// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging;

internal partial class TagSpanIntervalTree<TTag>
{
    private readonly struct TagNode(ITagSpan<TTag> tagSpan, SpanTrackingMode trackingMode)
    {
        private readonly ITagSpan<TTag> _originalTagSpan = tagSpan;
        public readonly ITrackingSpan Span = tagSpan.Span.CreateTrackingSpan(trackingMode);

        public TTag Tag => _originalTagSpan.Tag;

        public int GetStart(ITextSnapshot textSnapshot)
        {
            var localSpan = _originalTagSpan.Span;

            return localSpan.Snapshot == textSnapshot
                ? localSpan.Start
                : this.Span.GetStartPoint(textSnapshot);
        }

        public int GetLength(ITextSnapshot textSnapshot)
        {
            var localSpan = _originalTagSpan.Span;

            return localSpan.Snapshot == textSnapshot
                ? localSpan.Length
                : this.Span.GetSpan(textSnapshot).Length;
        }
    }
}
