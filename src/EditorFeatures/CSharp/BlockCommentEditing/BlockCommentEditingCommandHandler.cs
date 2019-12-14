// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentEditing
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(nameof(BlockCommentEditingCommandHandler))]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    internal sealed class BlockCommentEditingCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        [ImportingConstructor]
        public BlockCommentEditingCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            Contract.ThrowIfNull(undoHistoryRegistry);
            Contract.ThrowIfNull(editorOperationsFactoryService);

            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        public string DisplayName => EditorFeaturesResources.Block_Comment_Editing;

        public CommandState GetCommandState(ReturnKeyCommandArgs args)
            => CommandState.Unspecified;

        public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
            => TryHandleReturnKey(args.SubjectBuffer, args.TextView);

        private bool TryHandleReturnKey(ITextBuffer subjectBuffer, ITextView textView)
        {
            if (!subjectBuffer.GetFeatureOnOffOption(FeatureOnOffOptions.AutoInsertBlockCommentStartString))
            {
                return false;
            }

            var caretPosition = textView.GetCaretPoint(subjectBuffer);
            if (caretPosition == null)
            {
                return false;
            }

            var exteriorText = GetExteriorTextForNextLine(caretPosition.Value);
            if (exteriorText == null)
            {
                return false;
            }

            using var transaction = _undoHistoryRegistry.GetHistory(textView.TextBuffer).CreateTransaction(EditorFeaturesResources.Insert_new_line);

            var editorOperations = _editorOperationsFactoryService.GetEditorOperations(textView);

            editorOperations.InsertNewLine();
            editorOperations.InsertText(exteriorText);

            transaction.Complete();
            return true;
        }

        private string GetExteriorTextForNextLine(SnapshotPoint caretPosition)
        {
            var currentLine = caretPosition.GetContainingLine();

            var firstNonWhitespacePosition = currentLine.GetFirstNonWhitespacePosition() ?? -1;
            if (firstNonWhitespacePosition == -1)
            {
                return null;
            }

            var currentLineStartsWithBlockCommentStartString = currentLine.StartsWith(firstNonWhitespacePosition, "/*", ignoreCase: false);
            var currentLineStartsWithBlockCommentEndString = currentLine.StartsWith(firstNonWhitespacePosition, "*/", ignoreCase: false);
            var currentLineStartsWithBlockCommentMiddleString = currentLine.StartsWith(firstNonWhitespacePosition, "*", ignoreCase: false);

            if (!currentLineStartsWithBlockCommentStartString && !currentLineStartsWithBlockCommentMiddleString)
            {
                return null;
            }

            if (!IsCaretInsideBlockCommentSyntax(caretPosition))
            {
                return null;
            }

            if (currentLineStartsWithBlockCommentStartString)
            {
                if (BlockCommentEndsRightAfterCaret(caretPosition))
                {
                    //      /*|*/
                    return " ";
                }
                else if (caretPosition == firstNonWhitespacePosition + 1)
                {
                    //      /|*
                    return null; // The newline inserted could break the syntax in a way that this handler cannot fix, let's leave it.
                }
                else
                {
                    //      /*|
                    return " *" + GetPaddingOrIndentation(currentLine, caretPosition, firstNonWhitespacePosition, "/*");
                }
            }

            if (currentLineStartsWithBlockCommentEndString)
            {
                if (BlockCommentEndsRightAfterCaret(caretPosition))
                {
                    //      /*
                    //      |*/
                    return " ";
                }
                else if (caretPosition == firstNonWhitespacePosition + 1)
                {
                    //      *|/
                    return "*";
                }
                else
                {
                    //      /*
                    //   |   */
                    return " * ";
                }
            }

            if (currentLineStartsWithBlockCommentMiddleString)
            {
                if (BlockCommentEndsRightAfterCaret(caretPosition))
                {
                    //      *|*/
                    return "";
                }
                else if (caretPosition > firstNonWhitespacePosition)
                {
                    //      *|
                    return "*" + GetPaddingOrIndentation(currentLine, caretPosition, firstNonWhitespacePosition, "*");
                }
                else
                {
                    //      /*
                    //   |   *
                    return " * ";
                }
            }

            return null;
        }

        private static bool BlockCommentEndsRightAfterCaret(SnapshotPoint caretPosition)
        {
            var snapshot = caretPosition.Snapshot;
            return ((int)caretPosition + 2 <= snapshot.Length) ? snapshot.GetText(caretPosition, 2) == "*/" : false;
        }

        private static string GetPaddingOrIndentation(ITextSnapshotLine currentLine, int caretPosition, int firstNonWhitespacePosition, string exteriorText)
        {
            Debug.Assert(caretPosition >= firstNonWhitespacePosition + exteriorText.Length);

            var firstNonWhitespaceOffset = firstNonWhitespacePosition - currentLine.Start;
            Debug.Assert(firstNonWhitespaceOffset > -1);

            var lineText = currentLine.GetText();
            if (lineText.Length == firstNonWhitespaceOffset + exteriorText.Length)
            {
                //     *|
                return " ";
            }

            var interiorText = lineText.Substring(firstNonWhitespaceOffset + exteriorText.Length);
            var interiorFirstNonWhitespaceOffset = interiorText.GetFirstNonWhitespaceOffset() ?? -1;

            if (interiorFirstNonWhitespaceOffset == 0)
            {
                //    /****|
                return " ";
            }

            var interiorFirstWhitespacePosition = firstNonWhitespacePosition + exteriorText.Length;
            if (interiorFirstNonWhitespaceOffset == -1 || caretPosition <= interiorFirstWhitespacePosition + interiorFirstNonWhitespaceOffset)
            {
                // *  |
                // or
                // *  |  1.
                //  ^^
                return currentLine.Snapshot.GetText(interiorFirstWhitespacePosition, caretPosition - interiorFirstWhitespacePosition);
            }
            else
            {
                // *   1. |
                //  ^^^
                return currentLine.Snapshot.GetText(interiorFirstWhitespacePosition, interiorFirstNonWhitespaceOffset);
            }
        }

        public static bool IsCaretInsideBlockCommentSyntax(SnapshotPoint caretPosition)
        {
            var snapshot = caretPosition.Snapshot;
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            var syntaxTree = document.GetSyntaxTreeSynchronously(CancellationToken.None);
            var trivia = syntaxTree.FindTriviaAndAdjustForEndOfFile(caretPosition, CancellationToken.None);

            var isBlockComment = trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);
            if (isBlockComment)
            {
                var span = trivia.FullSpan;
                if (span.Start < caretPosition && caretPosition < span.End)
                {
                    return true;
                }

                // FindTriviaAndAdjustForEndOfFile always returns something if position is EOF,
                // whether or not the result includes the position.
                // And the SyntaxTrivia for block comments always ends on EOF, closed or not.
                // So we need to handle
                // /**/|EOF
                // and
                // /*  |EOF
                if (caretPosition == snapshot.Length)
                {
                    if (span.Length < "/**/".Length)
                    {
                        return true;
                    }

                    // If the block comment is not closed, SyntaxTrivia contains diagnostics
                    // So when the SyntaxTrivia is clean, the block comment should be closed
                    if (!trivia.ContainsDiagnostics)
                    {
                        return false;
                    }

                    var textBeforeCaret = snapshot.GetText(caretPosition.Position - 2, 2);
                    return textBeforeCaret != "*/";
                }
            }

            return false;
        }
    }
}
