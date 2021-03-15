﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
{
    internal partial class BraceCompletionSessionProvider
    {
        // ported and modified from "Platform\Text\Impl\BraceCompletion\BraceCompletionDefaultSession.cs"
        //
        // we want to provide better context based brace completion but IBraceCompletionContext is too simple for that.
        // fortunately, editor provides another extension point where we have more control over brace completion but we do not
        // want to re-implement logics base session provider already provides. so I ported editor's default session and 
        // modified it little bit so that we can use it as base class.
        private class BraceCompletionSession : ForegroundThreadAffinitizedObject, IBraceCompletionSession
        {
            #region Private Members

            public char OpeningBrace { get; }
            public char ClosingBrace { get; }
            public ITrackingPoint OpeningPoint { get; private set; }
            public ITrackingPoint ClosingPoint { get; private set; }
            public ITextBuffer SubjectBuffer { get; }
            public ITextView TextView { get; }

            private readonly ITextUndoHistory _undoHistory;
            private readonly IEditorOperations _editorOperations;
            private readonly IBraceCompletionService _service;

            #endregion

            #region Constructors

            public BraceCompletionSession(
                ITextView textView, ITextBuffer subjectBuffer,
                SnapshotPoint openingPoint, char openingBrace, char closingBrace, ITextUndoHistory undoHistory,
                IEditorOperationsFactoryService editorOperationsFactoryService, IBraceCompletionService service,
                IThreadingContext threadingContext)
                : base(threadingContext, assertIsForeground: true)
            {
                this.TextView = textView;
                this.SubjectBuffer = subjectBuffer;
                this.OpeningBrace = openingBrace;
                this.ClosingBrace = closingBrace;
                this.ClosingPoint = SubjectBuffer.CurrentSnapshot.CreateTrackingPoint(openingPoint.Position, PointTrackingMode.Positive);
                _undoHistory = undoHistory;
                _editorOperations = editorOperationsFactoryService.GetEditorOperations(textView);
                _service = service;
            }

            #endregion

            #region IBraceCompletionSession Methods

            public void Start()
            {
                this.AssertIsForeground();
                // Brace completion is not cancellable.
                if (!this.TryStart(CancellationToken.None))
                {
                    EndSession();
                }
            }

            private bool TryStart(CancellationToken cancellationToken)
            {
                this.AssertIsForeground();
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

                var context = GetBraceCompletionContext();
                if (context == null)
                {
                    return false;
                }

                var braceResult = _service.GetBraceCompletionAsync(context.Value, cancellationToken).WaitAndGetResult(cancellationToken);
                if (braceResult == null)
                {
                    return false;
                }

                using var caretPreservingTransaction = new CaretPreservingEditTransaction(EditorFeaturesResources.Brace_Completion, _undoHistory, _editorOperations);

                // Apply the change to complete the brace.
                ApplyBraceCompletionResult(braceResult.Value);

                // switch the closing point from positive to negative tracking so that the closing point stays against the closing brace
                ClosingPoint = SubjectBuffer.CurrentSnapshot.CreateTrackingPoint(ClosingPoint.GetPoint(SubjectBuffer.CurrentSnapshot), PointTrackingMode.Negative);

                var contextAfterStart = GetBraceCompletionContext();
                if (contextAfterStart != null)
                {
                    var changesAfterStart = _service.GetTextChangesAfterCompletionAsync(contextAfterStart.Value, cancellationToken).WaitAndGetResult(cancellationToken);
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
                this.AssertIsForeground();
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
                this.AssertIsForeground();
                handledCommand = false;
                if (ClosingPoint == null)
                {
                    return;
                }

                // Brace completion is not cancellable.
                var cancellationToken = CancellationToken.None;
                var snapshot = this.SubjectBuffer.CurrentSnapshot;

                var closingSnapshotPoint = ClosingPoint.GetPoint(snapshot);

                if (!HasForwardTyping && AllowOverType())
                {
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

                return;

                bool AllowOverType()
                {
                    var context = GetBraceCompletionContext();
                    return context != null && _service.AllowOverTypeAsync(context.Value, cancellationToken).WaitAndGetResult(cancellationToken);
                }
            }

            public void PostOverType()
            {
            }

            public void PreTab(out bool handledCommand)
            {
                this.AssertIsForeground();
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
                this.AssertIsForeground();
                if (this.GetCaretPosition().HasValue)
                {
                    var closingSnapshotPoint = ClosingPoint.GetPoint(SubjectBuffer.CurrentSnapshot);

                    if (closingSnapshotPoint.Position > 0 && HasNoForwardTyping(this.GetCaretPosition().Value, closingSnapshotPoint.Subtract(1)))
                    {
                        var context = GetBraceCompletionContext();
                        if (context == null)
                        {
                            return;
                        }

                        var changesAfterReturn = _service.GetTextChangeAfterReturnAsync(context.Value, CancellationToken.None).WaitAndGetResult(CancellationToken.None);
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
                    this.AssertIsForeground();
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
                this.AssertIsForeground();
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

            private BraceCompletionContext? GetBraceCompletionContext()
            {
                this.AssertIsForeground();
                var snapshot = SubjectBuffer.CurrentSnapshot;

                var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                {
                    return null;
                }

                var closingSnapshotPoint = ClosingPoint.GetPosition(snapshot);
                var openingSnapshotPoint = OpeningPoint.GetPosition(snapshot);
                // The user is actively typing so the caret position should not be null.
                var caretPosition = this.GetCaretPosition().Value.Position;
                return new BraceCompletionContext(document, openingSnapshotPoint, closingSnapshotPoint, caretPosition);
            }

            private void ApplyBraceCompletionResult(BraceCompletionResult result)
            {
                this.AssertIsForeground();
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
}
