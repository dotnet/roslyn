// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    internal abstract partial class RoslynNavigationBarItem
    {
        public sealed class SymbolItem : RoslynNavigationBarItem
        {
            public readonly string Name;
            public readonly bool IsObsolete;

            /// <summary>
            /// The full span and navigation span in the originating document where this symbol was found.  Any time the
            /// caret is within the full span, the item should be appropriately 'selected' in whatever UI is displaying
            /// these.  The navigation span is the location in the starting document that should be navigated to when
            /// this item is selected If this symbol's location is in another document then this will be <see
            /// langword="null"/>.
            /// </summary>
            public readonly (TextSpan fullSpan, TextSpan navigationSpan)? InDocumentSpans;
            public readonly (DocumentId documentId, TextSpan span)? OtherDocumentSpans;

            public SymbolItem(
                string name,
                string text,
                Glyph glyph,
                bool isObsolete,
                (TextSpan fullSpan, TextSpan navigationSpan)? inDocumentSpans,
                (DocumentId documentId, TextSpan span)? otherDocumentSpans,
                ImmutableArray<RoslynNavigationBarItem> childItems = default,
                int indent = 0,
                bool bolded = false,
                bool grayed = false)
                : base(RoslynNavigationBarItemKind.Symbol, text, glyph, bolded, grayed, indent, childItems)
            {
                Contract.ThrowIfTrue(inDocumentSpans == null && otherDocumentSpans == null);
                Contract.ThrowIfTrue(inDocumentSpans != null && otherDocumentSpans != null);

                this.Name = name;
                this.IsObsolete = isObsolete;
                InDocumentSpans = inDocumentSpans;
                OtherDocumentSpans = otherDocumentSpans;
            }

            protected internal override SerializableNavigationBarItem Dehydrate()
                => SerializableNavigationBarItem.SymbolItem(Text, Glyph, Name, IsObsolete, InDocumentSpans?.fullSpan, InDocumentSpans?.navigationSpan, OtherDocumentSpans?.documentId, OtherDocumentSpans?.span, SerializableNavigationBarItem.Dehydrate(ChildItems), Indent, Bolded, Grayed);
        }
    }
}
