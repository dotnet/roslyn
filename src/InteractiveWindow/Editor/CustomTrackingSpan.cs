// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// This is a custom span which is like an EdgeInclusive span.  We need a custom span because elision buffers
    /// do not allow EdgeInclusive unless it spans the entire buffer.  We create snippets of our language spans
    /// and these are initially zero length.  When we insert at the beginning of these we'll end up keeping the
    /// span zero length if we're just EdgePositive tracking.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal sealed class CustomTrackingSpan : ITrackingSpan
    {
        private readonly ITrackingPoint _start;
        private readonly ITrackingPoint _end;

        public CustomTrackingSpan(ITextSnapshot snapshot, Span span, bool canAppend = false)
        {
            _start = snapshot.CreateTrackingPoint(span.Start, PointTrackingMode.Negative);
            _end = snapshot.CreateTrackingPoint(span.End, canAppend ? PointTrackingMode.Positive : PointTrackingMode.Negative);
        }

        #region ITrackingSpan Members

        public SnapshotPoint GetEndPoint(ITextSnapshot snapshot)
        {
            return _end.GetPoint(snapshot);
        }

        public Span GetSpan(ITextVersion version)
        {
            return Span.FromBounds(_start.GetPosition(version), _end.GetPosition(version));
        }

        public SnapshotSpan GetSpan(ITextSnapshot snapshot)
        {
            return new SnapshotSpan(snapshot, Span.FromBounds(_start.GetPoint(snapshot), _end.GetPoint(snapshot)));
        }

        public SnapshotPoint GetStartPoint(ITextSnapshot snapshot)
        {
            return _start.GetPoint(snapshot);
        }

        public string GetText(ITextSnapshot snapshot)
        {
            return GetSpan(snapshot).GetText();
        }

        public ITextBuffer TextBuffer
        {
            get { return _start.TextBuffer; }
        }

        public TrackingFidelityMode TrackingFidelity
        {
            get { return TrackingFidelityMode.Forward; }
        }

        public SpanTrackingMode TrackingMode
        {
            get { return SpanTrackingMode.Custom; }
        }

        #endregion

        private string GetDebuggerDisplay()
        {
            return "CustomSpan: " + GetSpan(_start.TextBuffer.CurrentSnapshot).ToString();
        }
    }
}
