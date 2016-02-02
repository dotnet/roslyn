// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.BlockCommentEditing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentEditing
{
    [ExportCommandHandler(nameof(BlockCommentEditingCommandHandler), ContentTypeNames.CSharpContentType)]
    [Order(After = PredefinedCommandHandlerNames.Completion)]
    internal class BlockCommentEditingCommandHandler : AbstractBlockCommentEditingCommandHandler
    {
        [ImportingConstructor]
        public BlockCommentEditingCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService) : base(undoHistoryRegistry, editorOperationsFactoryService)
        {
        }

        protected override string GetExteriorTextForNextLine(SnapshotPoint caretPosition)
        {
            var currentLine = caretPosition.Snapshot.GetLineFromPosition(caretPosition);

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
            return (caretPosition.Position + 2 > snapshot.Length) ? false : snapshot.GetText(caretPosition, 2) == "*/";
        }

        private static string GetPaddingOrIndentation(ITextSnapshotLine currentLine, int caretPosition, int firstNonWhitespacePosition, string exteriorText)
        {
            Debug.Assert(caretPosition >= firstNonWhitespacePosition + exteriorText.Length);

            var firstNonWhitespaceOffset = firstNonWhitespacePosition - currentLine.Start;
            Debug.Assert(firstNonWhitespaceOffset > -1);

            var lineText = currentLine.GetText();
            if ((lineText.Length == firstNonWhitespaceOffset + exteriorText.Length))
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

        private static bool IsCaretInsideBlockCommentSyntax(SnapshotPoint caretPosition)
        {
            var document = caretPosition.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            var syntaxTree = document.GetSyntaxTreeAsync().WaitAndGetResult(CancellationToken.None);
            var trivia = syntaxTree.FindTriviaAndAdjustForEndOfFile(caretPosition, CancellationToken.None);

            return trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);
        }
    }
}
