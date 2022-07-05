// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.DocumentOutline;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageServiceBrokerShim;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Interaction logic for DocumentOutlineControl.xaml
    /// </summary>
    internal partial class DocumentOutlineControl : UserControl, IVsCodeWindowEvents
    {
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; }
        public IVsCodeWindow CodeWindow { get; }

        private IThreadingContext ThreadingContext { get; }
        private ILanguageServiceBrokerShim LanguageServiceBroker { get; }

        /// <summary>
        /// The type of sorting applied to the document model.
        /// </summary>
        private SortOption SortOption { get; set; }

        /// <summary>
        /// Queue to batch up work to do to get the current document model. Used so we can batch up a lot of events 
        /// and only fetch the model once for every batch.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<bool, DocumentSymbolModel?> _computeModelQueue;

        /// <summary>
        /// Queue to batch up work to do to update the current document model and UI.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<bool, DocumentSymbolModel?> _updateUIQueue;

        /// <summary>
        /// Queue to batch up work to do to highlight the currently selected symbol node.
        /// </summary>
        private readonly AsyncBatchingWorkQueue _highlightNodeQueue;

        /// <summary>
        /// Queue to batch up work to do to select code in the editor based on the current caret position.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<DocumentSymbolItem> _jumpToContentQueue;

        /// <summary>
        /// Keeps track of the current primary and secondary text views.
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

            ThreadingContext = threadingContext;
            LanguageServiceBroker = languageServiceBroker;
            EditorAdaptersFactoryService = editorAdaptersFactoryService;
            CodeWindow = codeWindow;
            ComEventSink.Advise<IVsCodeWindowEvents>(codeWindow, this);
            SortOption = SortOption.Order;

            _computeModelQueue = new AsyncBatchingWorkQueue<bool, DocumentSymbolModel?>(
                DelayTimeSpan.Short,
                ComputeModelAndUpdateUIAsync,
                EqualityComparer<bool>.Default,
                asyncListener,
                threadingContext.DisposalToken);

            _updateUIQueue = new AsyncBatchingWorkQueue<bool, DocumentSymbolModel?>(
                    DelayTimeSpan.NearImmediate,
                    UpdateUIAsync,
                    EqualityComparer<bool>.Default,
                    asyncListener,
                    threadingContext.DisposalToken);

            _highlightNodeQueue = new AsyncBatchingWorkQueue(
                    DelayTimeSpan.NearImmediate,
                    HightlightNodeAsync,
                    asyncListener,
                    threadingContext.DisposalToken);

            _jumpToContentQueue = new AsyncBatchingWorkQueue<DocumentSymbolItem>(
                    DelayTimeSpan.NearImmediate,
                    JumpToContentAsync,
                    asyncListener,
                    threadingContext.DisposalToken);

            codeWindow.GetPrimaryView(out var pTextViewPrimary);
            StartTrackingView(pTextViewPrimary, out var textViewPrimary);

            // Primary text view should always exist on window initialization unless an error is thrown.
            if (textViewPrimary is null)
                return;

            codeWindow.GetSecondaryView(out var pTextViewSecondary);
            if (pTextViewSecondary is not null)
                StartTrackingView(pTextViewSecondary, out var _);

            StartComputeModelTask();
        }

        int IVsCodeWindowEvents.OnNewView(IVsTextView pView)
        {
            StartTrackingView(pView, out var _);
            return VSConstants.S_OK;
        }

        private void StartTrackingView(IVsTextView pTextView, out IWpfTextView? wpfTextView)
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            wpfTextView = null;
            if (pTextView != null)
            {
                wpfTextView = EditorAdaptersFactoryService.GetWpfTextView(pTextView);
                if (wpfTextView != null)
                {
                    _trackedTextViews.Add(pTextView, wpfTextView);
                    wpfTextView.Caret.PositionChanged += Caret_PositionChanged;
                    wpfTextView.TextBuffer.Changed += TextBuffer_Changed;
                }
            }
        }

        int IVsCodeWindowEvents.OnCloseView(IVsTextView pView)
        {
            if (_trackedTextViews.TryGetValue(pView, out var view))
            {
                view.Caret.PositionChanged -= Caret_PositionChanged;
                view.TextBuffer.Changed -= TextBuffer_Changed;

                _trackedTextViews.Remove(pView);
            }

            return VSConstants.S_OK;
        }

        private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
            => StartComputeModelTask();

        // On caret position change, highlight the corresponding symbol node
        private void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            if (!e.NewPosition.Equals(e.OldPosition))
                StartHightlightNodeTask();
        }

        private void ExpandAll(object sender, RoutedEventArgs e)
        {
            DocumentOutlineHelper.SetIsExpanded((IEnumerable<DocumentSymbolItem>)symbolTree.ItemsSource, true);
        }

        private void CollapseAll(object sender, RoutedEventArgs e)
        {
            DocumentOutlineHelper.SetIsExpanded((IEnumerable<DocumentSymbolItem>)symbolTree.ItemsSource, false);
        }

        private void Search(object sender, EventArgs e)
        {
            StartUpdateUITask();
        }

        private void SortByName(object sender, EventArgs e)
        {
            SortOption = SortOption.Name;
            StartUpdateUITask();
        }

        private void SortByOrder(object sender, EventArgs e)
        {
            SortOption = SortOption.Order;
            StartUpdateUITask();
        }

        private void SortByType(object sender, EventArgs e)
        {
            SortOption = SortOption.Type;
            StartUpdateUITask();
        }

        // When symbol node clicked, select the corresponding code
        private void JumpToContent(object sender, EventArgs e)
        {
            if (sender is StackPanel panel && panel.DataContext is DocumentSymbolItem symbol)
                StartJumpToContent(symbol);
        }
    }
}
