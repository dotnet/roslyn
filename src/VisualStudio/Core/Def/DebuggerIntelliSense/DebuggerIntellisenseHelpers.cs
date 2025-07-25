// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense;

internal static class DebuggerIntelliSenseHelpers
{
    extension(ITextSnapshot textSnapshot)
    {
        public ITrackingSpan CreateTrackingSpanFromIndexToEnd(int index, SpanTrackingMode trackingMode)
        => textSnapshot.CreateTrackingSpan(Span.FromBounds(index, textSnapshot.Length), trackingMode);

        public ITrackingSpan CreateTrackingSpanFromStartToIndex(int index, SpanTrackingMode trackingMode)
            => textSnapshot.CreateTrackingSpan(Span.FromBounds(0, index), trackingMode);

        public ITrackingSpan CreateFullTrackingSpan(SpanTrackingMode trackingMode)
            => textSnapshot.CreateTrackingSpan(Span.FromBounds(0, textSnapshot.Length), trackingMode);
    }
}
