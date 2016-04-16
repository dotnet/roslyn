// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    /// <summary>
    /// This class contains the logic common to VS and ETA when implementing IInlineRenameUndoManager
    /// </summary>
    internal abstract class AbstractInlineRenameUndoManager<TBufferState>
    {
        protected class ActiveSpanState
        {
            public string ReplacementText;
            public int SelectionAnchorPoint;
            public int SelectionActivePoint;
        }

        protected readonly InlineRenameService InlineRenameService;

        protected readonly Dictionary<ITextBuffer, TBufferState> UndoManagers = new Dictionary<ITextBuffer, TBufferState>();
        protected readonly Stack<ActiveSpanState> UndoStack = new Stack<ActiveSpanState>();
        protected readonly Stack<ActiveSpanState> RedoStack = new Stack<ActiveSpanState>();
        protected ActiveSpanState initialState;
        protected ActiveSpanState currentState;
        protected bool updatePending = false;

        public AbstractInlineRenameUndoManager(InlineRenameService inlineRenameService)
        {
            this.InlineRenameService = inlineRenameService;
        }

        public void Disconnect()
        {
            this.UndoManagers.Clear();
            this.UndoStack.Clear();
            this.RedoStack.Clear();
            this.initialState = null;
            this.currentState = null;
        }

        private void UpdateCurrentState(string replacementText, ITextSelection selection, SnapshotSpan activeSpan)
        {
            var snapshot = activeSpan.Snapshot;
            var selectionSpan = selection.GetSnapshotSpansOnBuffer(snapshot.TextBuffer).Single();

            var start = selectionSpan.Start.TranslateTo(snapshot, PointTrackingMode.Positive).Position - activeSpan.Start.Position;
            var end = selectionSpan.End.TranslateTo(snapshot, PointTrackingMode.Positive).Position - activeSpan.Start.Position;

            this.currentState = new ActiveSpanState()
            {
                ReplacementText = replacementText,
                SelectionAnchorPoint = selection.IsReversed ? end : start,
                SelectionActivePoint = selection.IsReversed ? start : end
            };
        }

        public void CreateInitialState(string replacementText, ITextSelection selection, SnapshotSpan startingSpan)
        {
            UpdateCurrentState(replacementText, selection, startingSpan);
            this.initialState = this.currentState;
        }

        public void OnTextChanged(ITextSelection selection, SnapshotSpan singleTrackingSpanTouched)
        {
            this.RedoStack.Clear();
            if (!this.UndoStack.Any())
            {
                this.UndoStack.Push(this.initialState);
            }

            // For now, we will only ever be one Undo away from the beginning of the rename session.  We can
            // implement Undo merging in the future. 
            var replacementText = singleTrackingSpanTouched.GetText();
            UpdateCurrentState(replacementText, selection, singleTrackingSpanTouched);

            this.InlineRenameService.ActiveSession.ApplyReplacementText(replacementText, propagateEditImmediately: false);
        }

        public void UpdateSelection(ITextView textView, ITextBuffer subjectBuffer, ITrackingSpan activeRenameSpan)
        {
            var snapshot = subjectBuffer.CurrentSnapshot;
            var anchor = new VirtualSnapshotPoint(snapshot, this.currentState.SelectionAnchorPoint + activeRenameSpan.GetStartPoint(snapshot));
            var active = new VirtualSnapshotPoint(snapshot, this.currentState.SelectionActivePoint + activeRenameSpan.GetStartPoint(snapshot));
            textView.SetSelection(anchor, active);
        }

        public void Undo(ITextBuffer subjectBuffer)
        {
            if (this.UndoStack.Count > 0)
            {
                this.RedoStack.Push(this.currentState);
                this.currentState = this.UndoStack.Pop();
                this.InlineRenameService.ActiveSession.ApplyReplacementText(this.currentState.ReplacementText, propagateEditImmediately: true);
            }
            else
            {
                this.InlineRenameService.ActiveSession.Cancel();
            }
        }

        public void Redo(ITextBuffer subjectBuffer)
        {
            if (this.RedoStack.Count > 0)
            {
                this.UndoStack.Push(this.currentState);
                this.currentState = this.RedoStack.Pop();
                this.InlineRenameService.ActiveSession.ApplyReplacementText(this.currentState.ReplacementText, propagateEditImmediately: true);
            }
        }

        protected abstract void UndoTemporaryEdits(ITextBuffer subjectBuffer, bool disconnect, bool undoConflictResolution);

        protected void ApplyReplacementText(ITextBuffer subjectBuffer, ITextUndoHistory undoHistory, object propagateSpansEditTag, IEnumerable<ITrackingSpan> spans, string replacementText)
        {
            // roll back to the initial state for the buffer after conflict resolution
            this.UndoTemporaryEdits(subjectBuffer, disconnect: false, undoConflictResolution: replacementText == string.Empty);

            using (var transaction = undoHistory.CreateTransaction(GetUndoTransactionDescription(replacementText)))
            using (var edit = subjectBuffer.CreateEdit(EditOptions.None, null, propagateSpansEditTag))
            {
                foreach (var span in spans)
                {
                    if (span.GetText(subjectBuffer.CurrentSnapshot) != replacementText)
                    {
                        edit.Replace(span.GetSpan(subjectBuffer.CurrentSnapshot), replacementText);
                    }
                }

                edit.Apply();
                if (!edit.HasEffectiveChanges && !this.UndoStack.Any())
                {
                    transaction.Cancel();
                }
                else
                {
                    transaction.Complete();
                }
            }
        }

        protected string GetUndoTransactionDescription(string replacementText)
        {
            return replacementText == string.Empty ? "Delete Text" : replacementText;
        }
    }
}
