// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.NavigationBar;

internal abstract partial class RoslynNavigationBarItem
{
    /// <summary>
    /// An item that is displayed and can be chosen but which has no action.
    /// </summary>
    // We suppress this as this type *does* override ComputeAdditionalHashCodeParts
    public class ActionlessItem(
        string text,
        Glyph glyph,
        ImmutableArray<RoslynNavigationBarItem> childItems = default,
        int indent = 0,
        bool bolded = false,
        bool grayed = false) : RoslynNavigationBarItem(RoslynNavigationBarItemKind.Actionless, text, glyph, bolded, grayed, indent, childItems), IEquatable<ActionlessItem>
    {
        protected internal override SerializableNavigationBarItem Dehydrate()
            => SerializableNavigationBarItem.ActionlessItem(Text, Glyph, SerializableNavigationBarItem.Dehydrate(ChildItems), Indent, Bolded, Grayed);

        public override bool Equals(object? obj)
            => Equals(obj as ActionlessItem);

        public bool Equals(ActionlessItem? other)
            => base.Equals(other);

        public override int GetHashCode()
            => throw new NotImplementedException();
    }
}
