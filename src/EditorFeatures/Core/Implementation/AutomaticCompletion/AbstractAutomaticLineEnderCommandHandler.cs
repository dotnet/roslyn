﻿// Licensed to the .NET Foundation under one or more agreements.
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

        protected AbstractAutomaticLineEnderCommandHandler(
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

        /// <summary>
        /// Add or remove the braces for <param name="selectedNode"/>.
        /// </summary>
        protected abstract void ModifySelectedNode(AutomaticLineEnderCommandArgs args, Document document, SyntaxNode selectedNode, bool addBrace, int caretPosition, CancellationToken cancellationToken);

        /// <summary>
        /// Get the syntax node needs add/remove braces.
        /// </summary>
        protected abstract (SyntaxNode selectedNode, bool addBrace)? GetValidNodeToModifyBraces(Document document, int caretPosition, CancellationToken cancellationToken);

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

            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Automatically_completing))
            {
                var cancellationToken = context.OperationContext.UserCancellationToken;

                // caret is not on the subject buffer. nothing we can do
                var caret = args.TextView.GetCaretPoint(args.SubjectBuffer);
                if (!caret.HasValue)
                {
                    NextAction(operations, nextHandler);
                    return;
                }

                var caretPosition = caret.Value;
                // special cases where we treat this command simply as Return.
                if (TreatAsReturn(document, caretPosition, cancellationToken))
                {
                    // leave it to the VS editor to handle this command.
                    // VS editor's default implementation of SmartBreakLine is simply BreakLine, which inserts
                    // a new line and positions the caret with smart indent.
                    nextHandler();
                    return;
                }

                var subjectLineWhereCaretIsOn = caretPosition.GetContainingLine();

                // Two possible operations
                // 1. Add/remove the brace for the selected syntax node (only for C#)
                // 2. Append an ending string to the line. (For C#, it is semicolon ';', For VB, it is underline '_')

                // Check if the node could be used to add/remove brace.
                var selectNodeAndOperationKind = GetValidNodeToModifyBraces(document, caretPosition, cancellationToken);
                if (selectNodeAndOperationKind != null)
                {
                    var (selectedNode, addBrace) = selectNodeAndOperationKind.Value;
                    using var transaction = args.TextView.CreateEditTransaction(EditorFeaturesResources.Automatic_Line_Ender, _undoRegistry, _editorOperationsFactoryService);
                    ModifySelectedNode(args, document, selectedNode, addBrace, caretPosition, cancellationToken);
                    NextAction(operations, nextHandler);
                    transaction.Complete();
                    return;
                }

                // Check if we could find the ending position
                var endingInsertionPosition = GetInsertionPositionForEndingString(document, subjectLineWhereCaretIsOn, cancellationToken);
                if (endingInsertionPosition != null)
                {
                    using var transaction = args.TextView.CreateEditTransaction(EditorFeaturesResources.Automatic_Line_Ender, _undoRegistry, _editorOperationsFactoryService);
                    InsertEnding(args.TextView, document, endingInsertionPosition.Value, caretPosition, cancellationToken);
                    NextAction(operations, nextHandler);
                    transaction.Complete();
                    return;
                }

                // Neither of the two operations could be performed
                using var editTransaction = args.TextView.CreateEditTransaction(EditorFeaturesResources.Automatic_Line_Ender, _undoRegistry, _editorOperationsFactoryService);
                NextAction(operations, nextHandler);
                editTransaction.Complete();
            }
        }

        /// <summary>
        /// return insertion point for the ending string
        /// </summary>
        private static int? GetInsertionPositionForEndingString(Document document, ITextSnapshotLine line, CancellationToken cancellationToken)
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
        private void InsertEnding(
            ITextView textView,
            Document document,
            int insertPosition,
            SnapshotPoint caretPosition,
            CancellationToken cancellationToken)
        {
            // 1. Move the caret to line end.
            textView.TryMoveCaretToAndEnsureVisible(caretPosition.GetContainingLine().End);

            // 2. Insert ending to the document.
            var newDocument = document;
            var endingString = GetEndingString(document, caretPosition, cancellationToken);
            if (endingString != null)
            {
                newDocument = document.InsertText(insertPosition, endingString, cancellationToken);
            }

            // 3. format the document and apply the changes to the workspace
            FormatAndApplyBasedOnEndToken(newDocument, insertPosition, cancellationToken);
        }
    }
}
