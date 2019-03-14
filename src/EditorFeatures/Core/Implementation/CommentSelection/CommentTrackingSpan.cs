// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
{
    /// <summary>
    /// Wrapper around a TextSpan that holds extra data used to create a tracking span.
    /// </summary>
    internal struct CommentTrackingSpan
    {
        public TextSpan TrackingTextSpan { get; }

        // In some cases, the tracking span needs to be adjusted by a specific amount after the changes have been applied.
        // These fields store the amount to adjust the span by after edits have been applied.
        public int AmountToAddToStart { get; }
        public int AmountToAddToEnd { get; }

        public CommentTrackingSpan(TextSpan trackingTextSpan)
        {
            TrackingTextSpan = trackingTextSpan;
            AmountToAddToStart = 0;
            AmountToAddToEnd = 0;
        }

        public CommentTrackingSpan(TextSpan trackingTextSpan, int amountToAddToStart, int amountToAddToEnd)
        {
            TrackingTextSpan = trackingTextSpan;
            AmountToAddToStart = amountToAddToStart;
            AmountToAddToEnd = amountToAddToEnd;
        }

        public bool HasPostApplyChanges()
        {
            return AmountToAddToStart != 0 || AmountToAddToEnd != 0;
        }
    }
}
