using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
{
    /// <summary>
    /// Wrapper around an ITrackingSpan that holds extra data used to format and modify span.
    /// </summary>
    internal struct CommentTrackingSpan
    {
        public Operation Operation { get; }

        private readonly ITrackingSpan _trackingSpan;

        // In some cases, the tracking span needs to be adjusted by a specific amount after the changes have been applied.
        // These fields store the amount to adjust the span by after edits have been applied.
        private readonly int _amountToAddToStart;
        private readonly int _amountToAddToEnd;

        public CommentTrackingSpan(ITrackingSpan trackingSpan, Operation operation)
        {
            Operation = operation;
            _trackingSpan = trackingSpan;
            _amountToAddToStart = 0;
            _amountToAddToEnd = 0;
        }

        public CommentTrackingSpan(ITrackingSpan trackingSpan, Operation operation, int amountToAddToStart, int amountToAddToEnd)
        {
            Operation = operation;
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
                var spanExpandedByAmount = Span.FromBounds(snapshotSpan.Start.Position + _amountToAddToStart, snapshotSpan.End.Position + _amountToAddToEnd);
                snapshotSpan = new SnapshotSpan(snapshot, spanExpandedByAmount);
            }

            return snapshotSpan;
        }
    }
}
