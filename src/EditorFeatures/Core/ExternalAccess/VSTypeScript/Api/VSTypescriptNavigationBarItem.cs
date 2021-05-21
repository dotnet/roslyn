// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal class VSTypescriptNavigationBarItem
    {
        public string Text { get; }
        public VSTypeScriptGlyph Glyph { get; }
        public bool Bolded { get; }
        public bool Grayed { get; }
        public int Indent { get; }
        public ImmutableArray<VSTypescriptNavigationBarItem> ChildItems { get; }
        public ImmutableArray<TextSpan> Spans { get; }

        public VSTypescriptNavigationBarItem(
            string text,
            VSTypeScriptGlyph glyph,
            ImmutableArray<TextSpan> spans,
            ImmutableArray<VSTypescriptNavigationBarItem> childItems = default,
            int indent = 0,
            bool bolded = false,
            bool grayed = false)
        {
            this.Text = text;
            this.Glyph = glyph;
            this.Spans = spans.NullToEmpty();
            this.ChildItems = childItems.NullToEmpty();
            this.Indent = indent;
            this.Bolded = bolded;
            this.Grayed = grayed;
        }
    }
}
