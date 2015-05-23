// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// An item that is displayed and can be chosen but which has no action.
    /// </summary>
    internal class NavigationBarActionlessItem : NavigationBarItem
    {
        public NavigationBarActionlessItem(
            string text,
            Glyph glyph,
            IList<TextSpan> spans,
            IList<NavigationBarItem> childItems = null,
            int indent = 0,
            bool bolded = false,
            bool grayed = false)
            : base(
                text,
                glyph,
                spans,
                childItems,
                indent,
                bolded,
                grayed)
        {
        }
    }
}
