// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    internal sealed partial class EventHookupSessionManager : ForegroundThreadAffinitizedObject
    {
        private readonly IHACK_EventHookupDismissalOnBufferChangePreventerService _prematureDismissalPreventer;
        private readonly IQuickInfoBroker _quickInfoBroker;

        internal EventHookupSession CurrentSession { get; set; }
        internal IQuickInfoSession QuickInfoSession { get; set; }

        // For test purposes only!
        internal FrameworkElement TEST_MostRecentQuickInfoContent { get; set; }

        internal EventHookupSessionManager(IHACK_EventHookupDismissalOnBufferChangePreventerService prematureDismissalPreventer, IQuickInfoBroker quickInfoBroker)
        {
            _prematureDismissalPreventer = prematureDismissalPreventer;
            _quickInfoBroker = quickInfoBroker;
        }

        internal void EventHookupFoundInSession(EventHookupSession analyzedSession)
        {
            AssertIsForeground();

            var caretPoint = analyzedSession.TextView.GetCaretPoint(analyzedSession.SubjectBuffer);

            // Ensure the analyzed session matches the current session and that the caret is still
            // in the session's tracking span.
            if (CurrentSession == analyzedSession &&
                QuickInfoSession == null &&
                caretPoint.HasValue &&
                analyzedSession.TrackingSpan.GetSpan(CurrentSession.TextView.TextSnapshot).Contains(caretPoint.Value))
            {
                QuickInfoSession = _quickInfoBroker.CreateQuickInfoSession(analyzedSession.TextView,
                    analyzedSession.TrackingPoint,
                    trackMouse: false);

                // Special indicator that this quick info session was created by event hookup,
                // which is used when deciding whether and how to display the session
                QuickInfoSession.Properties.AddProperty(typeof(EventHookupSessionManager), this);
                QuickInfoSession.Properties.AddProperty(QuickInfoUtilities.EventHookupKey, "EventHookup");

                // Watch all text buffer changes & caret moves while this quick info session is 
                // active
                analyzedSession.TextView.TextSnapshot.TextBuffer.Changed += TextBuffer_Changed;
                CurrentSession.Dismissed += () => { analyzedSession.TextView.TextSnapshot.TextBuffer.Changed -= TextBuffer_Changed; };

                analyzedSession.TextView.Caret.PositionChanged += Caret_PositionChanged;
                CurrentSession.Dismissed += () => { analyzedSession.TextView.Caret.PositionChanged -= Caret_PositionChanged; };

                QuickInfoSession.Start();

                // HACK! Workaround for VS dismissing quick info sessions on buffer changed events. 
                // This must happen after the QuickInfoSession is started.
                if (_prematureDismissalPreventer != null)
                {
                    _prematureDismissalPreventer.HACK_EnsureQuickInfoSessionNotDismissedPrematurely(analyzedSession.TextView);
                    QuickInfoSession.Dismissed += (s, e) => { _prematureDismissalPreventer.HACK_OnQuickInfoSessionDismissed(analyzedSession.TextView); };
                }
            }
        }

        internal void BeginSession(
            EventHookupCommandHandler eventHookupCommandHandler,
            ITextView textView,
            ITextBuffer subjectBuffer,
            AggregateAsynchronousOperationListener asyncListener,
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

            if (QuickInfoSession != null)
            {
                QuickInfoSession.Dismiss();
                QuickInfoSession = null;
                TEST_MostRecentQuickInfoContent = null;
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
            return CurrentSession != null && QuickInfoSession != null;
        }
    }
}
