// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SplitComment
{
    internal abstract class AbstractSplitCommentCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
    {
        protected ITextUndoHistoryRegistry _undoHistoryRegistry;
        protected IEditorOperationsFactoryService _editorOperationsFactoryService;

        protected abstract bool LineContainsComment(ITextSnapshotLine line, int caretPosition);
        protected abstract int? SplitComment(Document document, DocumentOptionSet options, int position, CancellationToken cancellationToken);

        public string DisplayName => EditorFeaturesResources.Split_comment;

        public CommandState GetCommandState(ReturnKeyCommandArgs args)
        {
            return CommandState.Unspecified;
        }

        public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
        {
            return ExecuteCommandWorker(args);
        }

        public bool ExecuteCommandWorker(ReturnKeyCommandArgs args)
        {
            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;
            var spans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);

            // Don't split comments if there is any actual selection.
            if (spans.Count == 1 && spans[0].IsEmpty)
            {
                var caret = textView.GetCaretPoint(subjectBuffer);
                if (caret != null)
                {
                    // Quick check.  If the line doesn't contain a comment in it before the caret,
                    // then no point in doing any more expensive synchronous work.
                    var line = subjectBuffer.CurrentSnapshot.GetLineFromPosition(caret.Value);
                    if (LineContainsComment(line, caret.Value))
                    {
                        return SplitComment(textView, subjectBuffer, caret.Value);
                    }
                }
            }

            return false;
        }

        private bool SplitComment(ITextView textView, ITextBuffer subjectBuffer, SnapshotPoint caret)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();

            if (document != null)
            {
                var options = document.GetOptionsAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                var enabled = options.GetOption(SplitCommentOptions.Enabled);

                if (enabled)
                {
                    using var transaction = CaretPreservingEditTransaction.TryCreate(
                        EditorFeaturesResources.Split_comment, textView, _undoHistoryRegistry, _editorOperationsFactoryService);

                    var cursorPosition = SplitComment(document, options, caret, CancellationToken.None);
                    if (cursorPosition != null)
                    {
                        var snapshotPoint = new SnapshotPoint(
                            subjectBuffer.CurrentSnapshot, cursorPosition.Value);
                        var newCaretPoint = textView.BufferGraph.MapUpToBuffer(
                            snapshotPoint, PointTrackingMode.Negative, PositionAffinity.Predecessor,
                            textView.TextBuffer);

                        if (newCaretPoint != null)
                        {
                            textView.Caret.MoveTo(newCaretPoint.Value);
                        }

                        transaction.Complete();
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
