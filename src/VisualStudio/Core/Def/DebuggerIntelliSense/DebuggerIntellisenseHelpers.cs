// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense;

internal static class DebuggerIntelliSenseHelpers
{
    public static ITrackingSpan CreateTrackingSpanFromIndexToEnd(this ITextSnapshot textSnapshot, int index, SpanTrackingMode trackingMode)
        => textSnapshot.CreateTrackingSpan(Span.FromBounds(index, textSnapshot.Length), trackingMode);

    public static ITrackingSpan CreateTrackingSpanFromStartToIndex(this ITextSnapshot textSnapshot, int index, SpanTrackingMode trackingMode)
        => textSnapshot.CreateTrackingSpan(Span.FromBounds(0, index), trackingMode);

    public static ITrackingSpan CreateFullTrackingSpan(this ITextSnapshot textSnapshot, SpanTrackingMode trackingMode)
        => textSnapshot.CreateTrackingSpan(Span.FromBounds(0, textSnapshot.Length), trackingMode);
}
