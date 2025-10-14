// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SolutionExplorer;
using Microsoft.VisualStudio.LanguageServices.Extensions;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

/// <param name="hasItemsDefault">If non-null, the known value for <see cref="HasItems"/>.  If null,
/// then only known once <see cref="_symbolTreeItems"/> is initialized</param>
internal sealed class SymbolTreeChildCollection(
    RootSymbolTreeItemSourceProvider rootProvider,
    object parentItem,
    bool? hasItemsDefault) : IAttachedCollectionSource, INotifyPropertyChanged
{
    private readonly BulkObservableCollectionWithInit<SymbolTreeItem> _symbolTreeItems = [];
    private readonly RootSymbolTreeItemSourceProvider _rootProvider = rootProvider;

    public object SourceItem { get; } = parentItem;

    private bool? _hasItemsDefault = hasItemsDefault;

    /// <summary>
    /// Whether or not we think we have items.  If we aren't fully initialized, then we'll guess that we do have items.
    /// Once fully initialized, we'll return the real result based on what is in our child item list.
    /// </summary>
    public bool HasItems
    {
        get
        {
            // Owner initialized us with a known value for this property.  Can just return that value.
            if (_hasItemsDefault.HasValue)
                return _hasItemsDefault.Value;

            // If we're not initialized yet, we don't know if we have values or not.  Return that we are
            // so the user can at least try to expand this node.
            if (!_symbolTreeItems.IsInitialized)
                return true;

            // We are initialized.  So return the actual state based on what has been computed.
            return _symbolTreeItems.Count > 0;
        }
    }

    public IEnumerable Items => _symbolTreeItems;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ResetToUncomputedState(bool? hasItemsDefault)
    {
        _hasItemsDefault = hasItemsDefault;
        _symbolTreeItems.Clear();

        // Move back to the state where the children are not initialized.  That way the next attemp to open
        // them will compute them.
        MarkInitialized(isInitialized: false);
    }

    public void SetItemsAndMarkComputed_OnMainThread(
        ISolutionExplorerSymbolTreeItemProvider itemProvider,
        ImmutableArray<SymbolTreeItemData> itemDatas)
    {
        Contract.ThrowIfFalse(_rootProvider.ThreadingContext.JoinableTaskContext.IsOnMainThread);

        using var _ = PooledDictionary<SymbolTreeItemKey, ArrayBuilder<SymbolTreeItem>>.GetInstance(out var keyToItems);
        foreach (var item in _symbolTreeItems)
            keyToItems.MultiAdd(item.ItemKey, item);

        using (this._symbolTreeItems.GetBulkOperation())
        {
            // We got some item datas.  Attempt to reuse existing symbol tree items that match up to preserve
            // identity in the tree between changes.
            // Clear out the old items we have.  Then go through setting the final list of items.
            // Attempt to reuse old items if they have the same visible data from before.
            _symbolTreeItems.Clear();

            foreach (var itemData in itemDatas)
            {
                if (keyToItems.TryGetValue(itemData.ItemKey, out var matchingItems))
                {
                    // Found a matching item we can use.  Remove it from the list of items so we don't reuse it again.
                    var matchingItem = matchingItems[0];
                    matchingItems.RemoveAt(0);
                    if (matchingItems.Count == 0)
                        keyToItems.Remove(itemData.ItemKey);

                    Contract.ThrowIfFalse(matchingItem.ItemProvider == itemProvider);
                    Contract.ThrowIfFalse(matchingItem.ItemKey == itemData.ItemKey);

                    // And update it to point to the new syntax information.
                    matchingItem.ItemSyntax = itemData.ItemSyntax;
                    _symbolTreeItems.Add(matchingItem);
                }
                else
                {
                    // If we didn't find an existing item, create a new one.
                    _symbolTreeItems.Add(new(_rootProvider, itemProvider, itemData.ItemKey)
                    {
                        ItemSyntax = itemData.ItemSyntax
                    });
                }
            }
        }

        keyToItems.FreeValues();

        // Once we've been initialized once, mark us that way so that we we move out of the 'spinning/computing' state.
        MarkInitialized(isInitialized: true);
    }

    public void ClearAndMarkComputed_OnMainThread()
    {
        Contract.ThrowIfFalse(_rootProvider.ThreadingContext.JoinableTaskContext.IsOnMainThread);

        using (this._symbolTreeItems.GetBulkOperation())
        {
            _symbolTreeItems.Clear();
        }

        // Once we've been initialized once, mark us that way so that we we move out of the 'spinning/computing' state.
        MarkInitialized(isInitialized: true);
    }

    private void MarkInitialized(bool isInitialized)
    {
        Contract.ThrowIfFalse(_rootProvider.ThreadingContext.JoinableTaskContext.IsOnMainThread);

        _symbolTreeItems.IsInitialized = isInitialized;

        // Notify any listenerrs that our items have changed.
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasItems)));
    }
}
