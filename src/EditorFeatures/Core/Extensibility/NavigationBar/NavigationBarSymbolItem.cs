// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
