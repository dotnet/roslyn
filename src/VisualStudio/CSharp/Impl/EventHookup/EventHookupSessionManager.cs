// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    internal sealed partial class EventHookupSessionManager : ForegroundThreadAffinitizedObject
    {
        internal EventHookupSession CurrentSession { get; set; }

        // For test purposes only!
        internal FrameworkElement TEST_MostRecentQuickInfoContent { get; set; }

        internal EventHookupSessionManager()
        {
        }

        internal void EventHookupFoundInSession(EventHookupSession analyzedSession)
        {
            AssertIsForeground();

            var caretPoint = analyzedSession.TextView.GetCaretPoint(analyzedSession.SubjectBuffer);

            // Ensure the analyzed session matches the current session and that the caret is still
            // in the session's tracking span.
            if (CurrentSession == analyzedSession &&
                caretPoint.HasValue &&
                analyzedSession.TrackingSpan.GetSpan(CurrentSession.TextView.TextSnapshot).Contains(caretPoint.Value))
            {
                // Watch all text buffer changes & caret moves while this quick info session is
                // active
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
