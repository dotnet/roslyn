// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities.Internal;

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
        private class BraceCompletionSession : IBraceCompletionSession
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
            private readonly IEditorBraceCompletionSession _session;
            private readonly ISmartIndentationService _smartIndentationService;

            #endregion

            #region Constructors

            public BraceCompletionSession(
                ITextView textView, ITextBuffer subjectBuffer,
                SnapshotPoint openingPoint, char openingBrace, char closingBrace, ITextUndoHistory undoHistory,
                IEditorOperationsFactoryService editorOperationsFactoryService, IEditorBraceCompletionSession session,
                ISmartIndentationService smartIndentationService)
            {
                this.TextView = textView;
                this.SubjectBuffer = subjectBuffer;
                this.OpeningBrace = openingBrace;
                this.ClosingBrace = closingBrace;
                this.ClosingPoint = SubjectBuffer.CurrentSnapshot.CreateTrackingPoint(openingPoint.Position, PointTrackingMode.Positive);
                _undoHistory = undoHistory;
                _editorOperations = editorOperationsFactoryService.GetEditorOperations(textView);
                _session = session;
                _smartIndentationService = smartIndentationService;
            }

            #endregion

            #region IBraceCompletionSession Methods

            public void Start()
            {
                // Brace completion is not cancellable.
                this.Start(CancellationToken.None);
            }

            private void Start(CancellationToken cancellationToken)
            {
                var closingSnapshotPoint = ClosingPoint.GetPoint(SubjectBuffer.CurrentSnapshot);

                if (closingSnapshotPoint.Position < 1)
                {
                    Debug.Fail("The closing point was not found at the expected position.");
                    EndSession();
                    return;
                }

                var openingSnapshotPoint = closingSnapshotPoint.Subtract(1);

                if (openingSnapshotPoint.GetChar() != OpeningBrace)
                {
                    // there is a bug in editor brace completion engine on projection buffer that already fixed in vs_pro. until that is FIed to use
                    // I will make this not to assert
                    // Debug.Fail("The opening brace was not found at the expected position.");
                    EndSession();
                    return;
                }

                OpeningPoint = SubjectBuffer.CurrentSnapshot.CreateTrackingPoint(openingSnapshotPoint, PointTrackingMode.Positive);

                var braceResult = _session.GetBraceCompletion(this, cancellationToken);
                if (braceResult == null)
                {
                    EndSession();
                    return;
                }

                using var caretPreservingTransaction = new CaretPreservingEditTransaction(EditorFeaturesResources.Brace_Completion, _undoHistory, _editorOperations);

                // Apply the change to complete the brace.
                ApplyBraceCompletionResult(braceResult.Value);

                // switch from positive to negative tracking so it stays against the closing brace
                ClosingPoint = SubjectBuffer.CurrentSnapshot.CreateTrackingPoint(ClosingPoint.GetPoint(SubjectBuffer.CurrentSnapshot), PointTrackingMode.Negative);

                var changesAfterStart = _session.GetChangesAfterCompletion(this, cancellationToken);
                if (changesAfterStart != null)
                {
                    ApplyBraceCompletionResult(changesAfterStart.Value);
                }

                caretPreservingTransaction.Complete();
            }

            public void PreBackspace(out bool handledCommand)
            {
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
                if (ClosingPoint == null)
                {
                    return;
                }

                // Brace completion is not cancellable.
                var cancellationToken = CancellationToken.None;
                var snapshot = this.SubjectBuffer.CurrentSnapshot;

                var closingSnapshotPoint = ClosingPoint.GetPoint(snapshot);
                if (!HasForwardTyping && _session.AllowOverType(this, cancellationToken))
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
            }

            public void PostOverType()
            {
            }

            public void PreTab(out bool handledCommand)
            {
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
                if (this.GetCaretPosition().HasValue)
                {
                    var closingSnapshotPoint = ClosingPoint.GetPoint(SubjectBuffer.CurrentSnapshot);

                    if (closingSnapshotPoint.Position > 0 && HasNoForwardTyping(this.GetCaretPosition().Value, closingSnapshotPoint.Subtract(1)))
                    {
                        var changesAfterReturn = _session.GetChangesAfterReturn(this, CancellationToken.None);
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

            private void ApplyBraceCompletionResult(BraceCompletionResult result)
            {
                // Apply sets of edits incrementally and update the buffer on each set applied.
                //
                // The text changes need to be small and incremental to ensure that the
                // opening and closing brace locations are not in the *middle* of a text change.
                // Otherwise, the tracking points will move to the start/end
                // of the text change span and no longer accurately reflect actual open and closing brace.
                // That causes the brace completion session to close prematurely so overtyping and tab
                // no longer work properly.
                foreach (var changes in result.TextChangesPerVersion)
                {
                    using var edit = SubjectBuffer.CreateEdit();
                    foreach (var change in changes)
                    {
                        edit.Replace(change.Span.ToSpan(), change.NewText);
                    }

                    edit.ApplyAndLogExceptions();

                    Debug.Assert(SubjectBuffer.CurrentSnapshot[OpeningPoint.GetPosition(SubjectBuffer.CurrentSnapshot)] == OpeningBrace,
                    "The opening point does not match the opening brace character");
                    Debug.Assert(SubjectBuffer.CurrentSnapshot[ClosingPoint.GetPosition(SubjectBuffer.CurrentSnapshot) - 1] == ClosingBrace,
                    "The closing point does not match the closing brace character");
                }

                var snapshotPoint = SubjectBuffer.CurrentSnapshot.GetPoint(result.CaretLocation);
                var line = snapshotPoint.GetContainingLine();

                if (line.GetText().IsNullOrWhiteSpace())
                {
                    // If the caret is on an empty line, put it at the correct indentation using virtual space.
                    PutCaretOnLine(this, _smartIndentationService, line.LineNumber);
                }
                else
                {
                    // Not on an empty line, just put the caret at the specified location.
                    TextView.TryMoveCaretToAndEnsureVisible(snapshotPoint);
                }

                static void PutCaretOnLine(IBraceCompletionSession session, ISmartIndentationService smartIndentationService, int lineNumber)
                {
                    var lineOnSubjectBuffer = session.SubjectBuffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber);

                    var indentation = GetDesiredIndentation(session, smartIndentationService, lineOnSubjectBuffer);

                    session.TextView.TryMoveCaretToAndEnsureVisible(new VirtualSnapshotPoint(lineOnSubjectBuffer, indentation));
                }

                static int GetDesiredIndentation(IBraceCompletionSession session, ISmartIndentationService smartIndentationService, ITextSnapshotLine lineOnSubjectBuffer)
                {
                    // first try VS's smart indentation service
                    var indentation = session.TextView.GetDesiredIndentation(smartIndentationService, lineOnSubjectBuffer);
                    if (indentation.HasValue)
                    {
                        return indentation.Value;
                    }

                    // do poor man's indentation
                    var openingPoint = session.OpeningPoint.GetPoint(lineOnSubjectBuffer.Snapshot);
                    var openingSpanLine = openingPoint.GetContainingLine();

                    return openingPoint - openingSpanLine.Start;
                }
            }

            #endregion
        }
    }
}
