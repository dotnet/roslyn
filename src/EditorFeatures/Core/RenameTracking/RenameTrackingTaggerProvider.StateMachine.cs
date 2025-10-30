// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;

internal sealed partial class RenameTrackingTaggerProvider
{
    /// <summary>
    /// Keeps track of the rename tracking state for a given text buffer by tracking its
    /// changes over time.
    /// </summary>
    private sealed class StateMachine
    {
        public readonly IThreadingContext ThreadingContext;

        private readonly IInlineRenameService _inlineRenameService;
        private readonly IAsynchronousOperationListener _asyncListener;

        // Store committed sessions so they can be restored on undo/redo. The undo transactions
        // may live beyond the lifetime of the buffer tracked by this StateMachine, so storing
        // them here allows them to be correctly cleaned up when the buffer goes away.
        private readonly IList<TrackingSession> _committedSessions = [];

        private int _refCount;

        public readonly IGlobalOptionService GlobalOptions;
        public TrackingSession TrackingSession { get; private set; }
        public ITextBuffer Buffer { get; }

        public event Action TrackingSessionUpdated = delegate { };
        public event Action<ITrackingSpan> TrackingSessionCleared = delegate { };

        public StateMachine(
            IThreadingContext threadingContext,
            ITextBuffer buffer,
            IInlineRenameService inlineRenameService,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListener asyncListener)
        {
            ThreadingContext = threadingContext;
            Buffer = buffer;
            Buffer.Changed += Buffer_Changed;
            _inlineRenameService = inlineRenameService;
            _asyncListener = asyncListener;
            GlobalOptions = globalOptions;
        }

        private void Buffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            ThreadingContext.ThrowIfNotOnUIThread();

            if (!GlobalOptions.GetOption(RenameTrackingOptionsStorage.RenameTracking))
            {
                // When disabled, ignore all text buffer changes and do not trigger retagging
                return;
            }

            using (Logger.LogBlock(FunctionId.Rename_Tracking_BufferChanged, CancellationToken.None))
            {
                // When the buffer changes, several things might be happening:
                // 1. If a non-identifier character has been added or deleted, we stop tracking
                //    completely.
                // 2. Otherwise, if the changes are completely contained an existing session, then
                //    continue that session.
                // 3. Otherwise, we're starting a new tracking session. Find and track the span of
                //    the relevant word in the foreground, and use a task to figure out whether the
                //    original word was a renameable identifier or not.

                if (e.Changes.Count != 1 || ShouldClearTrackingSession(e.Changes.Single()))
                {
                    ClearTrackingSession();
                    return;
                }

                // The change is trackable. Figure out whether we should continue an existing
                // session

                var change = e.Changes.Single();

                if (this.TrackingSession == null)
                {
                    StartTrackingSession(e);
                    return;
                }

                // There's an existing session. Continue that session if the current change is
                // contained inside the tracking span.

                var trackingSpanInNewSnapshot = this.TrackingSession.TrackingSpan.GetSpan(e.After);
                if (trackingSpanInNewSnapshot.Contains(change.NewSpan))
                {
                    // Continuing an existing tracking session. If there may have been a tag
                    // showing, then update the tags.
                    UpdateTrackingSessionIfRenamable();
                }
                else
                {
                    StartTrackingSession(e);
                }
            }
        }

