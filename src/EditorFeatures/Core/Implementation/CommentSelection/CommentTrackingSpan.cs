// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
{
    /// <summary>
    /// Wrapper around an ITrackingSpan that holds extra data used to format and modify span.
    /// </summary>
    internal struct CommentTrackingSpan
    {
        private readonly ITrackingSpan _trackingSpan;

        // In some cases, the tracking span needs to be adjusted by a specific amount after the changes have been applied.
        // These fields store the amount to adjust the span by after edits have been applied.
        private readonly int _amountToAddToStart;
        private readonly int _amountToAddToEnd;

        public CommentTrackingSpan(ITrackingSpan trackingSpan)
        {
            _trackingSpan = trackingSpan;
            _amountToAddToStart = 0;
            _amountToAddToEnd = 0;
        }

        public CommentTrackingSpan(ITrackingSpan trackingSpan, int amountToAddToStart, int amountToAddToEnd)
        {
            _trackingSpan = trackingSpan;
            _amountToAddToStart = amountToAddToStart;
            _amountToAddToEnd = amountToAddToEnd;
        }

        public Selection ToSelection(ITextBuffer buffer)
        {
            return new Selection(ToSnapshotSpan(buffer.CurrentSnapshot));
        }

        public SnapshotSpan ToSnapshotSpan(ITextSnapshot snapshot)
        {
            var snapshotSpan = _trackingSpan.GetSpan(snapshot);
            if (_amountToAddToStart != 0 || _amountToAddToEnd != 0)
            {
                var updatedStart = snapshotSpan.Start.Position + _amountToAddToStart;
                var updatedEnd = snapshotSpan.End.Position + _amountToAddToEnd;
                if (updatedStart >= snapshotSpan.Start.Position && updatedEnd <= snapshotSpan.End.Position)
                {
                    snapshotSpan = new SnapshotSpan(snapshot, Span.FromBounds(updatedStart, updatedEnd));
                }
            }

            return snapshotSpan;
        }
    }
}
