// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    /// <summary>
    /// Base type of all C#/VB navigation bar items.  Only for use internally to roslyn.
    /// </summary>
    internal abstract partial class RoslynNavigationBarItem
    {
        public readonly RoslynNavigationBarItemKind Kind;

        public readonly string Text;
        public readonly Glyph Glyph;
        public readonly bool Bolded;
        public readonly bool Grayed;
        public readonly int Indent;
        public readonly ImmutableArray<RoslynNavigationBarItem> ChildItems;
        public readonly ImmutableArray<TextSpan> Spans;

        protected RoslynNavigationBarItem(
            RoslynNavigationBarItemKind kind,
            string text,
            Glyph glyph,
            bool bolded,
            bool grayed,
            int indent,
            ImmutableArray<RoslynNavigationBarItem> childItems,
            ImmutableArray<TextSpan> spans)
        {
            Kind = kind;
            Text = text;
            Glyph = glyph;
            Spans = spans.NullToEmpty();
            ChildItems = childItems.NullToEmpty();
            Indent = indent;
            Bolded = bolded;
            Grayed = grayed;
        }

        protected internal abstract SerializableNavigationBarItem Dehydrate();
    }
}
