// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
{
    internal abstract class AbstractAutomaticLineEnderCommandHandler :
        IChainedCommandHandler<AutomaticLineEnderCommandArgs>
    {
        private readonly ITextUndoHistoryRegistry _undoRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        public string DisplayName => EditorFeaturesResources.Automatic_Line_Ender;

        public AbstractAutomaticLineEnderCommandHandler(
            ITextUndoHistoryRegistry undoRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            _undoRegistry = undoRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        /// <summary>
        /// get ending string if there is one
        /// </summary>
        protected abstract string? GetEndingString(Document document, int position, CancellationToken cancellationToken);

        /// <summary>
        /// do next action
        /// </summary>
        protected abstract void NextAction(IEditorOperations editorOperation, Action nextAction);

        /// <summary>
        /// format after inserting ending string
        /// </summary>
        protected abstract Document FormatAndApplyBasedOnEndToken(Document document, int position, CancellationToken cancellationToken);

        /// <summary>
        /// special cases where we do not want to do line completion but just fall back to line break and formatting.
        /// </summary>
        protected abstract bool TreatAsReturn(Document document, int caretPosition, CancellationToken cancellationToken);

        protected abstract void AddOrRemoveBracePairIfRequired(ITextView textView, Document document, int caretPosition, CancellationToken cancellationToken);

        protected abstract SyntaxNode? GetValidNodeToInsertBraces(Document document, int caretPosition, CancellationToken cancellationToken);

        public CommandState GetCommandState(AutomaticLineEnderCommandArgs args, Func<CommandState> nextHandler)
            => CommandState.Available;

        public void ExecuteCommand(AutomaticLineEnderCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            // get editor operation
            var operations = _editorOperationsFactoryService.GetEditorOperations(args.TextView);
            if (operations == null)
            {
                nextHandler();
                return;
            }

            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                NextAction(operations, nextHandler);
                return;
            }

            // feature off
            if (!document.Project.Solution.Workspace.Options.GetOption(InternalFeatureOnOffOptions.AutomaticLineEnder))
            {
                NextAction(operations, nextHandler);
                return;
            }

            using (context.OperationContext.AddScope(allowCancellation: false, EditorFeaturesResources.Automatically_completing))
            {
                // This is a non cancellable command
                var cancellationToken = CancellationToken.None;

                // caret is not on the subject buffer. nothing we can do
                var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer);
                if (!caretPosition.HasValue)
                {
                    NextAction(operations, nextHandler);
                    return;
                }

                // special cases where we treat this command simply as Return.
                if (TreatAsReturn(document, caretPosition.Value.Position, cancellationToken))
                {
                    // leave it to the VS editor to handle this command.
                    // VS editor's default implementation of SmartBreakLine is simply BreakLine, which inserts
                    // a new line and positions the caret with smart indent.
                    nextHandler();
                    return;
                }

                var subjectLineWhereCaretIsOn = caretPosition.Value.GetContainingLine();
                var endInsertionPoint = GetInsertionPointForEndingString(document, subjectLineWhereCaretIsOn, cancellationToken);
                var node = GetValidNodeToInsertBraces(document, caretPosition.Value, cancellationToken);
                if (node == null && !endInsertionPoint.HasValue)
                {
                    NextAction(operations, nextHandler);
                    return;
                }

                using var transaction = args.TextView.CreateEditTransaction(EditorFeaturesResources.Automatic_Line_Ender, _undoRegistry, _editorOperationsFactoryService);
                var newDocument = document;
                if (endInsertionPoint.HasValue)
                {
                    newDocument = InsertEndingAndFormat(args.TextView, document, endInsertionPoint.Value, caretPosition.Value, cancellationToken);
                }

                if (node != null)
                {
                    AddOrRemoveBracePairIfRequired(args.TextView, newDocument, caretPosition.Value, cancellationToken);
                }

                // now, insert new line
                NextAction(operations, nextHandler);
                transaction.Complete();
            }
        }

        /// <summary>
        /// return insertion point for the ending string
        /// </summary>
        private static int? GetInsertionPointForEndingString(Document document, ITextSnapshotLine line, CancellationToken cancellationToken)
        {
            var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
            var text = root.SyntaxTree.GetText(cancellationToken);

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // find last token on the line
            var token = syntaxFacts.FindTokenOnLeftOfPosition(root, line.End);
            if (token.RawKind == 0)
            {
                return null;
            }

            // bug # 16770
            // don't do anything if token is multiline token such as verbatim string
            if (line.End < token.Span.End)
            {
                return null;
            }

            // if there is only whitespace, token doesn't need to be on same line
            if (string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(token.Span.End, line.End))))
            {
                return line.End;
            }

            // if token is on different line than caret but caret line is empty, we insert ending point at the end of the line
            if (text.Lines.IndexOf(token.Span.End) != text.Lines.IndexOf(line.End))
            {
                return string.IsNullOrWhiteSpace(line.GetText()) ? (int?)line.End : null;
            }

            return token.Span.End;
        }

        /// <summary>
        /// insert ending string if there is one to insert
        /// </summary>
        private Document InsertEndingAndFormat(
            ITextView textView,
            Document document,
            int insertPosition,
            SnapshotPoint caretPosition,
            CancellationToken cancellationToken)
        {
            textView.TryMoveCaretToAndEnsureVisible(caretPosition.GetContainingLine().End);

            var newDocument = document;
            var endingString = GetEndingString(document, caretPosition, cancellationToken);
            if (endingString != null)
            {
                // apply end string to workspace
                newDocument = document.InsertText(insertPosition, endingString, cancellationToken);
            }

            // format the document and apply the changes to the workspace
            return FormatAndApplyBasedOnEndToken(newDocument, insertPosition, cancellationToken);
        }
    }
}
