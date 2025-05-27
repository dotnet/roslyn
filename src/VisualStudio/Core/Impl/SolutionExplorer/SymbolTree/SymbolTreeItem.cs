// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Roslyn.Utilities;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.CodeAnalysis.Editor.Wpf;
using System.Linq;
using System;
using Microsoft.VisualStudio.Shell;
using System.Collections;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal readonly record struct SymbolTreeItemKey(
    string Name,
    Glyph Glyph,
    bool HasItems);

internal readonly record struct SymbolTreeItemSyntax(
    SyntaxNode DeclarationNode,
    SyntaxToken NavigationToken);

internal readonly record struct SymbolTreeItemData(
    string Name,
    Glyph Glyph,
    bool HasItems,
    SyntaxNode DeclarationNode,
    SyntaxToken NavigationToken)
{
    public SymbolTreeItemKey Key => new(Name, Glyph, HasItems);
}

internal sealed class SymbolTreeItem(
    RootSymbolTreeItemSourceProvider sourceProvider,
    DocumentId documentId,
    ISolutionExplorerSymbolTreeItemProvider itemProvider,
    SymbolTreeItemKey itemKey)
    : BaseItem(canPreview: true),
    IInvocationController,
    IAttachedCollectionSource,
    ISupportExpansionEvents
{
    public readonly RootSymbolTreeItemSourceProvider SourceProvider = sourceProvider;
    public readonly DocumentId DocumentId = documentId;
    public readonly ISolutionExplorerSymbolTreeItemProvider ItemProvider = itemProvider;
    public readonly SymbolTreeItemKey ItemKey = itemKey;

    private bool _expanded;
    private SymbolTreeItemSyntax _itemSyntax;

    public SymbolTreeItemSyntax ItemSyntax
    {
        get => _itemSyntax;
        set
        {
            _itemSyntax = value;

            // When we update the item syntax we can reset ourselves to the initial state (if collapsed).
            // Then when we're expanded the next time, we'll recompute the child items properly.  If we 
            // are already expanded, then recompute our children which will recursively push the change
            // down further.
        }
    }

    public override string Name => this.ItemKey.Name;

    public override ImageMoniker IconMoniker => this.ItemKey.Glyph.GetImageMoniker();

    public override IInvocationController? InvocationController => this;

    public bool Invoke(IEnumerable<object> items, InputSource inputSource, bool preview)
    {
        if (items.FirstOrDefault() is not SymbolTreeItem item)
            return false;

        SourceProvider.NavigateTo(item, preview);
        return true;
    }

    public void BeforeExpand()
    {
        Contract.ThrowIfFalse(SourceProvider.ThreadingContext.JoinableTaskContext.IsOnMainThread);
        _expanded = true;
    }

    public void AfterCollapse()
    {
        Contract.ThrowIfFalse(SourceProvider.ThreadingContext.JoinableTaskContext.IsOnMainThread);
        _expanded = false;
    }

    object IAttachedCollectionSource.SourceItem => this;

    bool IAttachedCollectionSource.HasItems => ;

    IEnumerable IAttachedCollectionSource.Items => throw new NotImplementedException();
}
