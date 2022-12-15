// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    /// <summary>
    /// Interaction logic for DocumentOutlineView.xaml
    /// All operations happen on the UI thread for visual studio
    /// </summary>
    internal partial class DocumentOutlineView : UserControl, IVsCodeWindowEvents, IDisposable
    {
        private readonly DocumentOutlineViewModel _viewModel;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly Dictionary<IVsTextView, ITextView> _trackedTextViews = new();
        private readonly ComEventSink _codeWindowEventsSink;
        private bool _isNavigating = false;

        public DocumentOutlineView(
            DocumentOutlineViewModel viewModel,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IVsCodeWindow codeWindow)
        {
            _viewModel = viewModel;
            _viewModel.NavigationCompleted += NavigationCompleted;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            DataContext = _viewModel;
            InitializeComponent();

            // We don't think the shell is initialized lazily, so we'll Debug.Fail(), but if it was we'd still
            // see the view created later so this will still function.
            if (ErrorHandler.Failed(codeWindow.GetPrimaryView(out var primaryTextView)))
                Debug.Fail("GetPrimaryView failed during DocumentOutlineControl initialization.");

            if (ErrorHandler.Failed(StartTrackingView(primaryTextView)))
                Debug.Fail("StartTrackingView failed during DocumentOutlineControl initialization.");

            if (ErrorHandler.Succeeded(codeWindow.GetSecondaryView(out var secondaryTextView)))
            {
                if (ErrorHandler.Failed(StartTrackingView(secondaryTextView)))
                    Debug.Fail("StartTrackingView failed during DocumentOutlineControl initialization.");
            }

            _codeWindowEventsSink = ComEventSink.Advise<IVsCodeWindowEvents>(codeWindow, this);
        }

        /// <summary>
        /// Called by our View Model to tell us that navigation in the document has completed
        /// </summary>
        private void NavigationCompleted(object sender, EventArgs e)
        {
            _isNavigating = false;
        }

        private int StartTrackingView(IVsTextView textView)
        {
            var wpfTextView = _editorAdaptersFactoryService.GetWpfTextView(textView);
            if (wpfTextView is null)
                return VSConstants.E_FAIL;

            _trackedTextViews.Add(textView, wpfTextView);

            // In the split window case, there's two views (each with its own caret position) but only one text buffer.
            // Subscribe to caret position changes once per view.
            wpfTextView.Caret.PositionChanged += Caret_PositionChanged;

            return VSConstants.S_OK;
        }

        private void ExpandAll(object sender, RoutedEventArgs e)
            => _viewModel.EnqueueExpandOrCollapse(ExpansionOption.Expand);

        private void CollapseAll(object sender, RoutedEventArgs e)
            => _viewModel.EnqueueExpandOrCollapse(ExpansionOption.Collapse);

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            var newText = SearchBox.Text ?? string.Empty;
            _viewModel.EnqueueFilter(newText);
        }

        private void SortByName(object sender, EventArgs e)
            => UpdateSort(SortOption.Name, nameof(DocumentSymbolDataViewModel.Name));

        private void SortByOrder(object sender, EventArgs e)
            => UpdateSort(SortOption.Location, nameof(DocumentSymbolDataViewModel.StartPosition));

        private void SortByType(object sender, EventArgs e)
            => UpdateSort(SortOption.Type, nameof(DocumentSymbolDataViewModel.SymbolKind), nameof(DocumentSymbolDataViewModel.Name));

        private void UpdateSort(SortOption sortOption, params string[] sortProperties)
        {
            // Log which sort option was used
            Logger.Log(sortOption switch
            {
                SortOption.Name => FunctionId.DocumentOutline_SortByName,
                SortOption.Location => FunctionId.DocumentOutline_SortByOrder,
                SortOption.Type => FunctionId.DocumentOutline_SortByType,
                _ => throw new NotImplementedException(),
            }, logLevel: LogLevel.Information);

            var view = ((CollectionViewSource)FindResource("DocumentSymbolItems")).View;
            view.SortDescriptions.Clear();
            foreach (var sortProperty in sortProperties)
            {
                view.SortDescriptions.Add(new SortDescription(sortProperty, ListSortDirection.Ascending));
            }

            _viewModel.SortOption = sortOption;
            view.Refresh();
        }

        /// <summary>
        /// When a symbol node in the window is clicked, move the caret to its position in the latest active text view.
        /// </summary>
        private void SymbolTree_MouseDown(object sender, EventArgs e)
        {
            if (sender is StackPanel panel && panel.DataContext is DocumentSymbolDataViewModel symbol)
            {
                _isNavigating = true;
                _viewModel.EnqueueNavigation(symbol.SelectionRangeSpan.Span.ToTextSpan());
            }
        }

        /// <summary>
        /// On caret position change, highlight the corresponding symbol node in the window and update the view.
        /// </summary>
        private void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            if (!e.NewPosition.Equals(e.OldPosition) && !_isNavigating)
            {
                _viewModel.EnqueueSelectTreeNode(e.NewPosition);
            }
        }

        int IVsCodeWindowEvents.OnNewView(IVsTextView textView)
        {
            return StartTrackingView(textView);
        }

        int IVsCodeWindowEvents.OnCloseView(IVsTextView textView)
        {
            if (_trackedTextViews.TryGetValue(textView, out var view))
            {
                // In the split window case, there's two views (each with its own caret position) but only one text buffer.
                // Unsubscribe to caret position changes once per view.
                view.Caret.PositionChanged -= Caret_PositionChanged;

                _trackedTextViews.Remove(textView);
            }

            return VSConstants.S_OK;
        }

        public void Dispose()
        {
            _viewModel.Dispose();
            _codeWindowEventsSink.Unadvise();
        }
    }
}
