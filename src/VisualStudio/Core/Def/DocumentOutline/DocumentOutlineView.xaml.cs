// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.Text;
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
        private readonly VisualStudioCodeWindowInfoService _visualStudioCodeWindowInfoService;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly Dictionary<IVsTextView, ITextView> _trackedTextViews = new();
        private readonly ComEventSink _codeWindowEventsSink;

        public DocumentOutlineView(
            DocumentOutlineViewModel viewModel,
            VisualStudioCodeWindowInfoService visualStudioCodeWindowInfoService,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IVsCodeWindow codeWindow)
        {
            _viewModel = viewModel;
            _visualStudioCodeWindowInfoService = visualStudioCodeWindowInfoService;
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
            => EnqueueUIUpdateTask(ExpansionOption.Expand);

        private void CollapseAll(object sender, RoutedEventArgs e)
            => EnqueueUIUpdateTask(ExpansionOption.Collapse);

        private void SearchBox_TextChanged(object sender, EventArgs e)
            => EnqueueFilterAndSortTask();

        private void SortByName(object sender, EventArgs e)
            => SetSortOptionAndUpdateDataModel(SortOption.Name);

        private void SortByOrder(object sender, EventArgs e)
            => SetSortOptionAndUpdateDataModel(SortOption.Location);

        private void SortByType(object sender, EventArgs e)
            => SetSortOptionAndUpdateDataModel(SortOption.Type);

        private void SetSortOptionAndUpdateDataModel(SortOption sortOption)
        {
            _viewModel.SortOption = sortOption;
            EnqueueFilterAndSortTask();
        }

        private void EnqueueFilterAndSortTask()
        {
            var service = _visualStudioCodeWindowInfoService.GetServiceAndThrowIfNotOnUIThread();
            var caretPoint = service.GetCurrentCaretSnapshotPoint();
            _viewModel.EnqueueFilterAndSortTask(caretPoint);
        }

        private void EnqueueUIUpdateTask(ExpansionOption option)
        {
            var service = _visualStudioCodeWindowInfoService.GetServiceAndThrowIfNotOnUIThread();
            var caretPoint = service.GetCurrentCaretSnapshotPoint();
            _viewModel.EnqueueModelUpdateTask(option, caretPoint);
        }

        /// <summary>
        /// When a symbol node in the window is clicked, move the caret to its position in the latest active text view.
        /// </summary>
        private void SymbolTree_MouseDown(object sender, EventArgs e)
        {
            if (sender is StackPanel panel && panel.DataContext is DocumentSymbolItemViewModel symbol)
            {
                var service = _visualStudioCodeWindowInfoService.GetServiceAndThrowIfNotOnUIThread();
                var activeTextView = service.GetLastActiveIWpfTextView();
                Assumes.NotNull(activeTextView);

                // When the user clicks on a symbol node in the window, we want to move the cursor to that line in the editor. If we
                // don't unsubscribe from Caret_PositionChanged first, we will call EnqueueHighlightExpandAndPresentItemsTask() once
                // we move the cursor ourselves. This is not ideal because we would be doing extra work to update the view with an
                // identical document symbol tree.
                activeTextView.Caret.PositionChanged -= Caret_PositionChanged;

                // Prevents us from being permanently unsubscribed if an exception is thrown while updating the text view selection.
                try
                {
                    // Map the symbol's selection range start SnapshotPoint to a SnapshotPoint in the current textview then set the
                    // active text view caret position to this SnapshotPoint.
                    activeTextView.TryMoveCaretToAndEnsureVisible(
                        symbol.SelectionRangeSpan.Start.TranslateTo(activeTextView.TextSnapshot, PointTrackingMode.Negative));
                }
                finally
                {
                    // Resubscribe to Caret_PositionChanged again so that when the user clicks somewhere else, we can highlight that node.
                    activeTextView.Caret.PositionChanged += Caret_PositionChanged;
                }
            }
        }

        /// <summary>
        /// On caret position change, highlight the corresponding symbol node in the window and update the view.
        /// </summary>
        private void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            if (!e.NewPosition.Equals(e.OldPosition))
            {
                var service = _visualStudioCodeWindowInfoService.GetServiceAndThrowIfNotOnUIThread();
                var caretPoint = service.GetSnapshotPointFromCaretPosition(e.NewPosition);
                if (caretPoint.HasValue)
                {
                    _viewModel.EnqueueModelUpdateTask(ExpansionOption.CurrentExpansion, caretPoint.Value);
                }
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
