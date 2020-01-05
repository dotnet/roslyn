// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    [Export]
    internal sealed partial class EventHookupSessionManager : ForegroundThreadAffinitizedObject
    {
        private readonly IToolTipService _toolTipService;
        private IToolTipPresenter _toolTipPresenter;

        internal EventHookupSession CurrentSession { get; set; }

        // For test purposes only!
        internal ClassifiedTextElement[] TEST_MostRecentToolTipContent { get; set; }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EventHookupSessionManager(IThreadingContext threadingContext, IToolTipService toolTipService)
            : base(threadingContext)
        {
            _toolTipService = toolTipService;
        }

        internal void EventHookupFoundInSession(EventHookupSession analyzedSession)
        {
            AssertIsForeground();

            var caretPoint = analyzedSession.TextView.GetCaretPoint(analyzedSession.SubjectBuffer);

            // only generate tooltip if it is not already shown (_toolTipPresenter == null)
            // Ensure the analyzed session matches the current session and that the caret is still
            // in the session's tracking span.
            if (_toolTipPresenter == null &&
                CurrentSession == analyzedSession &&
                caretPoint.HasValue &&
                analyzedSession.TrackingSpan.GetSpan(CurrentSession.TextView.TextSnapshot).Contains(caretPoint.Value))
            {
                // Create a tooltip presenter that stays alive, even when the user types, without tracking the mouse.
                _toolTipPresenter = this._toolTipService.CreatePresenter(analyzedSession.TextView,
                    new ToolTipParameters(trackMouse: false, ignoreBufferChange: true));

                // tooltips text is: Program_MyEvents;      (Press TAB to insert)
                // GetEventNameTask() gets back the event name, only needs to add a semicolon after it.
                var textRuns = new[]
                {
                    new ClassifiedTextRun(ClassificationTypeNames.MethodName, analyzedSession.GetEventNameTask.Result, ClassifiedTextRunStyle.UseClassificationFont),
                    new ClassifiedTextRun(ClassificationTypeNames.Punctuation, ";", ClassifiedTextRunStyle.UseClassificationFont),
                    new ClassifiedTextRun(ClassificationTypeNames.Text, CSharpEditorResources.Press_TAB_to_insert),
                };
                var content = new[] { new ClassifiedTextElement(textRuns) };

                _toolTipPresenter.StartOrUpdate(analyzedSession.TrackingSpan, content);

                // For test purposes only!
                TEST_MostRecentToolTipContent = content;

                // Watch all text buffer changes & caret moves while this event hookup session is active
                analyzedSession.TextView.TextSnapshot.TextBuffer.Changed += TextBuffer_Changed;
                CurrentSession.Dismissed += () => { analyzedSession.TextView.TextSnapshot.TextBuffer.Changed -= TextBuffer_Changed; };

                analyzedSession.TextView.Caret.PositionChanged += Caret_PositionChanged;
                CurrentSession.Dismissed += () => { analyzedSession.TextView.Caret.PositionChanged -= Caret_PositionChanged; };
            }
        }

        internal void BeginSession(
            EventHookupCommandHandler eventHookupCommandHandler,
            ITextView textView,
            ITextBuffer subjectBuffer,
            IAsynchronousOperationListener asyncListener,
            Mutex testSessionHookupMutex)
        {
            CurrentSession = new EventHookupSession(this, eventHookupCommandHandler, textView, subjectBuffer, asyncListener, testSessionHookupMutex);
        }

        internal void CancelAndDismissExistingSessions()
        {
            AssertIsForeground();

            if (CurrentSession != null)
            {
                CurrentSession.Cancel();
                CurrentSession = null;
            }

            if (_toolTipPresenter != null)
            {
                _toolTipPresenter.Dismiss();
                _toolTipPresenter = null;
            }

            // For test purposes only!
            TEST_MostRecentToolTipContent = null;
        }

        /// <summary>
        /// If any text is deleted or any non-space text is entered, cancel the session.
        /// </summary>
        private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            AssertIsForeground();

            foreach (var change in e.Changes)
            {
                if (change.OldText.Length > 0 || change.NewText.Any(c => c != ' '))
                {
                    CancelAndDismissExistingSessions();
                    return;
                }
            }
        }

        /// <summary>
        /// If the caret moves outside the session's tracking span, cancel the session.
        /// </summary>
        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            AssertIsForeground();

            if (CurrentSession == null)
            {
                CancelAndDismissExistingSessions();
                return;
            }

            var caretPoint = CurrentSession.TextView.GetCaretPoint(CurrentSession.SubjectBuffer);

            if (!caretPoint.HasValue)
            {
                CancelAndDismissExistingSessions();
            }

            var snapshotSpan = CurrentSession.TrackingSpan.GetSpan(CurrentSession.TextView.TextSnapshot);
            if (snapshotSpan.Snapshot != caretPoint.Value.Snapshot || !snapshotSpan.Contains(caretPoint.Value))
            {
                CancelAndDismissExistingSessions();
            }
        }

        internal bool IsTrackingSession()
        {
            return CurrentSession != null;
        }
    }
}
