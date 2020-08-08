// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
