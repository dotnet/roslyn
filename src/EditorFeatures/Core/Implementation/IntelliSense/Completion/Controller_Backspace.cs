// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        VSCommanding.CommandState IChainedCommandHandler<BackspaceKeyCommandArgs>.GetCommandState(BackspaceKeyCommandArgs args, System.Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void IChainedCommandHandler<BackspaceKeyCommandArgs>.ExecuteCommand(BackspaceKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            ExecuteBackspaceOrDelete(args.TextView, nextHandler, isDelete: false);
        }

        private void ExecuteBackspaceOrDelete(ITextView textView, Action nextHandler, bool isDelete)
        {
            AssertIsForeground();

            char? deletedChar;
            var subjectBufferCaretPoint = GetCaretPointInSubjectBuffer();
            var viewBufferCaretPoint = GetCaretPointInViewBuffer();
            if (isDelete)
            {
                deletedChar = viewBufferCaretPoint.Position >= 0 && viewBufferCaretPoint.Position < textView.TextBuffer.CurrentSnapshot.Length
                    ? textView.TextBuffer.CurrentSnapshot[viewBufferCaretPoint.Position]
                    : default;
            }
            else
            {
                // backspace
                deletedChar = viewBufferCaretPoint > 0
                    ? textView.TextBuffer.CurrentSnapshot[viewBufferCaretPoint - 1]
                    : default;
            }

            if (sessionOpt == null)
            {
                // No computation. Disconnect from caret position changes, send the backspace through,
                // and start a computation.
                this.TextView.TextBuffer.PostChanged -= OnTextViewBufferPostChanged;
                this.TextView.Caret.PositionChanged -= OnCaretPositionChanged;
                try
                {
                    nextHandler();
                }
                finally
                {
                    this.TextView.TextBuffer.PostChanged += OnTextViewBufferPostChanged;
                    this.TextView.Caret.PositionChanged += OnCaretPositionChanged;
                }

                var trigger = CompletionTrigger.CreateDeletionTrigger(deletedChar.GetValueOrDefault());
                var completionService = this.GetCompletionService();

                if (completionService != null)
                {
                    this.StartNewModelComputation(completionService, trigger);
                }

                return;
            }
            else
            {
                var textBeforeDeletion = SubjectBuffer.AsTextContainer().CurrentText;
                var documentBeforeDeletion = textBeforeDeletion.GetDocumentWithFrozenPartialSemantics(CancellationToken.None);

                this.TextView.TextBuffer.PostChanged -= OnTextViewBufferPostChanged;
                this.TextView.Caret.PositionChanged -= OnCaretPositionChanged;
                try
                {
                    nextHandler();
                }
                finally
                {
                    this.TextView.TextBuffer.PostChanged += OnTextViewBufferPostChanged;
                    this.TextView.Caret.PositionChanged += OnCaretPositionChanged;
                }

                var model = sessionOpt.Computation.InitialUnfilteredModel;

                // DismissIfLastCharacterDeleted should be applied only when started with Insertion, and then Deleted all characters typed.
                // This confirms with the original VS 2010 behavior.
                if ((model == null && CaretHasLeftDefaultTrackingSpan(subjectBufferCaretPoint, documentBeforeDeletion)) ||
                    (model != null && this.IsCaretOutsideAllItemBounds(model, this.GetCaretPointInViewBuffer())) ||
                    (model != null && model.Trigger.Kind == CompletionTriggerKind.Insertion && model.OriginalList.Rules.DismissIfLastCharacterDeleted && AllFilterTextsEmpty(model, GetCaretPointInViewBuffer())))
                {
                    // If the caret moved out of bounds of our items, then we want to dismiss the list. 
                    this.DismissSessionIfActive();
                    return;
                }
                else if (model != null)
                {
                    sessionOpt.FilterModel(CompletionFilterReason.Deletion, filterState: null);
                }
            }
        }

        private bool CaretHasLeftDefaultTrackingSpan(int caretPoint, Document document)
        {
            var completionService = GetCompletionService();
            if (completionService == null)
            {
                // SubjectBuffer no longer even has a workspace mapping
                return true;
            }

            // We haven't finished computing the model, but we may need to dismiss.
            // Get the context span and see if we're outside it.
            var text = document.GetTextSynchronously(CancellationToken.None);
            var contextSpan = completionService.GetDefaultCompletionListSpan(text, caretPoint);
            var newCaretPoint = GetCaretPointInViewBuffer();
            return !contextSpan.IntersectsWith(new TextSpan(newCaretPoint, 0));
        }

        internal bool AllFilterTextsEmpty(Model model, SnapshotPoint caretPoint)
        {
            var textSpanToTextCache = new Dictionary<TextSpan, string>();
            var textSpanToViewSpanCache = new Dictionary<TextSpan, ViewTextSpan>();

            foreach (var item in model.TotalItems)
            {
                if (!IsFilterTextEmpty(model, caretPoint, item, textSpanToTextCache, textSpanToViewSpanCache))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsFilterTextEmpty(
           Model model,
           SnapshotPoint caretPoint,
           CompletionItem item,
           Dictionary<TextSpan, string> textSpanToText,
           Dictionary<TextSpan, ViewTextSpan> textSpanToViewSpan)
        {
            // Easy first check.  See if the caret point is before the start of the item.
            if (!textSpanToViewSpan.TryGetValue(item.Span, out var filterSpanInViewBuffer))
            {
                filterSpanInViewBuffer = model.GetViewBufferSpan(item.Span);
                textSpanToViewSpan[item.Span] = filterSpanInViewBuffer;
            }

            if (caretPoint < filterSpanInViewBuffer.TextSpan.Start)
            {
                return true;
            }

            var textSnapshot = caretPoint.Snapshot;

            var currentText = model.GetCurrentTextInSnapshot(item.Span, textSnapshot, textSpanToText);

            return currentText.Length == 0;
        }
    }
}
