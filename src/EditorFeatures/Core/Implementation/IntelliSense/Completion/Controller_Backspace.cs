// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        CommandState ICommandHandler<BackspaceKeyCommandArgs>.GetCommandState(BackspaceKeyCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void ICommandHandler<BackspaceKeyCommandArgs>.ExecuteCommand(BackspaceKeyCommandArgs args, Action nextHandler)
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
                    : default(char?);
            }
            else
            {
                // backspace
                deletedChar = viewBufferCaretPoint > 0
                    ? textView.TextBuffer.CurrentSnapshot[viewBufferCaretPoint - 1]
                    : default(char?);
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

                this.StartNewModelComputation(
                    completionService, trigger, filterItems: false, dismissIfEmptyAllowed: true);

                return;
            }
            else
            {
                var textBeforeDeletion = SubjectBuffer.AsTextContainer().CurrentText;
                var documentBeforeDeletion = textBeforeDeletion.GetDocumentWithFrozenPartialSemanticsAsync(CancellationToken.None)
                                                                .WaitAndGetResult(CancellationToken.None);

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

                if ((model == null && CaretHasLeftDefaultTrackingSpan(subjectBufferCaretPoint, documentBeforeDeletion)) ||
                    (model != null && this.IsCaretOutsideAllItemBounds(model, this.GetCaretPointInViewBuffer())) ||
                    (model != null && model.OriginalList.Rules.DismissIfLastCharacterDeleted && AllFilterTextsEmpty(model, GetCaretPointInViewBuffer())))
                {
                    // If the caret moved out of bounds of our items, then we want to dismiss the list. 
                    this.StopModelComputation();
                    return;
                }
                else if (model != null && model.Trigger.Kind != CompletionTriggerKind.Deletion)
                {
                    // Filter the model if it wasn't invoked on backspace.
                    sessionOpt.FilterModel(
                        CompletionFilterReason.BackspaceOrDelete,
                        recheckCaretPosition: false,
                        dismissIfEmptyAllowed: true,
                        filterState: null);
                }
            }
        }

        private bool CaretHasLeftDefaultTrackingSpan(int caretPoint, Document document)
        {
            // We haven't finished computing the model, but we may need to dismiss.
            // Get the context span and see if we're outside it.
            var text = document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            var contextSpan = GetCompletionService().GetDefaultCompletionListSpan(text, caretPoint);
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
           PresentationItem item,
           Dictionary<TextSpan, string> textSpanToText,
           Dictionary<TextSpan, ViewTextSpan> textSpanToViewSpan)
        {
            // Easy first check.  See if the caret point is before the start of the item.
            ViewTextSpan filterSpanInViewBuffer;
            if (!textSpanToViewSpan.TryGetValue(item.Item.Span, out filterSpanInViewBuffer))
            {
                filterSpanInViewBuffer = model.GetViewBufferSpan(item.Item.Span);
                textSpanToViewSpan[item.Item.Span] = filterSpanInViewBuffer;
            }

            if (caretPoint < filterSpanInViewBuffer.TextSpan.Start)
            {
                return true;
            }

            var textSnapshot = caretPoint.Snapshot;

            var currentText = model.GetCurrentTextInSnapshot(item.Item.Span, textSnapshot, textSpanToText);
            var currentTextSpan = new TextSpan(filterSpanInViewBuffer.TextSpan.Start, currentText.Length);

            return currentText.Length == 0;
        }
    }
}