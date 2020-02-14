// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar
{
    internal class NavigationBarSymbolItem : NavigationBarItem
    {
        public SymbolKey NavigationSymbolId { get; }
        public int? NavigationSymbolIndex { get; }

        public NavigationBarSymbolItem(
            string text,
            Glyph glyph,
            IList<TextSpan> spans,
            SymbolKey navigationSymbolId,
            int? navigationSymbolIndex,
            IList<NavigationBarItem> childItems = null,
            int indent = 0,
            bool bolded = false,
            bool grayed = false)
            : base(text, glyph, spans, childItems, indent, bolded, grayed)
        {
            this.NavigationSymbolId = navigationSymbolId;
            this.NavigationSymbolIndex = navigationSymbolIndex;
        }
    }
}
