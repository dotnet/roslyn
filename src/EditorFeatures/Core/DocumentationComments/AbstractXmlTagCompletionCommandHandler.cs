// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.DocumentationComments
{
    internal abstract class AbstractXmlTagCompletionCommandHandler : IChainedCommandHandler<TypeCharCommandArgs>
    {
        private readonly ITextUndoHistoryRegistry _undoHistory;

        public string DisplayName => EditorFeaturesResources.XML_End_Tag_Completion;

        public AbstractXmlTagCompletionCommandHandler(ITextUndoHistoryRegistry undoHistory)
            => _undoHistory = undoHistory;

        protected abstract void TryCompleteTag(ITextView textView, ITextBuffer subjectBuffer, Document document, SnapshotPoint position, CancellationToken cancellationToken);

        public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextHandler)
            => nextHandler();

        public void ExecuteCommand(TypeCharCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            // Ensure completion and any other buffer edits happen first.
            nextHandler();

            var cancellationToken = context.OperationContext.UserCancellationToken;
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                ExecuteCommandWorker(args, context);
            }
            catch (OperationCanceledException)
            {
                // According to Editor command handler API guidelines, it's best if we return early if cancellation
                // is requested instead of throwing. Otherwise, we could end up in an invalid state due to already
                // calling nextHandler().
            }
        }

        private void ExecuteCommandWorker(TypeCharCommandArgs args, CommandExecutionContext context)
        {
            if (args.TypedChar is not '>' and not '/')
            {
                return;
            }

            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Completing_Tag))
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

                TryCompleteTag(args.TextView, args.SubjectBuffer, document, position.Value, context.OperationContext.UserCancellationToken);
            }
        }

        protected void InsertTextAndMoveCaret(ITextView textView, ITextBuffer subjectBuffer, SnapshotPoint position, string insertionText, int? finalCaretPosition)
        {
            using var transaction = _undoHistory.GetHistory(textView.TextBuffer).CreateTransaction("XmlTagCompletion");

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
