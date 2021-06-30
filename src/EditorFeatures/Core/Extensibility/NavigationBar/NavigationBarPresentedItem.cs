// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// The items that are displayed in the Navigation Bar when it is not expanded. They are never
    /// indented and cannot be used as the target of navigation.
    /// </summary>
    // We suppress this as this type *does* override ComputeAdditionalHashCodeParts
    internal class NavigationBarPresentedItem : NavigationBarItem, IEquatable<NavigationBarPresentedItem>
    {
        public NavigationBarPresentedItem(
            string text,
            Glyph glyph,
            ImmutableArray<TextSpan> spans,
            TextSpan? navigationSpan,
            ImmutableArray<NavigationBarItem> childItems,
            bool bolded,
            bool grayed)
            : base(text, glyph, spans, navigationSpan, childItems, indent: 0, bolded: bolded, grayed: grayed)
        {
        }

        public override bool Equals(object? obj)
            => Equals(obj as NavigationBarPresentedItem);

        public bool Equals(NavigationBarPresentedItem? other)
            => base.Equals(other);

        public override int GetHashCode()
            => throw new NotImplementedException();
    }
}
