// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal abstract class NavigationBarItem
    {
        public string Text { get; }
        public Glyph Glyph { get; }
        public bool Bolded { get; }
        public bool Grayed { get; }
        public int Indent { get; }
        public ImmutableArray<NavigationBarItem> ChildItems { get; }

        public ImmutableArray<ITrackingSpan> TrackingSpans { get; }

        public NavigationBarItem(
            string text,
            Glyph glyph,
            ImmutableArray<ITrackingSpan> trackingSpans,
            ImmutableArray<NavigationBarItem> childItems = default,
            int indent = 0,
            bool bolded = false,
            bool grayed = false)
        {
            this.Text = text;
            this.Glyph = glyph;
            this.TrackingSpans = trackingSpans;
            this.ChildItems = childItems.NullToEmpty();
            this.Indent = indent;
            this.Bolded = bolded;
            this.Grayed = grayed;
        }

        internal static ImmutableArray<ITrackingSpan> GetTrackingSpans(ITextSnapshot textSnapshot, ImmutableArray<TextSpan> spans)
            => spans.NullToEmpty().SelectAsArray(static (s, ts) => ts.CreateTrackingSpan(s.ToSpan(), SpanTrackingMode.EdgeExclusive), textSnapshot);
    }
}
