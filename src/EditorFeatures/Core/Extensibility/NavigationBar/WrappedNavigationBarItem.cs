// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
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
                  GetInDocumentSpans(underlyingItem),
                  GetNavigationSpan(underlyingItem),
                  underlyingItem.ChildItems.SelectAsArray(v => (NavigationBarItem)new WrappedNavigationBarItem(textVersion, v)),
                  underlyingItem.Indent,
                  underlyingItem.Bolded,
                  underlyingItem.Grayed)
        {
            UnderlyingItem = underlyingItem;
        }

        private static ImmutableArray<TextSpan> GetInDocumentSpans(RoslynNavigationBarItem underlyingItem)
        {
            return underlyingItem switch
            {
                // For a regular symbol we want to select it if the user puts their caret in any of the spans of it in this file.
                RoslynNavigationBarItem.SymbolItem { Location.InDocumentInfo: { } symbolInfo } => symbolInfo.spans,

                // An actionless item represents something that exists just to show a child-list, but should otherwise
                // not navigate or cause anything to be generated.  However, we still want to automatically select it whenever
                // the user puts their caret in any of the spans of its child items in this file.
                RoslynNavigationBarItem.ActionlessItem actionless => actionless.ChildItems.SelectMany(i => GetInDocumentSpans(i)).OrderBy(s => s.Start).Distinct().ToImmutableArray(),
                _ => ImmutableArray<TextSpan>.Empty,
            };
        }

        private static TextSpan? GetNavigationSpan(RoslynNavigationBarItem underlyingItem)
        {
            return underlyingItem switch
            {
                // When a symbol item is selected, just navigate to it's preferred location in this file (if we have
                // such a location).  If we don't, then 
                // 
                RoslynNavigationBarItem.SymbolItem { Location.InDocumentInfo: { } symbolInfo } => symbolInfo.navigationSpan,
                _ => null,
            };
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
