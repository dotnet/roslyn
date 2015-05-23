// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// The items that are displayed in the Navigation Bar when it is not expanded. They are never
    /// indented and cannot be used as the target of navigation.
    /// </summary>
    internal class NavigationBarPresentedItem : NavigationBarItem
    {
        public NavigationBarPresentedItem(
            string text,
            Glyph glyph,
            IList<TextSpan> spans,
            IList<NavigationBarItem> childItems = null,
            bool bolded = false,
            bool grayed = false)
            : base(
                  text,
                  glyph,
                  spans,
                  childItems,
                  indent: 0,
                  bolded: bolded,
                  grayed: grayed)
        {
        }
    }
}
