// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using InternalUtilities = Microsoft.Internal.VisualStudio.PlatformUI.Utilities;
using IOleCommandTarget = Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget;
using OLECMD = Microsoft.VisualStudio.OLE.Interop.OLECMD;
using OLECMDF = Microsoft.VisualStudio.OLE.Interop.OLECMDF;
using OleConstants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline;

/// <summary>
/// Interaction logic for DocumentOutlineView.xaml
/// All operations happen on the UI thread for visual studio
/// </summary>
internal sealed partial class DocumentOutlineView : UserControl, IOleCommandTarget, IDisposable, IVsWindowSearch
{
    private readonly IThreadingContext _threadingContext;
    private readonly IGlobalOptionService _globalOptionService;
    private readonly IOutliningManagerService _outliningManagerService;
    private readonly VsCodeWindowViewTracker _viewTracker;
    private readonly DocumentOutlineViewModel _viewModel;
    private readonly IVsToolbarTrayHost _toolbarTrayHost;
    private readonly IVsWindowSearchHost _windowSearchHost;

    public DocumentOutlineView(
        IVsUIShell4 uiShell,
        IVsWindowSearchHostFactory windowSearchHostFactory,
        IThreadingContext threadingContext,
        IGlobalOptionService globalOptionService,
        IOutliningManagerService outliningManagerService,
        VsCodeWindowViewTracker viewTracker,
        DocumentOutlineViewModel viewModel)
    {
        _threadingContext = threadingContext;
        _globalOptionService = globalOptionService;
        _outliningManagerService = outliningManagerService;
        _viewTracker = viewTracker;
        _viewModel = viewModel;

        DataContext = _viewModel;
        InitializeComponent();
        UpdateSort(_globalOptionService.GetOption(DocumentOutlineOptionsStorage.DocumentOutlineSortOrder), userSelected: false);

        ErrorHandler.ThrowOnFailure(uiShell.CreateToolbarTray(this, out _toolbarTrayHost));
        ErrorHandler.ThrowOnFailure(_toolbarTrayHost.AddToolbar(Guids.RoslynGroupId, ID.RoslynCommands.DocumentOutlineToolbar));

        ErrorHandler.ThrowOnFailure(_toolbarTrayHost.GetToolbarTray(out var toolbarTray));
        ErrorHandler.ThrowOnFailure(toolbarTray.GetUIObject(out var uiObject));
        ErrorHandler.ThrowOnFailure(((IVsUIWpfElement)uiObject).GetFrameworkElement(out var frameworkElement));
        Commands.Content = frameworkElement;

        _windowSearchHost = windowSearchHostFactory.CreateWindowSearchHost(SearchHost);
        _windowSearchHost.SetupSearch(this);

        this.PreviewKeyDown += OnPreviewKeyDown;
        viewTracker.CaretMovedOrActiveViewChanged += ViewTracker_CaretMovedOrActiveViewChanged;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        // If focus is in the Commands toolbar, move it to the Search box on Down or Tab
        if ((e.Key == Key.Down || e.Key == Key.Tab) && Keyboard.Modifiers == ModifierKeys.None && Commands.IsKeyboardFocusWithin)
        {
            if (_windowSearchHost is not null)
            {
                _windowSearchHost.Activate();
                e.Handled = true;
            }
        }
        // If focus is in the Search box, move it to the SymbolTree on Down or Tab
        else if ((e.Key == Key.Down || e.Key == Key.Tab) && Keyboard.Modifiers == ModifierKeys.None && SearchHost.IsKeyboardFocusWithin)
        {
            SymbolTree.Focus();
            if (SymbolTree.Items.Count > 0)
            {
                var firstItem = SymbolTree.ItemContainerGenerator.ContainerFromIndex(0) as TreeViewItem;
                firstItem?.Focus();
            }

            e.Handled = true;
        }
    }

    public void Dispose()
    {
        this.PreviewKeyDown -= OnPreviewKeyDown;
        _toolbarTrayHost.Close();
        _windowSearchHost.TerminateSearch();
        _viewTracker.CaretMovedOrActiveViewChanged -= ViewTracker_CaretMovedOrActiveViewChanged;
        _viewModel.Dispose();
    }

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

    int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
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
                    UpdateSort(SortOption.Name, userSelected: true);
                    return VSConstants.S_OK;

                case ID.RoslynCommands.DocumentOutlineSortByOrder:
                    UpdateSort(SortOption.Location, userSelected: true);
                    return VSConstants.S_OK;

