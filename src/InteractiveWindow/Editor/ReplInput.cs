// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    [DebuggerDisplay("{GetDebuggerDisplay()}")]
    internal struct ReplSpan
    {
        // CustomTrackingSpan or string
        public object Span { get; }
        public ReplSpanKind Kind { get; }
        public int LineNumber { get; }

        public ReplSpan(CustomTrackingSpan span, ReplSpanKind kind, int lineNumber)
            : this((object)span, kind, lineNumber)
        {
            Debug.Assert(!kind.IsPrompt());
        }

        public ReplSpan(string literal, ReplSpanKind kind, int lineNumber)
            : this((object)literal, kind, lineNumber)
        {
        }

        private ReplSpan(object span, ReplSpanKind kind, int lineNumber)
        {
            this.Span = span;
            this.Kind = kind;
            this.LineNumber = lineNumber;
        }

        public CustomTrackingSpan TrackingSpan => (CustomTrackingSpan)Span;

        public ReplSpan WithEndTrackingMode(PointTrackingMode endTrackingMode)
        {
            return new ReplSpan(((CustomTrackingSpan)this.Span).WithEndTrackingMode(endTrackingMode), this.Kind, this.LineNumber);
        }

        public ReplSpan WithLineNumber(int lineNumber)
        {
            return new ReplSpan(this.Span, this.Kind, lineNumber);
        }

        public int Length
        {
            get
            {
                var value = Span as string;
                return (value != null) ? value.Length : TrackingSpan.GetSpan(TrackingSpan.TextBuffer.CurrentSnapshot).Length;
            }
        }

        private string GetDebuggerDisplay()
        {
            return $"Line {LineNumber}: {Kind} - {Span}";
        }
    }
}
