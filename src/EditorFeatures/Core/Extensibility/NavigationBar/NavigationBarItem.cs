// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal abstract class NavigationBarItem : IEquatable<NavigationBarItem>
    {
        public string Text { get; }
        public Glyph Glyph { get; }
        public bool Bolded { get; }
        public bool Grayed { get; }
        public int Indent { get; }
        public ImmutableArray<NavigationBarItem> ChildItems { get; }

        /// <summary>
        /// The spans in the owning document corresponding to this nav bar item.  If the user's caret enters one of
        /// these spans, we'll select that item in the nav bar (except if they're in an item's span that is nested
        /// within this).
        /// </summary>
        /// <remarks>This can be empty for items whose location is in another document.</remarks>
        public ImmutableArray<TextSpan> Spans { get; }

        /// <summary>
        /// The span in the owning document corresponding to where to navigate to if the user selects this item in the
        /// drop down.
        /// </summary>
        /// <remarks>This can be <see langword="null"/> for items whose location is in another document.</remarks>
        public TextSpan? NavigationSpan { get; }

        internal ITextVersion? TextVersion { get; }

        public NavigationBarItem(
            ITextVersion? textVersion,
            string text,
            Glyph glyph,
            ImmutableArray<TextSpan> spans,
            TextSpan? navigationSpan,
            ImmutableArray<NavigationBarItem> childItems = default,
            int indent = 0,
            bool bolded = false,
            bool grayed = false)
        {
            TextVersion = textVersion;
            Text = text;
            Glyph = glyph;
            Spans = spans;
            NavigationSpan = navigationSpan;
            ChildItems = childItems.NullToEmpty();
            Indent = indent;
            Bolded = bolded;
            Grayed = grayed;
        }

        public TextSpan? TryGetNavigationSpan(ITextVersion textVersion)
        {
            if (this.NavigationSpan == null)
                return null;

            return this.TextVersion!.CreateTrackingSpan(this.NavigationSpan.Value.ToSpan(), SpanTrackingMode.EdgeExclusive)
                                    .GetSpan(textVersion).ToTextSpan();
        }

        public abstract override bool Equals(object? obj);
        public abstract override int GetHashCode();

        public bool Equals(NavigationBarItem? other)
        {
            return other != null &&
                   Text == other.Text &&
                   Glyph == other.Glyph &&
                   Bolded == other.Bolded &&
                   Grayed == other.Grayed &&
                   Indent == other.Indent &&
                   ChildItems.SequenceEqual(other.ChildItems) &&
                   Spans.SequenceEqual(other.Spans) &&
                   EqualityComparer<TextSpan?>.Default.Equals(NavigationSpan, other.NavigationSpan);
        }
    }
}
