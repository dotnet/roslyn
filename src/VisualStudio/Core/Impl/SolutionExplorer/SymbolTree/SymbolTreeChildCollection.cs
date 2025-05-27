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

internal sealed class SymbolTreeChildCollection(object parentItem) : IAttachedCollectionSource, INotifyPropertyChanged
{
    private readonly BulkObservableCollectionWithInit<SymbolTreeItem> _symbolTreeItems = [];

    public object SourceItem { get; } = parentItem;
    public bool HasItems => !_symbolTreeItems.IsInitialized || _symbolTreeItems.Count > 0;
    public IEnumerable Items => _symbolTreeItems;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Reset()
    {
        _symbolTreeItems.Clear();

        // Move back to the state where the children are not initialized.  That way the next attemp to open
        // them will compute them.
        _symbolTreeItems.IsInitialized = false;

        // Notify any listenerrs that we may or may not have items now.
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasItems)));
    }

    public void UpdateItems(
        RootSymbolTreeItemSourceProvider rootProvider,
        DocumentId documentId,
        ISolutionExplorerSymbolTreeItemProvider itemProvider,
        ImmutableArray<SymbolTreeItemData> items)
    {
        using (this._symbolTreeItems.GetBulkOperation())
        {
            // We got some item datas.  Attempt to reuse existing symbol tree items that match up to preserve
            // identity in the tree between changes.
            IncorporateNewItems(rootProvider, documentId, itemProvider, items);
        }

        // Once we've been initialized once, mark us that way so that we we move out of the 'spinning/computing' state.
        _symbolTreeItems.IsInitialized = true;

        // Notify any listenerrs that we may or may not have items now.
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasItems)));
    }

    private void IncorporateNewItems(
        RootSymbolTreeItemSourceProvider rootProvider,
        DocumentId documentId,
        ISolutionExplorerSymbolTreeItemProvider itemProvider,
        ImmutableArray<SymbolTreeItemData> datas)
    {
        using var _ = PooledDictionary<SymbolTreeItemKey, ArrayBuilder<SymbolTreeItem>>.GetInstance(out var keyToItems);
        foreach (var item in _symbolTreeItems)
            keyToItems.MultiAdd(item.ItemKey, item);

        _symbolTreeItems.Clear();

        foreach (var data in datas)
        {
            if (keyToItems.TryGetValue(data.ItemKey, out var matchingItems))
            {
                // Found a matching item we can use.  Remove it from the list of items so we don't reuse it again.
                var matchingItem = matchingItems[0];
                matchingItems.RemoveAt(0);
                if (matchingItems.Count == 0)
                    keyToItems.Remove(data.ItemKey);

                // And update it to point to the new syntax information.
                Contract.ThrowIfFalse(matchingItem.DocumentId == documentId);
                Contract.ThrowIfFalse(matchingItem.ItemProvider == itemProvider);
                Contract.ThrowIfFalse(matchingItem.ItemKey == data.ItemKey);

                matchingItem.ItemSyntax = data.ItemSyntax;
                _symbolTreeItems.Add(matchingItem);
            }
            else
            {
                // If we didn't find an existing item, create a new one.
                _symbolTreeItems.Add(new(rootProvider, documentId, itemProvider, data.ItemKey)
                {
                    ItemSyntax = data.ItemSyntax
                });
            }
        }

        keyToItems.FreeValues();
    }
}

//internal abstract class AbstractSymbolTreeItemCollectionSource<TItem>(
//    RootSymbolTreeItemSourceProvider provider,
//    TItem parentItem) : IAttachedCollectionSource, INotifyPropertyChanged
//{
//    protected readonly RootSymbolTreeItemSourceProvider RootProvider = provider;
//    protected readonly TItem ParentItem = parentItem;



//}
