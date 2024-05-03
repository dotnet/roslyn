// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.NavigationBar;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor;

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
              underlyingItem.ChildItems.SelectAsArray(v => (NavigationBarItem)new WrappedNavigationBarItem(textVersion, v)),
              underlyingItem.Indent,
              underlyingItem.Bolded,
              underlyingItem.Grayed)
    {
        UnderlyingItem = underlyingItem;
    }

    private static ImmutableArray<TextSpan> GetSpans(RoslynNavigationBarItem underlyingItem)
    {
        using var _ = ArrayBuilder<TextSpan>.GetInstance(out var spans);
        AddSpans(underlyingItem, spans);
        spans.SortAndRemoveDuplicates(Comparer<TextSpan>.Default);
        return spans.ToImmutableAndClear();

        static void AddSpans(RoslynNavigationBarItem underlyingItem, ArrayBuilder<TextSpan> spans)
        {
            // For a regular symbol we want to select it if the user puts their caret in any of the spans of it in this file.
            if (underlyingItem is RoslynNavigationBarItem.SymbolItem { Location.InDocumentInfo.spans: var symbolSpans })
            {
                spans.AddRange(symbolSpans);
            }
            else if (underlyingItem is RoslynNavigationBarItem.ActionlessItem)
            {
                // An actionless item represents something that exists just to show a child-list, but should otherwise
                // not navigate or cause anything to be generated.  However, we still want to automatically select it
                // whenever the user puts their caret in any of the spans of its child items in this file.
                //
                // For example, in VB any withevents members will be put in the type-list, and the events those members
                // are hooked up to will then be in the member-list.  In this case, we want moving into the span of that
                // member to select the withevent member in the type-list.
                foreach (var child in underlyingItem.ChildItems)
                    AddSpans(child, spans);
            }
        }
    }

    public override bool Equals(object? obj)
        => Equals(obj as WrappedNavigationBarItem);

    public bool Equals(WrappedNavigationBarItem? other)
        => base.Equals(other) &&
           UnderlyingItem.Equals(other.UnderlyingItem);

    public override int GetHashCode()
        => throw new NotImplementedException();
}
