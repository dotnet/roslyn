// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

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

        public ImmutableArray<TextSpan> Spans { get; internal set; }
        internal ImmutableArray<ITrackingSpan> TrackingSpans { get; set; } = ImmutableArray<ITrackingSpan>.Empty;

        // Legacy constructor for TypeScript.
        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        public NavigationBarItem(string text, Glyph glyph, IList<TextSpan> spans, IList<NavigationBarItem>? childItems = null, int indent = 0, bool bolded = false, bool grayed = false)
            : this(text, glyph, spans.ToImmutableArrayOrEmpty(), childItems.ToImmutableArrayOrEmpty(), indent, bolded, grayed)
        {
        }

        public NavigationBarItem(
            string text,
            Glyph glyph,
            ImmutableArray<TextSpan> spans,
            ImmutableArray<NavigationBarItem> childItems = default,
            int indent = 0,
            bool bolded = false,
            bool grayed = false)
        {
            this.Text = text;
            this.Glyph = glyph;
            this.Spans = spans;
            this.ChildItems = childItems.NullToEmpty();
            this.Indent = indent;
            this.Bolded = bolded;
            this.Grayed = grayed;
        }

        internal void InitializeTrackingSpans(ITextSnapshot textSnapshot)
        {
            this.TrackingSpans = this.Spans.SelectAsArray(s => textSnapshot.CreateTrackingSpan(s.ToSpan(), SpanTrackingMode.EdgeExclusive));
            this.ChildItems.Do(i => i.InitializeTrackingSpans(textSnapshot));
        }
    }
}
