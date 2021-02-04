// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    internal abstract partial class RoslynNavigationBarItem
    {
        public class SymbolItem : RoslynNavigationBarItem
        {
            [DataMember(Order = 8)]
            public readonly SymbolKey NavigationSymbolId;
            [DataMember(Order = 9)]
            public readonly int? NavigationSymbolIndex;

            public SymbolItem(
                string text,
                Glyph glyph,
                ImmutableArray<TextSpan> spans,
                SymbolKey navigationSymbolId,
                int? navigationSymbolIndex,
                ImmutableArray<NavigationBarItem> childItems = default,
                int indent = 0,
                bool bolded = false,
                bool grayed = false)
                : base(RoslynNavigationBarItemKind.Symbol, text, glyph, spans, childItems, indent, bolded, grayed)
            {
                this.NavigationSymbolId = navigationSymbolId;
                this.NavigationSymbolIndex = navigationSymbolIndex;
            }
        }
    }
}
