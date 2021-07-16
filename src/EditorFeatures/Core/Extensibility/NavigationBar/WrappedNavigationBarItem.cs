// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.NavigationBar;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// Implementation of the editor layer <see cref="NavigationBarItem"/> that wraps a feature layer <see cref="RoslynNavigationBarItem"/>
    /// </summary>
    internal class WrappedNavigationBarItem : NavigationBarItem
    {
        public readonly RoslynNavigationBarItem UnderlyingItem;

        internal WrappedNavigationBarItem(
            RoslynNavigationBarItem underlyingItem, ITextSnapshot textSnapshot)
            : base(
                  underlyingItem.Text,
                  underlyingItem.Glyph,
                  GetTrackingSpans(underlyingItem, textSnapshot),
                  GetNavigationTrackingSpan(underlyingItem, textSnapshot),
                  underlyingItem.ChildItems.SelectAsArray(v => (NavigationBarItem)new WrappedNavigationBarItem(v, textSnapshot)),
                  underlyingItem.Indent,
                  underlyingItem.Bolded,
                  underlyingItem.Grayed)
        {
            UnderlyingItem = underlyingItem;
        }

        private static ImmutableArray<ITrackingSpan> GetTrackingSpans(RoslynNavigationBarItem underlyingItem, ITextSnapshot textSnapshot)
        {
            return underlyingItem is RoslynNavigationBarItem.SymbolItem symbolItem && symbolItem.Location.InDocumentInfo != null
                ? GetTrackingSpans(textSnapshot, symbolItem.Location.InDocumentInfo.Value.spans)
                : ImmutableArray<ITrackingSpan>.Empty;
        }

        private static ITrackingSpan? GetNavigationTrackingSpan(RoslynNavigationBarItem underlyingItem, ITextSnapshot textSnapshot)
        {
            return underlyingItem is RoslynNavigationBarItem.SymbolItem symbolItem && symbolItem.Location.InDocumentInfo != null
                ? GetTrackingSpan(textSnapshot, symbolItem.Location.InDocumentInfo.Value.navigationSpan)
                : null;
        }
    }
}