                case ID.RoslynCommands.DocumentOutlineSortByType:
                    UpdateSort(SortOption.Type, userSelected: true);
                    return VSConstants.S_OK;
            }
        }

        return (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;
    }

    bool IVsWindowSearch.SearchEnabled => true;

    Guid IVsWindowSearch.Category => Guids.DocumentOutlineSearchCategoryId;

    IVsEnumWindowSearchFilters? IVsWindowSearch.SearchFiltersEnum => null;

    IVsEnumWindowSearchOptions? IVsWindowSearch.SearchOptionsEnum => null;

    IVsSearchTask IVsWindowSearch.CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
    {
        _viewModel.SearchText = pSearchQuery.SearchString;
        return new VsSearchTask(dwCookie, pSearchQuery, pSearchCallback);
    }

    void IVsWindowSearch.ClearSearch()
    {
        _viewModel.SearchText = "";
    }

    void IVsWindowSearch.ProvideSearchSettings(IVsUIDataSource pSearchSettings)
    {
        InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.ControlMaxWidth, uint.MaxValue);
        InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchStartType, (uint)VSSEARCHSTARTTYPE.SST_DELAYED);
        InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchStartDelay, (uint)100);
        InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchUseMRU, true);
        InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.PrefixFilterMRUItems, false);
        InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.MaximumMRUItems, (uint)25);
        InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchWatermark, ServicesVSResources.Document_Outline_Search);
        InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchPopupAutoDropdown, false);
        InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.ControlBorderThickness, "1");
        InternalUtilities.SetValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchProgressType, (uint)VSSEARCHPROGRESSTYPE.SPT_INDETERMINATE);
    }

    bool IVsWindowSearch.OnNavigationKeyDown(uint dwNavigationKey, uint dwModifiers)
    {
        return false;
    }

    private void UpdateSort(SortOption sortOption, bool userSelected)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        if (userSelected)
        {
            // Log which sort option was used and save it back to the global options
            Logger.Log(sortOption switch
            {
                SortOption.Name => FunctionId.DocumentOutline_SortByName,
                SortOption.Location => FunctionId.DocumentOutline_SortByOrder,
                SortOption.Type => FunctionId.DocumentOutline_SortByType,
                _ => throw new NotImplementedException(),
            }, logLevel: LogLevel.Information);

            _globalOptionService.SetGlobalOption(DocumentOutlineOptionsStorage.DocumentOutlineSortOrder, sortOption);
        }

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
        [new SortDescription(
            $"{nameof(DocumentSymbolDataViewModel.Data)}.{nameof(DocumentSymbolDataViewModel.Data.Name)}",
            ListSortDirection.Ascending)];
    private static ImmutableArray<SortDescription> LocationSortDescriptions { get; } =
        [new SortDescription(
            $"{nameof(DocumentSymbolDataViewModel.Data)}.{nameof(DocumentSymbolDataViewModel.Data.RangeSpan)}.{nameof(DocumentSymbolDataViewModel.Data.RangeSpan.Start)}.{nameof(DocumentSymbolDataViewModel.Data.RangeSpan.Start.Position)}",
            ListSortDirection.Ascending)];
    private static ImmutableArray<SortDescription> TypeSortDescriptions { get; } =
    [
        new SortDescription(
                $"{nameof(DocumentSymbolDataViewModel.Data)}.{nameof(DocumentSymbolDataViewModel.Data.SymbolKind)}",
                ListSortDirection.Ascending),
        new SortDescription(
            $"{nameof(DocumentSymbolDataViewModel.Data)}.{nameof(DocumentSymbolDataViewModel.Data.Name)}",
            ListSortDirection.Ascending),
    ];

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
    private void SymbolTree_SourceUpdated(object sender, DataTransferEventArgs e)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        // 🐉 In practice, this event was firing in cases where the user did not manually select an item in the
        // tree view, resulting in sporadic/unexpected navigation while editing. To filter out these cases, we
        // include a final check that keyboard focus in currently within the selected tree view item, which implies
        // that the keyboard focus is _not_ within the editor (and thus, we will not be interfering with a user who
        // is editing source code). See https://github.com/dotnet/roslyn/issues/69292.
        if (!_viewModel.IsNavigating
            && e.OriginalSource is TreeViewItem { DataContext: DocumentSymbolDataViewModel symbolModel } item
            && FocusHelper.IsKeyboardFocusWithin(item))
        {
            // This is a user-initiated navigation, and we need to prevent reentrancy.  Specifically: when a user
            // does click on an item, we do navigate, and that does move the caret. This part happens synchronously.
            // So we do want to block navigation in that case.
            _viewModel.IsNavigating = true;
            try
            {
                var textView = _viewTracker.GetActiveView();

                // Attempt to move the item to the center of the view.  The user selected the item explicitly, and this
                // gives them a consistent location they can expect to see the result at.
                textView.TryMoveCaretToAndEnsureVisible(
                    symbolModel.Data.SelectionRangeSpan.TranslateTo(textView.TextSnapshot, SpanTrackingMode.EdgeInclusive).Start,
                    _outliningManagerService,
                    EnsureSpanVisibleOptions.AlwaysCenter);
            }
            finally
            {
                _viewModel.IsNavigating = false;
            }
        }
    }

    /// <summary>
    /// When a symbol node in the window is selected, make sure it is visible.
    /// </summary>
    private void SymbolTreeItem_Selected(object sender, RoutedEventArgs e)
    {
        // Construct a rectangle at the left of the item to avoid horizontal scrolling when the items is longer than
        // fits in the view. We make the rectangle 25% the width of the containing tree view to ensure at least some
        // of the text is visible for deeply nested items.
        if (e.OriginalSource is TreeViewItem item)
        {
            double renderHeight;
            if (item.IsExpanded && item.HasItems)
            {
                // The first child is a container. Inside the container are three children:
                // 1. The expander
                // 2. The border for the header item
                // 3. The container for the children
                //
                // For expanded items, we want to only consider the render heigh of the header item, since that is
                // the specific item which is selected.
                var container = VisualTreeHelper.GetChild(item, 0);
                var border = VisualTreeHelper.GetChild(container, 1);
                renderHeight = ((UIElement)border).RenderSize.Height;
            }
            else
            {
                renderHeight = item.RenderSize.Height;
            }

            var croppedRenderWidth = Math.Min(item.RenderSize.Width, SymbolTree.RenderSize.Width / 4);
            item.BringIntoView(new Rect(new Point(0, 0), new Size(croppedRenderWidth, renderHeight)));
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
