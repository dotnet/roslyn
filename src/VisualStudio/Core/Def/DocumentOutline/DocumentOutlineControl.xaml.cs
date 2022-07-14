// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageServiceBrokerShim;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    /// <summary>
    /// Interaction logic for DocumentOutlineControl.xaml
    /// </summary>
    internal partial class DocumentOutlineControl : UserControl, IVsCodeWindowEvents, IDisposable
    {
        private readonly ILanguageServiceBrokerShim _languageServiceBroker;
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IVsCodeWindow _codeWindow;
        private readonly ComEventSink _codeWindowEventsSink;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// The type of sorting to be applied to the data model in <see cref="FilterAndSortDataModelAsync"/>.
        /// </summary>
        /// <remarks>
        /// It is only safe to read/mutate SortOption from the UI thread.
        /// </remarks>
        private SortOption SortOption
        {
            get
            {
                _threadingContext.ThrowIfNotOnUIThread();
                return _sortOption;
            }
            set
            {
                _threadingContext.ThrowIfNotOnUIThread();
                _sortOption = value;
            }
        }

        private SortOption _sortOption;

        /// <summary>
        /// Queue to batch up work to do to compute the data model. Used so we can batch up a lot of events 
        /// and only fetch the model once for every batch. The bool parameter is unused.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<bool, DocumentSymbolDataModel?> _computeDataModelQueue;

        /// <summary>
        /// Queue to batch up work to do to filter and sort the data model. The bool parameter is unused.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<bool, DocumentSymbolDataModel?> _filterAndSortDataModelQueue;

        /// <summary>
        /// Queue to batch up work to do to highlight the currently selected symbol node, expand/collapse nodes,
        /// then update the UI.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<ExpansionOption> _determineHighlightAndPresentItemsQueue;

        /// <summary>
        /// Keeps track of the current primary and secondary text views. Should only be accessed by the UI thread.
        /// </summary>
        private readonly Dictionary<IVsTextView, ITextView> _trackedTextViews = new();

        public DocumentOutlineControl(
            ILanguageServiceBrokerShim languageServiceBroker,
            IThreadingContext threadingContext,
            IAsynchronousOperationListener asyncListener,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IVsCodeWindow codeWindow)
        {
            InitializeComponent();

            _languageServiceBroker = languageServiceBroker;
            _threadingContext = threadingContext;
            _asyncListener = asyncListener;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _codeWindow = codeWindow;
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            SortOption = SortOption.Location;

            _computeDataModelQueue = new AsyncBatchingWorkQueue<bool, DocumentSymbolDataModel?>(
                DelayTimeSpan.Short,
                ComputeDataModelAsync,
                EqualityComparer<bool>.Default,
                asyncListener,
                _cancellationToken);

            _filterAndSortDataModelQueue = new AsyncBatchingWorkQueue<bool, DocumentSymbolDataModel?>(
                DelayTimeSpan.NearImmediate,
                FilterAndSortDataModelAsync,
                EqualityComparer<bool>.Default,
                asyncListener,
                _cancellationToken);

            _determineHighlightAndPresentItemsQueue = new AsyncBatchingWorkQueue<ExpansionOption>(
                DelayTimeSpan.NearImmediate,
                DetermineHighlightedItemAndPresentItemsAsync,
                asyncListener,
                _cancellationToken);

            // Primary text view is expected to exist on window initialization.
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
            StartComputeDataModelTask();
        }

        public void Dispose()
        {
            _codeWindowEventsSink.Unadvise();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        int IVsCodeWindowEvents.OnNewView(IVsTextView pView)
        {
            _threadingContext.ThrowIfNotOnUIThread();
            return StartTrackingView(pView);
        }

        private int StartTrackingView(IVsTextView textView)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var wpfTextView = _editorAdaptersFactoryService.GetWpfTextView(textView);
            if (wpfTextView is null)
                return VSConstants.E_FAIL;

            _trackedTextViews.Add(textView, wpfTextView);

            wpfTextView.Caret.PositionChanged += Caret_PositionChanged;

            // Subscribe only once since text buffer is the same for the primary and secondary text views.
            if (_trackedTextViews.Count == 1)
                wpfTextView.TextBuffer.Changed += TextBuffer_Changed;

            return VSConstants.S_OK;
        }

        int IVsCodeWindowEvents.OnCloseView(IVsTextView pView)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            if (_trackedTextViews.TryGetValue(pView, out var view))
            {
                view.Caret.PositionChanged -= Caret_PositionChanged;

                // Unsubscribe only once since text buffer is the same for the primary and secondary text views.
                if (_trackedTextViews.Count == 1)
                    view.TextBuffer.Changed -= TextBuffer_Changed;

                _trackedTextViews.Remove(pView);
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// On text buffer change, start computing the data model.
        /// </summary>
        private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
            => StartComputeDataModelTask();

        /// <summary>
        /// On caret position change in a text view, highlight the corresponding symbol node in the window.
        /// </summary>
        private void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            if (!e.NewPosition.Equals(e.OldPosition))
                StartDetermineHighlightedItemAndPresentItemsTask(ExpansionOption.NoChange);
        }

        private void ExpandAll(object sender, RoutedEventArgs e)
            => StartDetermineHighlightedItemAndPresentItemsTask(ExpansionOption.Expand);

        private void CollapseAll(object sender, RoutedEventArgs e)
            => StartDetermineHighlightedItemAndPresentItemsTask(ExpansionOption.Collapse);

        private void SearchBox_TextChanged(object sender, EventArgs e)
            => StartFilterAndSortDataModelTask();

        private void SortByName(object sender, EventArgs e)
            => UpdateSortOptionAndDataModel(SortOption.Name);

        private void SortByOrder(object sender, EventArgs e)
            => UpdateSortOptionAndDataModel(SortOption.Location);

        private void SortByType(object sender, EventArgs e)
            => UpdateSortOptionAndDataModel(SortOption.Type);

        private void UpdateSortOptionAndDataModel(SortOption sortOption)
        {
            SortOption = sortOption;
            StartFilterAndSortDataModelTask();
        }

        /// <summary>
        /// When a symbol node in the window is clicked, move the caret to its position in the latest active text view.
        /// </summary>
        private void JumpToContent(object sender, EventArgs e)
        {
            if (sender is StackPanel panel && panel.DataContext is DocumentSymbolUIItem symbol)
            {
                var token = _asyncListener.BeginAsyncOperation(nameof(JumpToContentAsync));
                var task = JumpToContentAsync(symbol);
                task.CompletesAsyncOperation(token);
            }
        }

        /// <summary>
        /// Given a DocumentSymbolUIItem, moves the caret to the start of its selection range in the latest active text view.
        /// </summary>
        private async Task JumpToContentAsync(DocumentSymbolUIItem symbol)
        {
            // Switch to the UI thread to update the latest active text view.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(_cancellationToken);

            var activeTextView = GetLastActiveIWpfTextView();
            if (activeTextView is null)
                return;

            // When the user clicks on a symbol node in the window, we want to move the cursor to that line in the editor. If we
            // don't unsubscribe from Caret_PositionChanged first, we will call StartDetermineHighlightedItemAndPresentItemsTask()
            // once we move the cursor ourselves. This is not ideal because we would be doing extra work to highlight a node that's
            // already highlighted.
            activeTextView.Caret.PositionChanged -= Caret_PositionChanged;

            // Prevents us from being permanently unsubscribed if an exception is thrown while updating the text view selection.
            try
            {
                // Map the symbol's selection range starting SnapshotPoint to a SnapshotPoint in the current textview.
                var currentPoint = symbol.SelectionRangeSpan.Start.TranslateTo(activeTextView.TextSnapshot, PointTrackingMode.Negative);

                // Set the active text view selection to this SnapshotPoint (by converting it to a SnapshotSpan).
                var currentSpan = new SnapshotSpan(currentPoint, currentPoint);
                activeTextView.SetSelection(currentSpan);
                activeTextView.ViewScroller.EnsureSpanVisible(currentSpan);
            }
            finally
            {
                // Resubscribe to Caret_PositionChanged again so that when the user clicks somewhere else, we can highlight that node.
                activeTextView.Caret.PositionChanged += Caret_PositionChanged;
            }
        }
    }
}
