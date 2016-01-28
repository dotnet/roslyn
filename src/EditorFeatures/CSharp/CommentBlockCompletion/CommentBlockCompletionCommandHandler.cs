// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.CommentBlockCompletion;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Editor.CSharp.CommentBlockCompletion
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.CommentBlockCompletion, ContentTypeNames.CSharpContentType)]
    internal class CommentBlockCompletionCommandHandler : AbstractCommentBlockCompletionCommandHandler
    {
        [ImportingConstructor]
        public CommentBlockCompletionCommandHandler(
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

            var isCurrentLineStartsWithCommentBlockStart = snapshotLine.StartsWith(firstNonWhitespacePosition, "/*", false);
            var isCurrentLineStartsWithCommentBlockEnd = snapshotLine.StartsWith(firstNonWhitespacePosition, "*/", false);
            var isCurrentLineStartsWithCommentBlockMiddle = snapshotLine.StartsWith(firstNonWhitespacePosition, "*", false);

            if (!isCurrentLineStartsWithCommentBlockStart && !isCurrentLineStartsWithCommentBlockMiddle)
            {
                return null;
            }

            if (!IsCaretInsideCommentBlockSyntax(caretPosition))
            {
                return null;
            }

            if (isCurrentLineStartsWithCommentBlockStart)
            {
                if (IsCommentBlockEndsAfterCaret(caretPosition))
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

            if (isCurrentLineStartsWithCommentBlockEnd)
            {
                if (IsCommentBlockEndsAfterCaret(caretPosition))
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

            if (isCurrentLineStartsWithCommentBlockMiddle)
            {
                if (IsCommentBlockEndsAfterCaret(caretPosition))
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

        private static bool IsCommentBlockEndsAfterCaret(SnapshotPoint caretPosition) => caretPosition.Snapshot.GetText(caretPosition, 2) == "*/";

        private static string GetPaddingOrIndentation(ITextSnapshotLine snapshotLine, int caretPosition, int firstNonWhitespacePosition, string exteriorText)
        {
            Debug.Assert(caretPosition >= firstNonWhitespacePosition + exteriorText.Length);

            var firstNonWhitespaceOffset = firstNonWhitespacePosition - snapshotLine.Start;
            Debug.Assert(firstNonWhitespaceOffset > -1);

            var lineText = snapshotLine.GetText();
            if ((lineText.Length == firstNonWhitespaceOffset + exteriorText.Length))
            {
                return " ";
            }

            var interiorText = lineText.Substring(firstNonWhitespaceOffset + exteriorText.Length);
            var interiorFirstNonWhitespaceOffset = interiorText.GetFirstNonWhitespaceOffset() ?? -1;

            if (interiorFirstNonWhitespaceOffset == -1)
            {
                return " ";
            }

            var interiorFirstWhitespacePosition = firstNonWhitespacePosition + exteriorText.Length;
            if (caretPosition <= interiorFirstWhitespacePosition + interiorFirstNonWhitespaceOffset)
            {
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

        private static bool IsCaretInsideCommentBlockSyntax(SnapshotPoint caretPosition)
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
