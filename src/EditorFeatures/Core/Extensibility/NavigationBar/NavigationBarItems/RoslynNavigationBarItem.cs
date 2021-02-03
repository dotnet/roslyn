// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar
{
    /// <summary>
    /// Base type of all C#/VB navigation bar items.  Only for use internally to roslyn.
    /// </summary>
    internal abstract class RoslynNavigationBarItem : NavigationBarItem
    {
        protected RoslynNavigationBarItem(
            string text,
            Glyph glyph,
            IList<TextSpan> spans,
            IList<NavigationBarItem> childItems = null,
            int indent = 0,
            bool bolded = false,
            bool grayed = false)
            : base(text, glyph, spans, childItems, indent, bolded, grayed)
        {
        }
    }
}
