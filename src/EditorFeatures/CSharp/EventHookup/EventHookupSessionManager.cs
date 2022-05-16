// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    [Export]
    internal sealed partial class EventHookupSessionManager
    {
        public readonly IThreadingContext ThreadingContext;

        private readonly IToolTipService _toolTipService;
        private IToolTipPresenter _toolTipPresenter;

        internal EventHookupSession CurrentSession { get; set; }

        // For test purposes only!
        internal ClassifiedTextElement[] TEST_MostRecentToolTipContent { get; set; }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EventHookupSessionManager(IThreadingContext threadingContext, IToolTipService toolTipService)
        {
            ThreadingContext = threadingContext;
            _toolTipService = toolTipService;
        }

        internal void EventHookupFoundInSession(EventHookupSession analyzedSession)
        {
            ThreadingContext.ThrowIfNotOnUIThread();

            var caretPoint = analyzedSession.TextView.GetCaretPoint(analyzedSession.SubjectBuffer);

            // only generate tooltip if it is not already shown (_toolTipPresenter == null)
            // Ensure the analyzed session matches the current session and that the caret is still
            // in the session's tracking span.
            if (_toolTipPresenter == null &&
                CurrentSession == analyzedSession &&
                caretPoint.HasValue &&
                IsCaretWithinSpanOrAtEnd(analyzedSession.TrackingSpan, analyzedSession.TextView.TextSnapshot, caretPoint.Value))
            {
                // Create a tooltip presenter that stays alive, even when the user types, without tracking the mouse.
                _toolTipPresenter = _toolTipService.CreatePresenter(analyzedSession.TextView,
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

        private static bool IsCaretWithinSpanOrAtEnd(ITrackingSpan trackingSpan, ITextSnapshot textSnapshot, SnapshotPoint caretPoint)
        {
            var snapshotSpan = trackingSpan.GetSpan(textSnapshot);

            // If the caret is within the span, then we want to show the tooltip
            if (snapshotSpan.Contains(caretPoint))
            {
                return true;
            }

            // Otherwise if the span is empty, and at the end of the file, and the caret
            // is also at the end of the file, then show the tooltip.
            if (snapshotSpan.IsEmpty &&
                snapshotSpan.Start.Position == caretPoint.Position &&
                caretPoint.Position == textSnapshot.Length)
            {
                return true;
            }

            return false;
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
            ThreadingContext.ThrowIfNotOnUIThread();

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
            ThreadingContext.ThrowIfNotOnUIThread();

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
            ThreadingContext.ThrowIfNotOnUIThread();

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
            => CurrentSession != null;
    }
}
