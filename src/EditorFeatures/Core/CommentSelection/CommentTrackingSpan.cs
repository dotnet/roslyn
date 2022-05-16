// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CommentSelection
{
    /// <summary>
    /// Wrapper around a TextSpan that holds extra data used to create a tracking span.
    /// </summary>
    internal readonly struct CommentTrackingSpan
    {
        public TextSpan TrackingTextSpan { get; }

        // In some cases, the tracking span needs to be adjusted by a specific amount after the changes have been applied.
        // These fields store the amount to adjust the span by after edits have been applied.
        // e.g. The selection begins in a comment -
        //    /*Com[|ment*/ int i = 1;|] -> /*Com*//*ment*/ int i = 1;*/
        // There are two new comment markers added inside the original comment, only the second should be selected.
        public int AmountToAddToTrackingSpanStart { get; }
        public int AmountToAddToTrackingSpanEnd { get; }

        public CommentTrackingSpan(TextSpan trackingTextSpan)
        {
            TrackingTextSpan = trackingTextSpan;
            AmountToAddToTrackingSpanStart = 0;
            AmountToAddToTrackingSpanEnd = 0;
        }

        public CommentTrackingSpan(TextSpan trackingTextSpan, int amountToAddToStart, int amountToAddToEnd)
        {
            TrackingTextSpan = trackingTextSpan;
            AmountToAddToTrackingSpanStart = amountToAddToStart;
            AmountToAddToTrackingSpanEnd = amountToAddToEnd;
        }

        public bool HasPostApplyChanges()
            => AmountToAddToTrackingSpanStart != 0 || AmountToAddToTrackingSpanEnd != 0;
    }
}
