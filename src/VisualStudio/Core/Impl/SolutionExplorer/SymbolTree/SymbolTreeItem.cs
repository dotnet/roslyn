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
using System.ComponentModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal readonly record struct SymbolTreeItemKey(
    string Name,
    Glyph Glyph,
    bool HasItems);

internal readonly record struct SymbolTreeItemSyntax(
    SyntaxNode DeclarationNode,
    SyntaxToken NavigationToken);

internal readonly record struct SymbolTreeItemData(
    SymbolTreeItemKey ItemKey,
    SymbolTreeItemSyntax ItemSyntax)
{
    public SymbolTreeItemData(
        string name,
        Glyph glyph,
        bool hasItems,
        SyntaxNode declarationNode,
        SyntaxToken navigationToken)
        : this(new(name, glyph, hasItems), new(declarationNode, navigationToken))
    {
    }
}

internal sealed class SymbolTreeItem : BaseItem,
    IInvocationController,
    IAttachedCollectionSource,
    ISupportExpansionEvents,
    INotifyPropertyChanged
{
    public readonly RootSymbolTreeItemSourceProvider SourceProvider;
    public readonly DocumentId DocumentId;
    public readonly ISolutionExplorerSymbolTreeItemProvider ItemProvider;
    public readonly SymbolTreeItemKey ItemKey;

    private readonly SymbolTreeChildCollection _childCollection;

    private bool _expanded;
    private SymbolTreeItemSyntax _itemSyntax;

    public SymbolTreeItem(
        RootSymbolTreeItemSourceProvider sourceProvider,
        DocumentId documentId,
        ISolutionExplorerSymbolTreeItemProvider itemProvider,
        SymbolTreeItemKey itemKey) : base(canPreview: true)
    {
        SourceProvider = sourceProvider;
        DocumentId = documentId;
        ItemProvider = itemProvider;
        ItemKey = itemKey;
        _childCollection = new(this);
    }

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
            if (_expanded)
            {
                var items = this.ItemProvider.GetItems(
                    value.DeclarationNode, this.SourceProvider.ThreadingContext.DisposalToken);
                _childCollection.UpdateItems(this.DocumentId, this.ItemProvider, items);
            }
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
        _childCollection.Reset();
    }

    object IAttachedCollectionSource.SourceItem => _childCollection.SourceItem;

    bool IAttachedCollectionSource.HasItems => _childCollection.HasItems;

    IEnumerable IAttachedCollectionSource.Items => _childCollection.Items;

    event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
    {
        add => _childCollection.PropertyChanged += value;
        remove => _childCollection.PropertyChanged -= value;
    }
}
