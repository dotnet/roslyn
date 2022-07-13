// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
    internal partial class DocumentOutlineControl : UserControl, IVsCodeWindowEvents
    {
        private readonly ILanguageServiceBrokerShim _languageServiceBroker;

        private readonly IThreadingContext _threadingContext;

        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;

        private readonly IVsCodeWindow _codeWindow;

        private SortOption _sortOption;

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
        /// Queue to batch up work to do to select code in the editor based on the current caret position.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<DocumentSymbolUIItem> _jumpToContentQueue;

        /// <summary>
        /// Keeps track of the current primary and secondary text views. Should only be accessed by the UI thread.
        /// </summary>
        private readonly Dictionary<IVsTextView, ITextView> _trackedTextViews = new();

        public DocumentOutlineControl(
            ILanguageServiceBrokerShim languageServiceBroker,
            IThreadingContext threadingContext,
            IAsynchronousOperationListener asyncListener,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IVsCodeWindow codeWindow,
            CancellationToken cancellationToken)
        {
            InitializeComponent();

            _languageServiceBroker = languageServiceBroker;
            _threadingContext = threadingContext;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _codeWindow = codeWindow;
            ComEventSink.Advise<IVsCodeWindowEvents>(codeWindow, this);
            SortOption = SortOption.Location;

            _computeDataModelQueue = new AsyncBatchingWorkQueue<bool, DocumentSymbolDataModel?>(
                DelayTimeSpan.Short,
                ComputeDataModelAsync,
                EqualityComparer<bool>.Default,
                asyncListener,
                cancellationToken);

            _filterAndSortDataModelQueue = new AsyncBatchingWorkQueue<bool, DocumentSymbolDataModel?>(
                DelayTimeSpan.NearImmediate,
                FilterAndSortDataModelAsync,
                EqualityComparer<bool>.Default,
                asyncListener,
                cancellationToken);

            _determineHighlightAndPresentItemsQueue = new AsyncBatchingWorkQueue<ExpansionOption>(
                DelayTimeSpan.NearImmediate,
                DetermineHighlightedItemAndPresentItemsAsync,
                asyncListener,
                cancellationToken);

            _jumpToContentQueue = new AsyncBatchingWorkQueue<DocumentSymbolUIItem>(
                DelayTimeSpan.NearImmediate,
                JumpToContentAsync,
                asyncListener,
                cancellationToken);

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

            StartComputeDataModelTask();
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
                StartJumpToContentTask(symbol);
        }
    }
}
