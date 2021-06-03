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
            public readonly string Name;
            public readonly bool IsObsolete;
            /// <summary>
            /// All the spans in the starting document where this symbol was found.  ANy time the caret is within
            /// one of these spans, the item should be appropriately 'selected' in whatever UI is displaying these.
            /// </summary>
            public readonly ImmutableArray<TextSpan> Spans;
            /// <summary>
            /// The location in the starting document that should be navigated to when this item is selected.
            /// </summary>
            public readonly TextSpan? SelectionSpan;
            public readonly SymbolKey NavigationSymbolId;
            public readonly int NavigationSymbolIndex;

            public SymbolItem(
                string name,
                string text,
                Glyph glyph,
                bool isObsolete,
                ImmutableArray<TextSpan> spans,
                TextSpan? selectionSpan,
                SymbolKey navigationSymbolId,
                int navigationSymbolIndex,
                ImmutableArray<RoslynNavigationBarItem> childItems = default,
                int indent = 0,
                bool bolded = false,
                bool grayed = false)
                : base(RoslynNavigationBarItemKind.Symbol, text, glyph, bolded, grayed, indent, childItems)
            {
                this.Name = name;
                this.IsObsolete = isObsolete;
                this.Spans = spans.NullToEmpty();
                this.SelectionSpan = selectionSpan;
                this.NavigationSymbolId = navigationSymbolId;
                this.NavigationSymbolIndex = navigationSymbolIndex;
            }

            protected internal override SerializableNavigationBarItem Dehydrate()
                => SerializableNavigationBarItem.SymbolItem(Text, Glyph, Name, IsObsolete, Spans, SelectionSpan, NavigationSymbolId, NavigationSymbolIndex, SerializableNavigationBarItem.Dehydrate(ChildItems), Indent, Bolded, Grayed);
        }
    }
}
