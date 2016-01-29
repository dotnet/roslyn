// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.BlockCommentCompletion;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentCompletion
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.BlockCommentCompletion, ContentTypeNames.CSharpContentType)]
    internal class BlockCommentCompletionCommandHandler : AbstractBlockCommentCompletionCommandHandler
    {
        [ImportingConstructor]
        public BlockCommentCompletionCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService) : base(undoHistoryRegistry, editorOperationsFactoryService)
        {
        }

        protected override string GetExteriorTextForNextLine(SnapshotPoint caretPosition)
        {
            var snapshotLine = caretPosition.Snapshot.GetLineFromPosition(caretPosition);

            var firstNonWhitespacePosition = snapshotLine.GetFirstNonWhitespacePosition() ?? -1;
            if (firstNonWhitespacePosition == -1)
            {
                return null;
            }

            var currentLineStartsWithBlockCommentStartString = snapshotLine.StartsWith(firstNonWhitespacePosition, "/*", false);
            var currentLineStartsWithBlockCommentEndString = snapshotLine.StartsWith(firstNonWhitespacePosition, "*/", false);
            var currentLineStartsWithBlockCommentMiddleString = snapshotLine.StartsWith(firstNonWhitespacePosition, "*", false);

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
                    return " *" + GetPaddingOrIndentation(snapshotLine, caretPosition, firstNonWhitespacePosition, "/*");
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
                    return "*" + GetPaddingOrIndentation(snapshotLine, caretPosition, firstNonWhitespacePosition, "*");
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

        private static bool BlockCommentEndsRightAfterCaret(SnapshotPoint caretPosition) => caretPosition.Snapshot.GetText(caretPosition, 2) == "*/";

        private static string GetPaddingOrIndentation(ITextSnapshotLine snapshotLine, int caretPosition, int firstNonWhitespacePosition, string exteriorText)
        {
            Debug.Assert(caretPosition >= firstNonWhitespacePosition + exteriorText.Length);

            var firstNonWhitespaceOffset = firstNonWhitespacePosition - snapshotLine.Start;
            Debug.Assert(firstNonWhitespaceOffset > -1);

            var lineText = snapshotLine.GetText();
            if ((lineText.Length == firstNonWhitespaceOffset + exteriorText.Length))
            {
                //     *|
                return " ";
            }

            var interiorText = lineText.Substring(firstNonWhitespaceOffset + exteriorText.Length);
            var interiorFirstNonWhitespaceOffset = interiorText.GetFirstNonWhitespaceOffset() ?? -1;

            if (interiorFirstNonWhitespaceOffset == 0)
            {
                // /****|
                return " ";
            }

            var interiorFirstWhitespacePosition = firstNonWhitespacePosition + exteriorText.Length;
            if (interiorFirstNonWhitespaceOffset == -1 || caretPosition <= interiorFirstWhitespacePosition + interiorFirstNonWhitespaceOffset)
            {
                // *  |
                // or
                // *  |  1.
                //  ^^
                return snapshotLine.Snapshot.GetText(interiorFirstWhitespacePosition, caretPosition - interiorFirstWhitespacePosition);
            }
            else
            {
                // *   1. |
                //  ^^^
                return snapshotLine.Snapshot.GetText(interiorFirstWhitespacePosition, interiorFirstNonWhitespaceOffset);
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

            return trivia.RawKind == (int)SyntaxKind.MultiLineCommentTrivia;
        }
    }
}
