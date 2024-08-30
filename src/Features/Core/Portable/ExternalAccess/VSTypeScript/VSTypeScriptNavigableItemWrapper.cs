// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

internal sealed class VSTypeScriptNavigableItemWrapper(IVSTypeScriptNavigableItem navigableItem) : INavigableItem
{
    private readonly IVSTypeScriptNavigableItem _navigableItem = navigableItem;

    public Glyph Glyph => _navigableItem.Glyph;

    public ImmutableArray<TaggedText> DisplayTaggedParts => _navigableItem.DisplayTaggedParts;

    public bool DisplayFileLocation => _navigableItem.DisplayFileLocation;

    public bool IsImplicitlyDeclared => _navigableItem.IsImplicitlyDeclared;

    public INavigableItem.NavigableDocument Document { get; } = INavigableItem.NavigableDocument.FromDocument(navigableItem.Document);

    public TextSpan SourceSpan => _navigableItem.SourceSpan;

    public bool IsStale => false;

    public ImmutableArray<INavigableItem> ChildItems
        => _navigableItem.ChildItems.IsDefault
            ? default
            : _navigableItem.ChildItems.SelectAsArray(i => (INavigableItem)new VSTypeScriptNavigableItemWrapper(i));
}
