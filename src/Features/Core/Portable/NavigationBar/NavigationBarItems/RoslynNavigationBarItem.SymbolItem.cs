// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    internal abstract partial class RoslynNavigationBarItem
    {
        public sealed class SymbolItem : RoslynNavigationBarItem
        {
            public readonly SymbolKey NavigationSymbolId;
            public readonly int NavigationSymbolIndex;

            public SymbolItem(
                string text,
                Glyph glyph,
                ImmutableArray<TextSpan> spans,
                SymbolKey navigationSymbolId,
                int navigationSymbolIndex,
                ImmutableArray<RoslynNavigationBarItem> childItems = default,
                int indent = 0,
                bool bolded = false,
                bool grayed = false)
                : base(RoslynNavigationBarItemKind.Symbol, text, glyph, bolded, grayed, indent, childItems, spans)
            {
                this.NavigationSymbolId = navigationSymbolId;
                this.NavigationSymbolIndex = navigationSymbolIndex;
            }

            protected internal override SerializableNavigationBarItem Dehydrate()
                => SerializableNavigationBarItem.SymbolItem(Text, Glyph, Spans, NavigationSymbolId, NavigationSymbolIndex, SerializableNavigationBarItem.Dehydrate(ChildItems), Indent, Bolded, Grayed);
        }
    }
}
