// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
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
        public IList<NavigationBarItem> ChildItems { get; }

        public IList<TextSpan> Spans { get; internal set; }
        internal IList<ITrackingSpan> TrackingSpans { get; set; }

        public NavigationBarItem(
            string text,
            Glyph glyph,
            IList<TextSpan> spans,
            IList<NavigationBarItem> childItems = null,
            int indent = 0,
            bool bolded = false,
            bool grayed = false)
        {
            this.Text = text;
            this.Glyph = glyph;
            this.Spans = spans;
            this.ChildItems = childItems ?? SpecializedCollections.EmptyList<NavigationBarItem>();
            this.Indent = indent;
            this.Bolded = bolded;
            this.Grayed = grayed;
        }

        internal void InitializeTrackingSpans(ITextSnapshot textSnapshot)
        {
            this.TrackingSpans = this.Spans.Select(s => textSnapshot.CreateTrackingSpan(s.ToSpan(), SpanTrackingMode.EdgeExclusive)).ToList();

            if (this.ChildItems != null)
            {
                this.ChildItems.Do(i => i.InitializeTrackingSpans(textSnapshot));
            }
        }
    }
}
