// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.LanguageServices.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking;

internal class TreeViewItemBase : ViewModelBase
{
    public ObservableCollection<TreeViewItemBase> ChildItems { get; } = [];
    public TreeViewItemBase? Parent { get; set; }

    public virtual string AutomationName { get; } = string.Empty;

    private bool _isExpanded = false;
    public virtual bool IsNodeExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    private bool _isSelected = false;
    public bool IsNodeSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public TreeViewItemBase()
    {
        ChildItems.CollectionChanged += ChildItems_CollectionChanged;
    }

    /// <summary>
    /// Returns the next logical item in the tree that could be seen (Parent is expanded)
    /// </summary>
    public TreeViewItemBase GetNextInTree()
    {
        if (IsNodeExpanded && ChildItems.Any())
        {
            return ChildItems.First();
        }

        var sibling = GetSibling(next: true);
        if (sibling is not null)
        {
            return sibling;
        }

        return Parent?.GetSibling(next: true) ?? this;
    }

    /// <summary>
    /// Returns the previous logical item in the tree that could be seen (Parent is expanded)
    /// </summary>
    public TreeViewItemBase GetPreviousInTree()
    {
        var sibling = GetSibling(next: false);
        if (sibling is not null)
        {
            return sibling.GetLastVisibleDescendentOrSelf();
        }

        return Parent ?? this;
    }

    private TreeViewItemBase GetLastVisibleDescendentOrSelf()
    {
        if (!IsNodeExpanded || ChildItems.Count == 0)
        {
            return this;
        }

        var lastChild = ChildItems.Last();
        return lastChild.GetLastVisibleDescendentOrSelf();
    }

    private TreeViewItemBase? GetSibling(bool next = true)
    {
        if (Parent is null)
        {
            return null;
        }

        var thisIndex = Parent.ChildItems.IndexOf(this);
        var siblingIndex = next ? thisIndex + 1 : thisIndex - 1;

        if (siblingIndex < 0 || siblingIndex >= Parent.ChildItems.Count)
        {
            return null;
        }

        return Parent.ChildItems[siblingIndex];
    }

    private void ChildItems_CollectionChanged(object _, NotifyCollectionChangedEventArgs args)
    {
        if (args.Action is not NotifyCollectionChangedAction.Add and not NotifyCollectionChangedAction.Remove)
        {
            return;
        }

        SetParents(args.OldItems, null);
        SetParents(args.NewItems, this);

        static void SetParents(IList? items, TreeViewItemBase? parent)
        {
            if (items is null)
            {
                return;
            }

            foreach (var item in items.Cast<TreeViewItemBase>())
            {
                item.Parent = parent;
            }
        }
    }
}
