// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.Implementation.DocumentationComments
{
    internal abstract class AbstractXmlTagCompletionCommandHandler : ICommandHandler<TypeCharCommandArgs>
    {
        private readonly ITextUndoHistoryRegistry _undoHistory;
        private readonly IWaitIndicator _waitIndicator;

        public AbstractXmlTagCompletionCommandHandler(ITextUndoHistoryRegistry undoHistory, IWaitIndicator waitIndicator)
        {
            _undoHistory = undoHistory;
            _waitIndicator = waitIndicator;
        }

        protected abstract void TryCompleteTag(ITextView textView, ITextBuffer subjectBuffer, Document document, SnapshotPoint position, CancellationToken cancellationToken);

        public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(TypeCharCommandArgs args, Action nextHandler)
        {
            // Ensure completion and any other buffer edits happen first.
            nextHandler();

            if (args.TypedChar != '>' && args.TypedChar != '/')
            {
                return;
            }

            _waitIndicator.Wait(
                title: EditorFeaturesResources.XML_End_Tag_Completion,
                message: EditorFeaturesResources.Completing_Tag,
                allowCancel: true,
                action: w =>
                    {
                        var buffer = args.SubjectBuffer;

                        var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                        if (document == null)
                        {
                            return;
                        }

                        // We actually want the caret position after any operations
                        var position = args.TextView.GetCaretPoint(args.SubjectBuffer);

                        // No caret position? No edit!
                        if (!position.HasValue)
                        {
                            return;
                        }

                        TryCompleteTag(args.TextView, args.SubjectBuffer, document, position.Value, w.CancellationToken);
                    });
        }

        protected void InsertTextAndMoveCaret(ITextView textView, ITextBuffer subjectBuffer, SnapshotPoint position, string insertionText, int? finalCaretPosition)
        {
            using (var transaction = _undoHistory.GetHistory(textView.TextBuffer).CreateTransaction("XmlTagCompletion"))
            {
                subjectBuffer.Insert(position, insertionText);

                if (finalCaretPosition.HasValue)
                {
                    var point = subjectBuffer.CurrentSnapshot.GetPoint(finalCaretPosition.Value);
                    textView.TryMoveCaretToAndEnsureVisible(point);
                }

                transaction.Complete();
            }
        }
    }
}
