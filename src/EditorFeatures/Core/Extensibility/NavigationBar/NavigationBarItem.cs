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

        /// <summary>
        /// The tracking spans in the owning document corresponding to this nav bar item.  If the user's
        /// caret enters one of these spans, we'll select that item in the nav bar (except if they're in
        /// an item's span that is nested within this).  Tracking spans allow us to know where things are
        /// as the users edits while the computation for the latest items might be going on.
        /// </summary>
        /// <remarks>This can be empty for items whose location is in another document.</remarks>
        public ImmutableArray<ITrackingSpan> TrackingSpans { get; }

        /// <summary>
        /// The tracking span in the owning document corresponding to where to navigate to if the user
        /// selects this item in the drop down.
        /// </summary>
        /// <remarks>This can be <see langword="null"/> for items whose location is in another document.</remarks>
        public ITrackingSpan? NavigationTrackingSpan { get; }

        public NavigationBarItem(
            string text,
            Glyph glyph,
            ImmutableArray<ITrackingSpan> trackingSpans,
            ITrackingSpan? navigationTrackingSpan,
            ImmutableArray<NavigationBarItem> childItems = default,
            int indent = 0,
            bool bolded = false,
            bool grayed = false)
        {
            this.Text = text;
            this.Glyph = glyph;
            this.TrackingSpans = trackingSpans;
            this.NavigationTrackingSpan = navigationTrackingSpan;
            this.ChildItems = childItems.NullToEmpty();
            this.Indent = indent;
            this.Bolded = bolded;
            this.Grayed = grayed;
        }

        internal static ImmutableArray<ITrackingSpan> GetTrackingSpans(ITextSnapshot textSnapshot, ImmutableArray<TextSpan> spans)
            => spans.NullToEmpty().SelectAsArray(static (s, ts) => GetTrackingSpan(ts, s), textSnapshot);

        internal static ITrackingSpan GetTrackingSpan(ITextSnapshot textSnapshot, TextSpan textSpan)
            => textSnapshot.CreateTrackingSpan(textSpan.ToSpan(), SpanTrackingMode.EdgeExclusive);
    }
}