        public void UpdateTrackingSessionIfRenamable()
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            if (this.TrackingSession.IsDefinitelyRenamableIdentifierFastCheck())
            {
                this.TrackingSession.CheckNewIdentifier(this, Buffer.CurrentSnapshot);
                TrackingSessionUpdated();
            }
        }

        private bool ShouldClearTrackingSession(ITextChange change)
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            if (!TryGetSyntaxFactsService(out var syntaxFactsService))
            {
                return true;
            }

            // The editor will replace virtual space with spaces and/or tabs when typing on a 
            // previously blank line. Trim these characters from the start of change.NewText. If 
            // the resulting change is empty (the user just typed a <space>), clear the session.
            var changedText = change.OldText + change.NewText.TrimStart(' ', '\t');
            if (changedText.IsEmpty())
            {
                return true;
            }

            return changedText.Any(c => !IsTrackableCharacter(syntaxFactsService, c));
        }

        private void StartTrackingSession(TextContentChangedEventArgs eventArgs)
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            ClearTrackingSession();

            if (_inlineRenameService.ActiveSession != null)
            {
                return;
            }

            // Synchronously find the tracking span in the old document.

            var change = eventArgs.Changes.Single();
            var beforeText = eventArgs.Before.AsText();
            if (!TryGetSyntaxFactsService(out var syntaxFactsService))
            {
                return;
            }

            var leftSidePosition = change.OldPosition;
            var rightSidePosition = change.OldPosition + change.OldText.Length;

            while (leftSidePosition > 0 && IsTrackableCharacter(syntaxFactsService, beforeText[leftSidePosition - 1]))
            {
                leftSidePosition--;
            }

            while (rightSidePosition < beforeText.Length && IsTrackableCharacter(syntaxFactsService, beforeText[rightSidePosition]))
            {
                rightSidePosition++;
            }

            var originalSpan = new Span(leftSidePosition, rightSidePosition - leftSidePosition);
            this.TrackingSession = new TrackingSession(this, new SnapshotSpan(eventArgs.Before, originalSpan), _asyncListener);
        }

        private static bool IsTrackableCharacter(ISyntaxFactsService syntaxFactsService, char c)
        {
            // Allow identifier part characters at the beginning of strings (even if they are
            // not identifier start characters). If an intermediate name is not valid, the smart
            // tag will not be shown due to later checks. Also allow escape chars anywhere as
            // they might be in the middle of a complex edit.
            return syntaxFactsService.IsIdentifierPartCharacter(c) || syntaxFactsService.IsIdentifierEscapeCharacter(c);
        }

        public bool ClearTrackingSession()
        {
            ThreadingContext.ThrowIfNotOnUIThread();

            if (this.TrackingSession != null)
            {
                // Disallow the existing TrackingSession from triggering IdentifierFound.
                var previousTrackingSession = this.TrackingSession;
                this.TrackingSession = null;

                previousTrackingSession.Cancel();

                // If there may have been a tag showing, then actually clear the tags.
                if (previousTrackingSession.IsDefinitelyRenamableIdentifierFastCheck())
                {
                    TrackingSessionCleared(previousTrackingSession.TrackingSpan);
                }

                return true;
            }

            return false;
        }

        public bool ClearVisibleTrackingSession()
        {
            ThreadingContext.ThrowIfNotOnUIThread();

            if (this.TrackingSession != null && this.TrackingSession.IsDefinitelyRenamableIdentifierFastCheck())
            {
                var document = Buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document != null)
                {
                    // When rename tracking is dismissed via escape, we no longer wish to
                    // provide a diagnostic/codefix, but nothing has changed in the workspace
                    // to trigger the diagnostic system to reanalyze, so we trigger it 
                    // manually.
                    var service = document.Project.Solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
                    service.RequestDiagnosticRefresh();
                }

                // Disallow the existing TrackingSession from triggering IdentifierFound.
                var previousTrackingSession = this.TrackingSession;
                this.TrackingSession = null;

                previousTrackingSession.Cancel();
                TrackingSessionCleared(previousTrackingSession.TrackingSpan);
                return true;
            }

            return false;
        }

        internal int StoreCurrentTrackingSessionAndGenerateId()
        {
            ThreadingContext.ThrowIfNotOnUIThread();

            var existingIndex = _committedSessions.IndexOf(TrackingSession);
            if (existingIndex >= 0)
            {
                return existingIndex;
            }

            var index = _committedSessions.Count;
            _committedSessions.Insert(index, TrackingSession);
            return index;
        }

        public bool CanInvokeRename(
            [NotNullWhen(true)] out TrackingSession trackingSession,
            bool isSmartTagCheck = false)
        {
            // This needs to be able to run on a background thread for the diagnostic.

            trackingSession = this.TrackingSession;
            if (trackingSession == null)
                return false;

            return TryGetSyntaxFactsService(out var syntaxFactsService) && TryGetLanguageHeuristicsService(out var languageHeuristicsService) &&
                trackingSession.CanInvokeRename(syntaxFactsService, languageHeuristicsService, isSmartTagCheck);
        }

        internal (CodeAction action, TextSpan renameSpan) TryGetCodeAction(
            Document document,
            SourceText text,
            TextSpan userSpan,
            IEnumerable<IRefactorNotifyService> refactorNotifyServices,
            ITextUndoHistoryRegistry undoHistoryRegistry)
        {
            try
            {
                // This can be called on a background thread. We are being asked whether a 
                // lightbulb should be shown for the given document, but we only know about the 
                // current state of the buffer. Compare the text to see if we should bail early.
                // Even if the text is the same, the buffer may change on the UI thread during this
                // method. If it does, we may give an incorrect response, but the diagnostics 
                // engine will know that the document changed and not display the lightbulb anyway.

                if (Buffer.AsTextContainer().CurrentText == text &&
                    CanInvokeRename(out var trackingSession))
                {
                    var snapshotSpan = trackingSession.TrackingSpan.GetSpan(Buffer.CurrentSnapshot);

                    // user needs to be on the same line as the diagnostic location.
                    if (text.AreOnSameLine(userSpan.Start, snapshotSpan.Start))
                    {
                        var title = string.Format(
                            WorkspacesResources.Rename_0_to_1,
                            trackingSession.OriginalName,
                            snapshotSpan.GetText());

                        return (new RenameTrackingCodeAction(ThreadingContext, document, title, refactorNotifyServices, undoHistoryRegistry, GlobalOptions),
                                snapshotSpan.Span.ToTextSpan());
                    }
                }

                return default;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        public void RestoreTrackingSession(int trackingSessionId)
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            ClearTrackingSession();

            this.TrackingSession = _committedSessions[trackingSessionId];
            TrackingSessionUpdated();
        }

        public void OnTrackingSessionUpdated(TrackingSession trackingSession)
        {
            ThreadingContext.ThrowIfNotOnUIThread();

            if (this.TrackingSession == trackingSession)
            {
                TrackingSessionUpdated();
            }
        }

        private bool TryGetSyntaxFactsService(out ISyntaxFactsService syntaxFactsService)
        {
            // Can be called on a background thread

            syntaxFactsService = null;
            var document = Buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            }

            return syntaxFactsService != null;
        }

        private bool TryGetLanguageHeuristicsService(out IRenameTrackingLanguageHeuristicsService languageHeuristicsService)
        {
            // Can be called on a background thread

            languageHeuristicsService = null;
            var document = Buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                languageHeuristicsService = document.GetLanguageService<IRenameTrackingLanguageHeuristicsService>();
            }

            return languageHeuristicsService != null;
        }

        public void Connect()
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            _refCount++;
        }

        public void Disconnect()
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            _refCount--;
            Contract.ThrowIfFalse(_refCount >= 0);

            if (_refCount == 0)
            {
                this.Buffer.Properties.RemoveProperty(typeof(StateMachine));
                this.Buffer.Changed -= Buffer_Changed;
            }
        }
    }
}
