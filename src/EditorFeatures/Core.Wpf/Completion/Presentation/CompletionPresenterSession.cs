// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using RoslynCompletion = Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    internal sealed class CompletionPresenterSession : ForegroundThreadAffinitizedObject, ICompletionPresenterSession
    {
        /// <summary>
        /// Used to allow us to stash away the original ITextSnapshot to an ICompletionSession
        /// when we make it.  We can use this later to recover the original Document and provide
        /// completion descriptions.
        /// </summary>
        public static readonly object TextSnapshotKey = new object();

        internal static readonly object Key = new object();

        private readonly ICompletionBroker _completionBroker;
        internal readonly IGlyphService GlyphService;

        private readonly ITextView _textView;

        public event EventHandler<EventArgs> Dismissed;
        public event EventHandler<CompletionItemEventArgs> ItemCommitted;
        public event EventHandler<CompletionItemEventArgs> ItemSelected;
        public event EventHandler<CompletionItemFilterStateChangedEventArgs> FilterStateChanged;

        private readonly RoslynCompletionSet _completionSet;

        private ICompletionSession _editorSessionOpt;
        private bool _ignoreSelectionStatusChangedEvent;

        // Now that PresentItemsInternal is called after a delay, editorSessionOpt may be null because
        // it hasn't been shown yet, or because it was already dismissed.  Use isDismissed to track
        // this scenario.
        private bool _isDismissed;

        public ITextBuffer SubjectBuffer { get; }

        /// <summary>
        /// this cancellation is used to log whether presentation is
        /// actually shown to users or not
        /// </summary>
        private readonly CancellationTokenSource _trackLogSession;
        private IDisposable _logger;

        public CompletionPresenterSession(
            IThreadingContext threadingContext,
            ICompletionBroker completionBroker,
            IGlyphService glyphService,
            ITextView textView,
            ITextBuffer subjectBuffer)
            : base(threadingContext)
        {
            _completionBroker = completionBroker;
            this.GlyphService = glyphService;
            _textView = textView;
            SubjectBuffer = subjectBuffer;

            _trackLogSession = new CancellationTokenSource();
            _logger = Logger.LogBlock(FunctionId.Intellisense_Completion,
                KeyValueLogMessage.Create(LogType.UserAction),
                _trackLogSession.Token);

            _completionSet = new RoslynCompletionSet(this, textView, subjectBuffer);
            _completionSet.SelectionStatusChanged += OnCompletionSetSelectionStatusChanged;
        }

        public void PresentItems(
            ITextSnapshot textSnapshot,
            ITrackingSpan triggerSpan,
            IList<RoslynCompletion.CompletionItem> completionItems,
            RoslynCompletion.CompletionItem selectedItem,
            RoslynCompletion.CompletionItem suggestionModeItem,
            bool suggestionMode,
            bool isSoftSelected,
            ImmutableArray<CompletionItemFilter> completionItemFilters,
            string filterText)
        {
            AssertIsForeground();

            // check if this update is still relevant
            if (_textView.IsClosed || _isDismissed)
            {
                return;
            }

            if (triggerSpan != null)
            {
                _completionSet.SetTrackingSpan(triggerSpan);
            }

            _ignoreSelectionStatusChangedEvent = true;
            try
            {
                _completionSet.SetCompletionItems(
                    completionItems, selectedItem, suggestionModeItem, suggestionMode,
                    isSoftSelected, completionItemFilters, filterText);
            }
            finally
            {
                _ignoreSelectionStatusChangedEvent = false;
            }

            if (_editorSessionOpt == null)
            {
                // We're tracking the caret.  Don't have the editor do it. 
                // Map the span instead of a point to avoid affinity problems.
                _editorSessionOpt = _completionBroker.CreateCompletionSession(
                    _textView,
                    triggerSpan.GetStartTrackingPoint(PointTrackingMode.Negative),
                    trackCaret: false);

                if (_textView is IDebuggerTextView debugTextView && !debugTextView.IsImmediateWindow)
                {
                    debugTextView.HACK_StartCompletionSession(_editorSessionOpt);
                }

                _editorSessionOpt.Dismissed += (s, e) => OnEditorSessionDismissed();

                // So here's the deal.  We cannot create the editor session and give it the right
                // items (even though we know what they are).  Instead, the session will call
                // back into the ICompletionSourceProvider (which is us) to get those values. It
                // will pass itself along with the calls back into ICompletionSourceProvider.
                // So, in order to make that connection work, we add properties to the session
                // so that we can call back into ourselves, get the items and add it to the
                // session.
                _editorSessionOpt.Properties.AddProperty(Key, this);
                _editorSessionOpt.Properties.AddProperty(TextSnapshotKey, textSnapshot);
                _editorSessionOpt.Start();
            }

            // Call so that the editor will refresh the completion text to embolden.
            _editorSessionOpt?.Match();
        }

        private void OnEditorSessionDismissed()
        {
            AssertIsForeground();
            this.Dismissed?.Invoke(this, new EventArgs());
        }

        internal void OnCompletionItemCommitted(RoslynCompletion.CompletionItem completionItem)
        {
            AssertIsForeground();
            this.ItemCommitted?.Invoke(this, new CompletionItemEventArgs(completionItem));
        }

        private void OnCompletionSetSelectionStatusChanged(
            object sender, ValueChangedEventArgs<CompletionSelectionStatus> eventArgs)
        {
            AssertIsForeground();

            if (_ignoreSelectionStatusChangedEvent || _textView.IsClosed)
            {
                return;
            }

            var item = _completionSet.GetCompletionItem(eventArgs.NewValue.Completion);
            if (item != null)
            {
                this.ItemSelected?.Invoke(this, new CompletionItemEventArgs(item));
            }
        }

        internal void AugmentCompletionSession(IList<CompletionSet> completionSets)
        {
            Contract.ThrowIfTrue(completionSets.Contains(_completionSet));
            completionSets.Add(_completionSet);
        }

        internal void OnIntelliSenseFiltersChanged(ImmutableDictionary<CompletionItemFilter, bool> filterStates)
        {
            this.FilterStateChanged?.Invoke(this,
                new CompletionItemFilterStateChangedEventArgs(filterStates));
        }

        public void Dismiss()
        {
            AssertIsForeground();

            // we need to distinguish a case where completion UI is shown to users and then dismissed
            // or it got dismissed before UI is shown to users in telemetry events.
            // we use cancellation for it. here, we raise cancellation for trackLogSession, and then
            // call ReportPerformance. if UI is already shown, then it will become noop. if it didn't yet,
            // then event will be fired with cancellation on.
            _trackLogSession.Cancel();

            ReportPerformance();

            _isDismissed = true;
            if (_editorSessionOpt == null)
            {
                // No editor session, nothing to do here.
                return;
            }

            _editorSessionOpt.Dismiss();
            _editorSessionOpt = null;
        }

        private bool ExecuteKeyboardCommand(IntellisenseKeyboardCommand command)
        {
            var target = _editorSessionOpt != null
                ? _editorSessionOpt.Presenter as IIntellisenseCommandTarget
                : null;

            return target != null && target.ExecuteKeyboardCommand(command);
        }

        public void SelectPreviousItem()
        {
            ExecuteKeyboardCommand(IntellisenseKeyboardCommand.Up);
        }

        public void SelectNextItem()
        {
            ExecuteKeyboardCommand(IntellisenseKeyboardCommand.Down);
        }

        public void SelectPreviousPageItem()
        {
            ExecuteKeyboardCommand(IntellisenseKeyboardCommand.PageUp);
        }

        public void SelectNextPageItem()
        {
            ExecuteKeyboardCommand(IntellisenseKeyboardCommand.PageDown);
        }

        public void ReportPerformance()
        {
            // we only report once. after that, this becomes noop
            _logger?.Dispose();
            _logger = null;
        }
    }
}
