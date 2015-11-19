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

                var triggerInfo = CompletionTriggerInfo.CreateBackspaceTriggerInfo(deletedChar);
                var completionService = this.GetCompletionService();

                this.StartNewModelComputation(completionService, triggerInfo, filterItems: false);

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
                    (model != null && GetCompletionService().DismissIfLastFilterCharacterDeleted && AllFilterTextsEmpty(model, GetCaretPointInViewBuffer())))
                {
                    // If the caret moved out of bounds of our items, then we want to dismiss the list. 
                    this.StopModelComputation();
                    return;
                }
                else if (model != null && model.TriggerInfo.TriggerReason != CompletionTriggerReason.BackspaceOrDeleteCommand)
                {
                    // Filter the model if it wasn't invoked on backspace.
                    sessionOpt.FilterModel(CompletionFilterReason.BackspaceOrDelete);
                }
            }
        }

        private bool CaretHasLeftDefaultTrackingSpan(int caretPoint, Document document)
        {
            // We haven't finished computing the model, but we may need to dismiss.
            // Get the default tracking span and see if we're outside it.
            var defaultTrackingSpan = GetCompletionService().GetDefaultTrackingSpanAsync(document, caretPoint, CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            var newCaretPoint = GetCaretPointInViewBuffer();
            return !defaultTrackingSpan.IntersectsWith(new TextSpan(newCaretPoint, 0));
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
            ViewTextSpan filterSpanInViewBuffer;
            if (!textSpanToViewSpan.TryGetValue(item.FilterSpan, out filterSpanInViewBuffer))
            {
                filterSpanInViewBuffer = model.GetSubjectBufferFilterSpanInViewBuffer(item.FilterSpan);
                textSpanToViewSpan[item.FilterSpan] = filterSpanInViewBuffer;
            }

            if (caretPoint < filterSpanInViewBuffer.TextSpan.Start)
            {
                return true;
            }

            var textSnapshot = caretPoint.Snapshot;

            var currentText = model.GetCurrentTextInSnapshot(item.FilterSpan, textSnapshot, textSpanToText);
            var currentTextSpan = new TextSpan(filterSpanInViewBuffer.TextSpan.Start, currentText.Length);

            return currentText.Length == 0;
        }
    }
}
