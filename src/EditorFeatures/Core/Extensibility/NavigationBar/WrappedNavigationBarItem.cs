// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.NavigationBar;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// Implementation of the editor layer <see cref="NavigationBarItem"/> that wraps a feature layer <see cref="RoslynNavigationBarItem"/>
    /// </summary>
    // We suppress this as this type *does* override ComputeAdditionalHashCodeParts
    internal sealed class WrappedNavigationBarItem : NavigationBarItem, IEquatable<WrappedNavigationBarItem>
    {
        public readonly RoslynNavigationBarItem UnderlyingItem;

        internal WrappedNavigationBarItem(ITextVersion textVersion, RoslynNavigationBarItem underlyingItem)
            : base(
                  textVersion,
                  underlyingItem.Text,
                  underlyingItem.Glyph,
                  GetSpans(underlyingItem),
                  GetNavigationSpan(underlyingItem),
                  underlyingItem.ChildItems.SelectAsArray(v => (NavigationBarItem)new WrappedNavigationBarItem(textVersion, v)),
                  underlyingItem.Indent,
                  underlyingItem.Bolded,
                  underlyingItem.Grayed)
        {
            UnderlyingItem = underlyingItem;
        }

        private static ImmutableArray<TextSpan> GetSpans(RoslynNavigationBarItem underlyingItem)
        {
            return underlyingItem is RoslynNavigationBarItem.SymbolItem symbolItem && symbolItem.Location.InDocumentInfo != null
                ? symbolItem.Location.InDocumentInfo.Value.spans
                : ImmutableArray<TextSpan>.Empty;
        }

        private static TextSpan? GetNavigationSpan(RoslynNavigationBarItem underlyingItem)
        {
            return underlyingItem is RoslynNavigationBarItem.SymbolItem symbolItem && symbolItem.Location.InDocumentInfo != null
                ? symbolItem.Location.InDocumentInfo.Value.navigationSpan
                : null;
        }

        public override bool Equals(object? obj)
            => Equals(obj as WrappedNavigationBarItem);

        public bool Equals(WrappedNavigationBarItem? other)
            => base.Equals(other) &&
               UnderlyingItem.Equals(other.UnderlyingItem);

        public override int GetHashCode()
            => throw new NotImplementedException();
    }
}
