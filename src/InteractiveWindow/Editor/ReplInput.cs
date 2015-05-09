// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal sealed class ReplSpan
    {
        // ITrackingSpan or string
        public object Span { get; }
        public ReplSpanKind Kind { get; }

        public ReplSpan(CustomTrackingSpan span, ReplSpanKind kind)
        {
            Debug.Assert(!kind.IsPrompt());
            this.Span = span;
            this.Kind = kind;
        }

        public ReplSpan(string litaral, ReplSpanKind kind)
        {
            this.Span = litaral;
            this.Kind = kind;
        }

        public string InertValue
        {
            get { return (string)Span; }
        }

        public CustomTrackingSpan TrackingSpan
        {
            get { return (CustomTrackingSpan)Span; }
        }

        public ReplSpan WithEndTrackingMode(PointTrackingMode endTrackingMode)
        {
            return new ReplSpan(((CustomTrackingSpan)this.Span).WithEndTrackingMode(endTrackingMode), this.Kind);
        }

        public int Length
        {
            get
            {
                return Span is string ? InertValue.Length : TrackingSpan.GetSpan(TrackingSpan.TextBuffer.CurrentSnapshot).Length;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Kind, Span);
        }
    }
}
