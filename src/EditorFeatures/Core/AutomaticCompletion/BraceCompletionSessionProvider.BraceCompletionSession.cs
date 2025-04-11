// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.AutomaticCompletion;

internal partial class BraceCompletionSessionProvider
{
    // ported and modified from "Platform\Text\Impl\BraceCompletion\BraceCompletionDefaultSession.cs"
    //
    // we want to provide better context based brace completion but IBraceCompletionContext is too simple for that.
    // fortunately, editor provides another extension point where we have more control over brace completion but we do not
    // want to re-implement logics base session provider already provides. so I ported editor's default session and 
    // modified it little bit so that we can use it as base class.
    private sealed class BraceCompletionSession(
        BraceCompletionSessionProvider provider,
        ITextView textView,
        ITextBuffer subjectBuffer,
        SnapshotPoint openingPoint,
        char openingBrace,
        char closingBrace,
        ITextUndoHistory undoHistory,
        IBraceCompletionService service,
        bool responsiveCompletion) : IBraceCompletionSession
    {
        private readonly BraceCompletionSessionProvider _provider = provider;

        private readonly IEditorOperations _editorOperations = provider._editorOperationsFactoryService.GetEditorOperations(textView);
        private readonly IBraceCompletionService _service = service;
        private readonly ITextUndoHistory _undoHistory = undoHistory;
        private readonly bool _responsiveCompletion = responsiveCompletion;

        private IThreadingContext ThreadingContext => _provider._threadingContext;
        private EditorOptionsService EditorOptionsService => _provider._editorOptionsService;

        public char OpeningBrace { get; } = openingBrace;
        public char ClosingBrace { get; } = closingBrace;

        public ITrackingPoint OpeningPoint { get; private set; }
        public ITrackingPoint ClosingPoint { get; private set; } = subjectBuffer.CurrentSnapshot.CreateTrackingPoint(openingPoint.Position, PointTrackingMode.Positive);

        public ITextBuffer SubjectBuffer { get; } = subjectBuffer;
        public ITextView TextView { get; } = textView;

        #region IBraceCompletionSession Methods

        /// <summary>
        /// Create a fresh cancellation token for a particular step of brace matching we want to perform.  We create
        /// fresh instances as brace-completion is a long running operation, that interacts with the user as they are
        /// typing.  As such, the overall time is unbounded in length (as the user may just pause and think for a
        /// while).  So we don't want to cap the *overall* time that brace completion is active, just the time
        /// particular operations take so that the user does not experience UI delays.
        /// </summary>
        private CancellationToken GetFreshCancellationToken()
            => GetCancellationToken(_responsiveCompletion);

        public void Start()
        {
            ThreadingContext.ThrowIfNotOnUIThread();

            try
            {
                var success = ThreadingContext.JoinableTaskFactory.Run(() => TryStartAsync(GetFreshCancellationToken()));
                if (!success)
                    EndSession();
            }
            catch (OperationCanceledException)
            {
                EndSession();
            }
        }

        private async Task<bool> TryStartAsync(CancellationToken cancellationToken)
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            cancellationToken.ThrowIfCancellationRequested();
            var closingSnapshotPoint = ClosingPoint.GetPoint(SubjectBuffer.CurrentSnapshot);

            if (closingSnapshotPoint.Position < 1)
            {
                Debug.Fail("The closing point was not found at the expected position.");
                return false;
            }

            var openingSnapshotPoint = closingSnapshotPoint.Subtract(1);

            if (openingSnapshotPoint.GetChar() != OpeningBrace)
            {
                // there is a bug in editor brace completion engine on projection buffer that already fixed in vs_pro. until that is FIed to use
                // I will make this not to assert
                // Debug.Fail("The opening brace was not found at the expected position.");
                return false;
            }

            OpeningPoint = SubjectBuffer.CurrentSnapshot.CreateTrackingPoint(openingSnapshotPoint, PointTrackingMode.Positive);

            var document = SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return false;

            var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);
            var context = GetBraceCompletionContext(parsedDocument, document.Project.GetFallbackAnalyzerOptions());

            // Note: completes synchronously unless Semantic Model is needed to determine the result:
            var hasBraceCompletions = await _service.HasBraceCompletionAsync(context, document, cancellationToken).ConfigureAwait(true);
            if (!hasBraceCompletions)
                return false;

            var braceResult = _service.GetBraceCompletion(context);

            using var caretPreservingTransaction = new CaretPreservingEditTransaction(EditorFeaturesResources.Brace_Completion, _undoHistory, _editorOperations);

            // Apply the change to complete the brace.
            ApplyBraceCompletionResult(braceResult);

            // switch the closing point from positive to negative tracking so that the closing point stays against the closing brace
            ClosingPoint = SubjectBuffer.CurrentSnapshot.CreateTrackingPoint(ClosingPoint.GetPoint(SubjectBuffer.CurrentSnapshot), PointTrackingMode.Negative);

