// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor
{
    internal class FSharpNavigationBarItem
    {
        public string Text { get; }
        public FSharpGlyph Glyph { get; }
        public bool Bolded { get; }
        public bool Grayed { get; }
        public int Indent { get; }
        public IList<FSharpNavigationBarItem> ChildItems { get; }

        public IList<TextSpan> Spans { get; internal set; }
        internal IList<ITrackingSpan> TrackingSpans { get; set; }

        public FSharpNavigationBarItem(
            string text,
            FSharpGlyph glyph,
            IList<TextSpan> spans,
            IList<FSharpNavigationBarItem> childItems = null,
            int indent = 0,
            bool bolded = false,
            bool grayed = false)
        {
            this.Text = text;
            this.Glyph = glyph;
            this.Spans = spans;
            this.ChildItems = childItems ?? SpecializedCollections.EmptyList<FSharpNavigationBarItem>();
            this.Indent = indent;
            this.Bolded = bolded;
            this.Grayed = grayed;
        }
    }
}
