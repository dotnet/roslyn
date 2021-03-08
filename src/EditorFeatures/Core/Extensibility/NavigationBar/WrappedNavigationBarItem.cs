// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.NavigationBar;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// Implementation of the editor layer <see cref="NavigationBarItem"/> that wraps a feature layer <see cref="RoslynNavigationBarItem"/>
    /// </summary>
    internal class WrappedNavigationBarItem : NavigationBarItem
    {
        public readonly RoslynNavigationBarItem UnderlyingItem;

        internal WrappedNavigationBarItem(RoslynNavigationBarItem underlyingItem)
            : base(
                  underlyingItem.Text,
                  underlyingItem.Glyph,
                  underlyingItem.Spans,
                  underlyingItem.ChildItems.Select(v => new WrappedNavigationBarItem(v)).ToList<NavigationBarItem>(),
                  underlyingItem.Indent,
                  underlyingItem.Bolded,
                  underlyingItem.Grayed)
        {
            UnderlyingItem = underlyingItem;
        }
    }
}
