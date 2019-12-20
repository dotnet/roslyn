// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Implementation.SplitComment;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitComment
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(nameof(SplitCommentCommandHandler))]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    internal partial class SplitCommentCommandHandler : AbstractSplitCommentCommandHandler
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        [ImportingConstructor]
        public SplitCommentCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        public override string DisplayName => CSharpEditorResources.Split_comment;

        public override CommandState GetCommandState(ReturnKeyCommandArgs args)
        {
            return CommandState.Unspecified;
        }

        public override bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
        {
            return ExecuteCommandWorker(args);
        }

        public override bool ExecuteCommandWorker(ReturnKeyCommandArgs args)
        {
            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;
            var spans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);

            // Don't split strings if there is any actual selection.
            if (spans.Count == 1 && spans[0].IsEmpty)
            {
                var caret = textView.GetCaretPoint(subjectBuffer);
                if (caret != null)
                {
                    // Quick check.  If the line doesn't contain a quote in it before the caret,
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

        protected override bool SplitComment(ITextView textView, ITextBuffer subjectBuffer, SnapshotPoint caret)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();

            if (document != null)
            {
                var options = document.GetOptionsAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                var enabled = options.GetOption(SplitCommentOptions.Enabled);

                if (enabled)
                {
                    using var transaction = CaretPreservingEditTransaction.TryCreate(
                        CSharpEditorResources.Split_comment, textView, _undoHistoryRegistry, _editorOperationsFactoryService);

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

        protected override bool LineContainsComment(ITextSnapshotLine line, int caretPosition)
        {
            var snapshot = line.Snapshot;
            for (int i = line.Start; i < caretPosition; i++)
            {
                if (snapshot[i] == '/' && snapshot[i + 1] == '/')
                {
                    return true;
                }
            }

            return false;
        }

        protected override int? SplitComment(
           Document document, DocumentOptionSet options, int position, CancellationToken cancellationToken)
        {
            var useTabs = options.GetOption(FormattingOptions.UseTabs);
            var tabSize = options.GetOption(FormattingOptions.TabSize);
            var indentStyle = options.GetOption(FormattingOptions.SmartIndent, LanguageNames.CSharp);

            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            var sourceText = root.SyntaxTree.GetText(cancellationToken);

            var splitter = CommentSplitter.Create(
                document, position, root, sourceText,
                useTabs, tabSize, indentStyle, cancellationToken);

            if (splitter == null)
            {
                return null;
            }

            return splitter.TrySplit();
        }
    }
}
