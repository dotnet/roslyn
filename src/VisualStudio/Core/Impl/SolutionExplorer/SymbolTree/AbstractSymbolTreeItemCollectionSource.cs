// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Extensions;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal abstract class AbstractSymbolTreeItemCollectionSource<TItem>(
    RootSymbolTreeItemSourceProvider provider,
    TItem parentItem) : IAttachedCollectionSource, INotifyPropertyChanged
{
    protected readonly RootSymbolTreeItemSourceProvider RootProvider = provider;
    protected readonly TItem ParentItem = parentItem;

    protected readonly BulkObservableCollectionWithInit<SymbolTreeItem> SymbolTreeItems = [];

    public object SourceItem { get; } = parentItem!;
    public bool HasItems => !SymbolTreeItems.IsInitialized || SymbolTreeItems.Count > 0;
    public IEnumerable Items => SymbolTreeItems;

    public event PropertyChangedEventHandler PropertyChanged = null!;

    protected void UpdateItems(
        DocumentId documentId,
        ISolutionExplorerSymbolTreeItemProvider itemProvider,
        ImmutableArray<SymbolTreeItemData> items)
    {
        using (this.SymbolTreeItems.GetBulkOperation())
        {
            if (items.Length == 0)
            {
                // If we got no items, clear everything out.
                this.SymbolTreeItems.Clear();
            }
            else
            {
                // We got some item datas.  Attempt to reuse existing symbol tree items that match up to preserve
                // identity in the tree between changes.
                IncorporateNewItems(documentId, itemProvider, items);
            }
        }

        // Once we've been initialized once, mark us that way so that we we move out of the 'spinning/computing' state.
        this.SymbolTreeItems.MarkAsInitialized();

        // Notify any listenerrs that we may or may not have items now.
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasItems)));
    }

    private void IncorporateNewItems(
        DocumentId documentId,
        ISolutionExplorerSymbolTreeItemProvider itemProvider,
        ImmutableArray<SymbolTreeItemData> datas)
    {
        using var _ = PooledDictionary<SymbolTreeItemKey, ArrayBuilder<SymbolTreeItem>>.GetInstance(out var keyToItems);
        foreach (var item in this.SymbolTreeItems)
            keyToItems.MultiAdd(item.ItemKey, item);

        this.SymbolTreeItems.Clear();

        foreach (var data in datas)
        {
            if (keyToItems.TryGetValue(data.Key, out var matchingItems))
            {
                // Found a matching item we can use.  Remove it from the list of items so we don't reuse it again.
                var matchingItem = matchingItems[0];
                matchingItems.RemoveAt(0);
                if (matchingItems.Count == 0)
                    keyToItems.Remove(data.Key);

                // And update it to point to the new data.
                Contract.ThrowIfFalse(matchingItem.DocumentId == documentId);
                Contract.ThrowIfFalse(matchingItem.ItemProvider == itemProvider);
                Contract.ThrowIfFalse(matchingItem.ItemKey == data.Key);

                matchingItem.ItemSyntax = new(data.DeclarationNode, data.NavigationToken);
                this.SymbolTreeItems.Add(matchingItem);
            }
            else
            {
                // If we didn't find an existing item, create a new one.
                this.SymbolTreeItems.Add(new(this.RootProvider, documentId, itemProvider)
                {
                    ItemKey = data.Key,
                    ItemSyntax = new(data.DeclarationNode, data.NavigationToken)
                });
            }
        }

        keyToItems.FreeValues();
    }
}
