// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.SolutionExplorer;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

/// <summary>
/// Actual in-memory object that will be presented in the solution explorer tree.  Note:
/// we attempt to reuse instances of these to maintain visual persistence (like selection state)
/// when items are recomputed.
/// </summary>
internal sealed class SymbolTreeItem : BaseItem,
    IInvocationController,
    IAttachedCollectionSource,
    ISupportExpansionEvents,
    INotifyPropertyChanged
{
    public readonly RootSymbolTreeItemSourceProvider RootProvider;
    public readonly DocumentId DocumentId;
    public readonly ISolutionExplorerSymbolTreeItemProvider ItemProvider;
    public readonly SymbolTreeItemKey ItemKey;

    private readonly SymbolTreeChildCollection _childCollection;

    private bool _expanded;
    private SymbolTreeItemSyntax _itemSyntax;

    public SymbolTreeItem(
        RootSymbolTreeItemSourceProvider rootProvider,
        DocumentId documentId,
        ISolutionExplorerSymbolTreeItemProvider itemProvider,
        SymbolTreeItemKey itemKey) : base(itemKey.Name, canPreview: true)
    {
        RootProvider = rootProvider;
        DocumentId = documentId;
        ItemProvider = itemProvider;
        ItemKey = itemKey;
        _childCollection = new(rootProvider, this, hasItems: ItemKey.HasItems);
    }

    private void ThrowIfNotOnMainThread()
        => Contract.ThrowIfFalse(this.RootProvider.ThreadingContext.JoinableTaskContext.IsOnMainThread);

    public SymbolTreeItemSyntax ItemSyntax
    {
        get => _itemSyntax;
        set
        {
            ThrowIfNotOnMainThread();

            // When the syntax node for this item is changed, we want to recompute the children for it
            // (if this  node is expanded). Otherwise, we can just throw away what we have and recompute
            // the next time when asked.
            _itemSyntax = value;
            UpdateChildren();
        }
    }

    public override ImageMoniker IconMoniker => this.ItemKey.Glyph.GetImageMoniker();

    // We act as our own invocation controller.
    public override IInvocationController? InvocationController => this;

    public bool Invoke(IEnumerable<object> items, InputSource inputSource, bool preview)
    {
        if (items.FirstOrDefault() is not SymbolTreeItem item)
            return false;

        RootProvider.NavigationSupport.NavigateTo(item.DocumentId, item.ItemSyntax.NavigationToken.SpanStart, preview);
        return true;
    }

    // We act as our own context menu controller.
    public override IContextMenuController? ContextMenuController => new SymbolItemContextMenuController(this);

    private sealed class SymbolItemContextMenuController : IContextMenuController, IOleCommandTarget
    {
        private readonly SymbolTreeItem _symbolTreeItem;

        public SymbolItemContextMenuController(SymbolTreeItem symbolTreeItem)
        {
            _symbolTreeItem = symbolTreeItem;
        }

        public bool ShowContextMenu(IEnumerable<object> items, Point location)
        {
            if (items.FirstOrDefault() is not SymbolTreeItem item)
                return false;

            var guidContextMenu = Guids.RoslynGroupId;
            if (Shell.Package.GetGlobalService(typeof(SVsUIShell)) is not IVsUIShell shell)
                return false;

            var result = shell.ShowContextMenu(
                dwCompRole: 0,
                ref guidContextMenu,
                //0x400,
                ID.RoslynCommands.SolutionExplorerSymbolItemContextMenu,
                [new() { x = (short)location.X, y = (short)location.Y }],
                pCmdTrgtActive: this);
            return ErrorHandler.Succeeded(result);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return HResult.OK;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            return HResult.OK;
        }
    }

    public void BeforeExpand()
    {
        ThrowIfNotOnMainThread();
        _expanded = true;
        UpdateChildren();
    }

    public void AfterCollapse()
    {
        ThrowIfNotOnMainThread();
        _expanded = false;
        UpdateChildren();
    }

    private void UpdateChildren()
    {
        ThrowIfNotOnMainThread();

        if (_expanded)
        {
            // When we update the item syntax we can reset ourselves to the initial state (if collapsed).
            // Then when we're expanded the next time, we'll recompute the child items properly.  If we 
            // are already expanded, then recompute our children which will recursively push the change
            // down further.
            var items = this.ItemProvider.GetItems(
                _itemSyntax.DeclarationNode, this.RootProvider.ThreadingContext.DisposalToken);
            _childCollection.SetItemsAndMarkComputed_OnMainThread(
                this.DocumentId, this.ItemProvider, items);
        }
        else
        {
            // Otherwise, return the child collection to the uninitialized state.
            _childCollection.ResetToUncomputedState();
        }
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
