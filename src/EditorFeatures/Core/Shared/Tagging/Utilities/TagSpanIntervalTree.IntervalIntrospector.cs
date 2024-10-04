// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging;

internal partial class TagSpanIntervalTree<TTag>
{
    private readonly struct IntervalIntrospector(
        ITextSnapshot snapshot,
        SpanTrackingMode trackingMode)
        : IIntervalIntrospector<TagSpan<TTag>>
    {
        public TextSpan GetSpan(TagSpan<TTag> value)
            => GetTranslatedSpan(value, snapshot, trackingMode).Span.ToTextSpan();
    }
}