            if (TryGetBraceCompletionContext(out var contextAfterStart, cancellationToken))
            {
                var indentationOptions = SubjectBuffer.GetIndentationOptions(EditorOptionsService, document.Project.GetFallbackAnalyzerOptions(), contextAfterStart.Document.LanguageServices, explicitFormat: false);
                var changesAfterStart = _service.GetTextChangesAfterCompletion(contextAfterStart, indentationOptions, cancellationToken);
                if (changesAfterStart != null)
                {
                    ApplyBraceCompletionResult(changesAfterStart.Value);
                }
            }

            caretPreservingTransaction.Complete();
            return true;
        }

        public void PreBackspace(out bool handledCommand)
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            handledCommand = false;

            var caretPos = this.GetCaretPosition();
            var snapshot = SubjectBuffer.CurrentSnapshot;

            if (caretPos.HasValue && caretPos.Value.Position > 0 && (caretPos.Value.Position - 1) == OpeningPoint.GetPoint(snapshot).Position
                && !HasForwardTyping)
            {
                using var undo = CreateUndoTransaction();
                using var edit = SubjectBuffer.CreateEdit();

                var span = new SnapshotSpan(OpeningPoint.GetPoint(snapshot), ClosingPoint.GetPoint(snapshot));

                edit.Delete(span);

                if (edit.HasFailedChanges)
                {
                    edit.Cancel();
                    undo.Cancel();
                    Debug.Fail("Unable to clear braces");
                }
                else
                {
                    // handle the command so the backspace does 
                    // not go through since we've already cleared the braces
                    handledCommand = true;
                    edit.ApplyAndLogExceptions();
                    undo.Complete();
                    EndSession();
                }
            }
        }

        public void PostBackspace()
        {
        }

        public void PreOverType(out bool handledCommand)
        {
            handledCommand = false;
            try
            {
                PreOverTypeWorker(out handledCommand, GetFreshCancellationToken());
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void PreOverTypeWorker(out bool handledCommand, CancellationToken cancellationToken)
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            handledCommand = false;
            if (ClosingPoint == null)
            {
                return;
            }

            var snapshot = this.SubjectBuffer.CurrentSnapshot;

            var closingSnapshotPoint = ClosingPoint.GetPoint(snapshot);

            if (HasForwardTyping)
            {
                return;
            }

            if (!TryGetBraceCompletionContext(out var context, cancellationToken) ||
                !_service.AllowOverType(context, cancellationToken))
            {
                return;
            }

            var caretPos = this.GetCaretPosition();

            Debug.Assert(caretPos.HasValue && caretPos.Value.Position < closingSnapshotPoint.Position);

            // ensure that we are within the session before clearing
            if (caretPos.HasValue && caretPos.Value.Position < closingSnapshotPoint.Position && closingSnapshotPoint.Position > 0)
            {
                using var undo = CreateUndoTransaction();

                _editorOperations.AddBeforeTextBufferChangePrimitive();

                var span = new SnapshotSpan(caretPos.Value, closingSnapshotPoint.Subtract(1));

                using var edit = SubjectBuffer.CreateEdit();

                edit.Delete(span);

                if (edit.HasFailedChanges)
                {
                    Debug.Fail("Unable to clear closing brace");
                    edit.Cancel();
                    undo.Cancel();
                }
                else
                {
                    handledCommand = true;

                    edit.ApplyAndLogExceptions();

                    MoveCaretToClosingPoint();

                    _editorOperations.AddAfterTextBufferChangePrimitive();

                    undo.Complete();
                }
            }
        }

        public void PostOverType()
        {
        }

        public void PreTab(out bool handledCommand)
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            handledCommand = false;

            if (!HasForwardTyping)
            {
                handledCommand = true;

                using var undo = CreateUndoTransaction();

                _editorOperations.AddBeforeTextBufferChangePrimitive();

                MoveCaretToClosingPoint();

                _editorOperations.AddAfterTextBufferChangePrimitive();

                undo.Complete();
            }
        }

        public void PreReturn(out bool handledCommand)
            => handledCommand = false;

        public void PostReturn()
        {
            try
            {
                PostReturnWorker(GetFreshCancellationToken());
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void PostReturnWorker(CancellationToken cancellationToken)
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            if (this.GetCaretPosition().HasValue)
            {
                var closingSnapshotPoint = ClosingPoint.GetPoint(SubjectBuffer.CurrentSnapshot);

                if (closingSnapshotPoint.Position > 0 && HasNoForwardTyping(this.GetCaretPosition().Value, closingSnapshotPoint.Subtract(1)))
                {
                    if (!TryGetBraceCompletionContext(out var context, cancellationToken))
                    {
                        return;
                    }

                    var indentationOptions = SubjectBuffer.GetIndentationOptions(EditorOptionsService, context.FallbackOptions, context.Document.LanguageServices, explicitFormat: false);
                    var changesAfterReturn = _service.GetTextChangeAfterReturn(context, indentationOptions, cancellationToken);
                    if (changesAfterReturn != null)
                    {
                        using var caretPreservingTransaction = new CaretPreservingEditTransaction(EditorFeaturesResources.Brace_Completion, _undoHistory, _editorOperations);
                        ApplyBraceCompletionResult(changesAfterReturn.Value);
                        caretPreservingTransaction.Complete();
                    }
                }
            }
        }

        public void Finish()
        {
        }

        #endregion

        #region Unused IBraceCompletionSession Methods

        public void PostTab() { }

        public void PreDelete(out bool handledCommand)
            => handledCommand = false;

        public void PostDelete() { }

        #endregion

        #region Private Helpers

        private void EndSession()
        {
            // set the points to null to get off the stack
            // the stack will determine that the current point
            // is not contained within the session if either are null
            OpeningPoint = null;
            ClosingPoint = null;
        }

        // check if there any typing between the caret the closing point
        private bool HasForwardTyping
        {
            get
            {
                ThreadingContext.ThrowIfNotOnUIThread();
                var closingSnapshotPoint = ClosingPoint.GetPoint(SubjectBuffer.CurrentSnapshot);

                if (closingSnapshotPoint.Position > 0)
                {
                    var caretPos = this.GetCaretPosition();

                    if (caretPos.HasValue && !HasNoForwardTyping(caretPos.Value, closingSnapshotPoint.Subtract(1)))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // verify that there is only whitespace between the two given points
        private static bool HasNoForwardTyping(SnapshotPoint caretPoint, SnapshotPoint endPoint)
        {
            Debug.Assert(caretPoint.Snapshot == endPoint.Snapshot, "snapshots do not match");

            if (caretPoint.Snapshot == endPoint.Snapshot)
            {
                if (caretPoint == endPoint)
                {
                    return true;
                }

                if (caretPoint.Position < endPoint.Position)
                {
                    var span = new SnapshotSpan(caretPoint, endPoint);

                    return string.IsNullOrWhiteSpace(span.GetText());
                }
            }

            return false;
        }

        internal ITextUndoTransaction CreateUndoTransaction()
            => _undoHistory.CreateTransaction(EditorFeaturesResources.Brace_Completion);

        private void MoveCaretToClosingPoint()
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            var closingSnapshotPoint = ClosingPoint.GetPoint(SubjectBuffer.CurrentSnapshot);

            // find the position just after the closing brace in the view's text buffer
            var afterBrace = TextView.BufferGraph.MapUpToBuffer(closingSnapshotPoint,
                PointTrackingMode.Negative, PositionAffinity.Predecessor, TextView.TextBuffer);

            Debug.Assert(afterBrace.HasValue, "Unable to move caret to closing point");

            if (afterBrace.HasValue)
            {
                TextView.Caret.MoveTo(afterBrace.Value);
            }
        }

        private bool TryGetBraceCompletionContext(out BraceCompletionContext context, CancellationToken cancellationToken)
        {
            var document = SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                context = default;
                return false;
            }

            context = GetBraceCompletionContext(ParsedDocument.CreateSynchronously(document, cancellationToken), document.Project.GetFallbackAnalyzerOptions());
            return true;
        }

        private BraceCompletionContext GetBraceCompletionContext(ParsedDocument document, StructuredAnalyzerConfigOptions fallbackOptions)
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            var snapshot = SubjectBuffer.CurrentSnapshot;

            var closingSnapshotPoint = ClosingPoint.GetPosition(snapshot);
            var openingSnapshotPoint = OpeningPoint.GetPosition(snapshot);
            // The user is actively typing so the caret position should not be null.
            var caretPosition = this.GetCaretPosition().Value.Position;

            return new BraceCompletionContext(document, fallbackOptions, openingSnapshotPoint, closingSnapshotPoint, caretPosition);
        }

        private void ApplyBraceCompletionResult(BraceCompletionResult result)
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            using var edit = SubjectBuffer.CreateEdit();
            foreach (var change in result.TextChanges)
            {
                edit.Replace(change.Span.ToSpan(), change.NewText);
            }

            edit.ApplyAndLogExceptions();

            try
            {
                Contract.ThrowIfFalse(SubjectBuffer.CurrentSnapshot[OpeningPoint.GetPosition(SubjectBuffer.CurrentSnapshot)] == OpeningBrace,
                    "The opening point does not match the opening brace character");
                Contract.ThrowIfFalse(SubjectBuffer.CurrentSnapshot[ClosingPoint.GetPosition(SubjectBuffer.CurrentSnapshot) - 1] == ClosingBrace,
                    "The closing point does not match the closing brace character");
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                return;
            }

            var caretLine = SubjectBuffer.CurrentSnapshot.GetLineFromLineNumber(result.CaretLocation.Line);
            TextView.TryMoveCaretToAndEnsureVisible(new VirtualSnapshotPoint(caretLine, result.CaretLocation.Character));
        }

        #endregion
    }
}
