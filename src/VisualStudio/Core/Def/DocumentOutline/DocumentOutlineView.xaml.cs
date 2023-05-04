// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using IOleCommandTarget = Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget;
using OLECMD = Microsoft.VisualStudio.OLE.Interop.OLECMD;
using OLECMDF = Microsoft.VisualStudio.OLE.Interop.OLECMDF;
using OleConstants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    /// <summary>
    /// Interaction logic for DocumentOutlineView.xaml
    /// All operations happen on the UI thread for visual studio
    /// </summary>
    internal sealed partial class DocumentOutlineView : UserControl, IOleCommandTarget, IDisposable
    {
        private readonly IThreadingContext _threadingContext;
        private readonly VsCodeWindowViewTracker _viewTracker;
        private readonly DocumentOutlineViewModel _viewModel;
        private readonly IVsToolbarTrayHost _toolbarTrayHost;

        public DocumentOutlineView(
            IVsUIShell4 uiShell,
            IThreadingContext threadingContext,
            VsCodeWindowViewTracker viewTracker,
            DocumentOutlineViewModel viewModel)
        {
            _threadingContext = threadingContext;
            _viewTracker = viewTracker;
            _viewModel = viewModel;

            DataContext = _viewModel;
            InitializeComponent();
            UpdateSort(SortOption.Location); // Set default sort for top-level items

            ErrorHandler.ThrowOnFailure(uiShell.CreateToolbarTray(this, out _toolbarTrayHost));
            ErrorHandler.ThrowOnFailure(_toolbarTrayHost.AddToolbar(Guids.RoslynGroupId, ID.RoslynCommands.DocumentOutlineToolbar));

            ErrorHandler.ThrowOnFailure(_toolbarTrayHost.GetToolbarTray(out var toolbarTray));
            ErrorHandler.ThrowOnFailure(toolbarTray.GetUIObject(out var uiObject));
            ErrorHandler.ThrowOnFailure(((IVsUIWpfElement)uiObject).GetFrameworkElement(out var frameworkElement));
            Commands.Content = frameworkElement;

            viewTracker.CaretMovedOrActiveViewChanged += ViewTracker_CaretMovedOrActiveViewChanged;
        }

        public void Dispose()
        {
            _toolbarTrayHost.Close();
            _viewTracker.CaretMovedOrActiveViewChanged -= ViewTracker_CaretMovedOrActiveViewChanged;
            _viewModel.Dispose();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => _viewModel.SearchText = SearchBox.Text;

        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == Guids.RoslynGroupId)
            {
                for (var i = 0; i < cCmds; i++)
                {
                    switch (prgCmds[i].cmdID)
                    {
                        case ID.RoslynCommands.DocumentOutlineExpandAll:
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                            break;

                        case ID.RoslynCommands.DocumentOutlineCollapseAll:
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                            break;

                        case ID.RoslynCommands.DocumentOutlineSortByName:
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                            if (_viewModel.SortOption == SortOption.Name)
                                prgCmds[i].cmdf |= (uint)OLECMDF.OLECMDF_LATCHED;

                            break;

                        case ID.RoslynCommands.DocumentOutlineSortByOrder:
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                            if (_viewModel.SortOption == SortOption.Location)
                                prgCmds[i].cmdf |= (uint)OLECMDF.OLECMDF_LATCHED;

                            break;

                        case ID.RoslynCommands.DocumentOutlineSortByType:
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                            if (_viewModel.SortOption == SortOption.Type)
                                prgCmds[i].cmdf |= (uint)OLECMDF.OLECMDF_LATCHED;

                            break;

                        default:
                            prgCmds[i].cmdf = 0;
                            break;
                    }
                }

                return VSConstants.S_OK;
            }

            return (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == Guids.RoslynGroupId)
            {
                switch (nCmdID)
                {
                    case ID.RoslynCommands.DocumentOutlineExpandAll:
                        _viewModel.ExpandOrCollapseAll(true);
                        return VSConstants.S_OK;

                    case ID.RoslynCommands.DocumentOutlineCollapseAll:
                        _viewModel.ExpandOrCollapseAll(false);
                        return VSConstants.S_OK;

                    case ID.RoslynCommands.DocumentOutlineSortByName:
                        UpdateSort(SortOption.Name);
                        return VSConstants.S_OK;

                    case ID.RoslynCommands.DocumentOutlineSortByOrder:
                        UpdateSort(SortOption.Location);
                        return VSConstants.S_OK;

                    case ID.RoslynCommands.DocumentOutlineSortByType:
                        UpdateSort(SortOption.Type);
                        return VSConstants.S_OK;
                }
            }

            return (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;
        }

        private void UpdateSort(SortOption sortOption)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            // Log which sort option was used
            Logger.Log(sortOption switch
            {
                SortOption.Name => FunctionId.DocumentOutline_SortByName,
                SortOption.Location => FunctionId.DocumentOutline_SortByOrder,
                SortOption.Type => FunctionId.DocumentOutline_SortByType,
                _ => throw new NotImplementedException(),
            }, logLevel: LogLevel.Information);

            // "DocumentSymbolItems" is the key name we specified for our CollectionViewSource in the XAML file
            var collectionView = ((CollectionViewSource)FindResource("DocumentSymbolItems")).View;

            // Defer changes until all the properties have been set
            using (var _ = collectionView.DeferRefresh())
            {
                // Update top-level sorting options for our tree view
                UpdateSortDescription(collectionView.SortDescriptions, sortOption);

                // Set the sort option property to begin live-sorting
                _viewModel.SortOption = sortOption;
            }

            // Queue a refresh now that everything is set.
            collectionView.Refresh();
        }

        private static ImmutableArray<SortDescription> NameSortDescriptions { get; } =
            ImmutableArray.Create(new SortDescription(
                $"{nameof(DocumentSymbolDataViewModel.Data)}.{nameof(DocumentSymbolDataViewModel.Data.Name)}",
                ListSortDirection.Ascending));
        private static ImmutableArray<SortDescription> LocationSortDescriptions { get; } =
            ImmutableArray.Create(new SortDescription(
                $"{nameof(DocumentSymbolDataViewModel.Data)}.{nameof(DocumentSymbolDataViewModel.Data.RangeSpan)}.{nameof(DocumentSymbolDataViewModel.Data.RangeSpan.Start)}.{nameof(DocumentSymbolDataViewModel.Data.RangeSpan.Start.Position)}",
                ListSortDirection.Ascending));
        private static ImmutableArray<SortDescription> TypeSortDescriptions { get; } = ImmutableArray.Create(
            new SortDescription(
                $"{nameof(DocumentSymbolDataViewModel.Data)}.{nameof(DocumentSymbolDataViewModel.Data.SymbolKind)}",
                ListSortDirection.Ascending),
            new SortDescription(
                $"{nameof(DocumentSymbolDataViewModel.Data)}.{nameof(DocumentSymbolDataViewModel.Data.Name)}",
                ListSortDirection.Ascending));

        public static void UpdateSortDescription(SortDescriptionCollection sortDescriptions, SortOption sortOption)
        {
            sortDescriptions.Clear();
            var newSortDescriptions = sortOption switch
            {
                SortOption.Name => NameSortDescriptions,
                SortOption.Location => LocationSortDescriptions,
                SortOption.Type => TypeSortDescriptions,
                _ => throw new InvalidOperationException(),
            };

            foreach (var newSortDescription in newSortDescriptions)
            {
                sortDescriptions.Add(newSortDescription);
            }
        }

        /// <summary>
        /// When a symbol node in the window is selected via the keyboard, move the caret to its position in the latest active text view.
        /// </summary>
        private void SymbolTreeItem_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            if (!_viewModel.IsNavigating && e.OriginalSource is TreeViewItem { DataContext: DocumentSymbolDataViewModel symbolModel })
            {
                // This is a user-initiated navigation, and we need to prevent reentrancy.  Specifically: when a user
                // does click on an item, we do navigate, and that does move the caret. This part happens synchronously.
                // So we do want to block navigation in that case.
                _viewModel.IsNavigating = true;
                try
                {
                    var textView = _viewTracker.GetActiveView();
                    textView.TryMoveCaretToAndEnsureVisible(
                        symbolModel.Data.SelectionRangeSpan.TranslateTo(textView.TextSnapshot, SpanTrackingMode.EdgeInclusive).Start);
                }
                finally
                {
                    _viewModel.IsNavigating = false;
                }
            }
        }

        /// <summary>
        /// On caret position change, highlight the corresponding symbol node in the window and update the view.
        /// </summary>
        private void ViewTracker_CaretMovedOrActiveViewChanged(object sender, EventArgs e)
        {
            _threadingContext.ThrowIfNotOnUIThread();
            _viewModel.ExpandAndSelectItemAtCaretPosition();
        }
    }
}
