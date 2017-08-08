// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense
{
    internal static class DebuggerIntelliSenseHelpers
    {
        public static ITrackingSpan CreateTrackingSpanFromIndexToEnd(this ITextSnapshot textSnapshot, int index, SpanTrackingMode trackingMode)
        {
            return textSnapshot.CreateTrackingSpan(Span.FromBounds(index, textSnapshot.Length), trackingMode);
        }

        public static ITrackingSpan CreateTrackingSpanFromStartToIndex(this ITextSnapshot textSnapshot, int index, SpanTrackingMode trackingMode)
        {
            return textSnapshot.CreateTrackingSpan(Span.FromBounds(0, index), trackingMode);
        }

        public static ITrackingSpan CreateFullTrackingSpan(this ITextSnapshot textSnapshot, SpanTrackingMode trackingMode)
        {
            return textSnapshot.CreateTrackingSpan(Span.FromBounds(0, textSnapshot.Length), trackingMode);
        }
    }
}
