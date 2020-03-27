// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Formatting;
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
                return false;

            var caretPosition = textView.GetCaretPoint(subjectBuffer);
            if (caretPosition == null)
                return false;

            var textToInsert = GetTextToInsert(caretPosition.Value);
            if (textToInsert == null)
                return false;

            using var transaction = _undoHistoryRegistry.GetHistory(textView.TextBuffer).CreateTransaction(EditorFeaturesResources.Insert_new_line);

            var editorOperations = _editorOperationsFactoryService.GetEditorOperations(textView);
            editorOperations.ReplaceText(GetReplacementSpan(caretPosition.Value), textToInsert);

            transaction.Complete();
            return true;
        }

        private Span GetReplacementSpan(SnapshotPoint caretPosition)
        {
            // We want to replace all the whitespace following the caret.  This is standard <enter> behavior in VS that
            // we want to mimic.
            var snapshot = caretPosition.Snapshot;
            var start = caretPosition.Position;
            var end = caretPosition;
            while (end < snapshot.Length && SyntaxFacts.IsWhitespace(end.GetChar()) && !SyntaxFacts.IsNewLine(end.GetChar()))
                end = end + 1;

            return Span.FromBounds(start, end);
        }

        private string GetTextToInsert(SnapshotPoint caretPosition)
        {
            var currentLine = caretPosition.GetContainingLine();
            var firstNonWhitespacePosition = currentLine.GetFirstNonWhitespacePosition() ?? -1;
            if (firstNonWhitespacePosition == -1)
                return null;

            var startsWithBlockCommentStartString = currentLine.StartsWith(firstNonWhitespacePosition, "/*", ignoreCase: false);
            var startsWithBlockCommentEndString = currentLine.StartsWith(firstNonWhitespacePosition, "*/", ignoreCase: false);
            var startsWithBlockCommentMiddleString = currentLine.StartsWith(firstNonWhitespacePosition, "*", ignoreCase: false);

            if (!startsWithBlockCommentStartString &&
                !startsWithBlockCommentMiddleString)
            {
                return null;
            }

            if (!IsCaretInsideBlockCommentSyntax(caretPosition, out var document, out var blockComment))
                return null;

            var textSnapshot = caretPosition.Snapshot;

            // The whitespace indentation on the line where the block-comment starts.
            var commentIndentation = textSnapshot.GetText(Span.FromBounds(
                textSnapshot.GetLineFromPosition(blockComment.FullSpan.Start).Start,
                blockComment.FullSpan.Start));

            // The whitespace indentation on the current line up to the first non-whitespace char.
            var lineIndentation = textSnapshot.GetText(Span.FromBounds(
                currentLine.Start,
                firstNonWhitespacePosition));

            var exteriorText = GetExteriorText();
            if (exteriorText == null)
                return null;

            var options = document.Project.Solution.Options;
            var newLine = options.GetOption(FormattingOptions.NewLine, LanguageNames.CSharp);
            return newLine + exteriorText;

            string GetExteriorText()
            {
                if (startsWithBlockCommentStartString)
                {
                    if (BlockCommentEndsRightAfterCaret(caretPosition))
                    {
                        //      /*|*/
                        return commentIndentation + " ";
                    }
                    else if (caretPosition == firstNonWhitespacePosition + 1)
                    {
                        //      /|*
                        return null; // The newline inserted could break the syntax in a way that this handler cannot fix, let's leave it.
                    }
                    else
                    {
                        //      /*|
                        // This is directly after the comment starts.  Insert ' * ' to continue the comment and to put
                        // the user one space in.  This is the idiomatic style for C#.  Note: if the user is hitting
                        // enter after
                        //
                        //  /*
                        //   *
                        //   *$$
                        //
                        // Then we don't add the space.  In this case, they are indicating they don't want this extra
                        // space added.
                        var padding = GetPaddingAfterCommentCharacter();
                        return commentIndentation + " *" + (padding == "" ? " " : padding);
                    }
                }

                if (startsWithBlockCommentEndString)
                {
                    if (BlockCommentEndsRightAfterCaret(caretPosition))
                    {
                        //      /*
                        //      |*/
                        return commentIndentation + " ";
                    }
                    else if (caretPosition == firstNonWhitespacePosition + 1)
                    {
                        //      *|/
                        return lineIndentation + "*";
                    }
                    else
                    {
                        //      /*
                        //   |   */
                        return commentIndentation + " ";
                    }
                }

                if (startsWithBlockCommentMiddleString)
                {
                    if (BlockCommentEndsRightAfterCaret(caretPosition))
                    {
                        //      *|*/
                        return lineIndentation;
                    }
                    else if (caretPosition > firstNonWhitespacePosition)
                    {
                        //      *|
                        return lineIndentation + "*" + GetPaddingAfterCommentCharacter();
                    }
                    else
                    {
                        //      /*
                        //   |   *
                        return commentIndentation + " ";
                    }
                }

                return null;
            }

            string GetPaddingAfterCommentCharacter()
            {
                var currentChar = firstNonWhitespacePosition;
                Debug.Assert(textSnapshot[currentChar] == '/' || textSnapshot[currentChar] == '*');

                // Skip past the first comment char.
                currentChar++;

                // Skip past any banner of ****'s 
                while (currentChar < caretPosition && textSnapshot[currentChar] == '*')
                    currentChar++;

                var start = currentChar;
                while (currentChar < caretPosition && char.IsWhiteSpace(textSnapshot[currentChar]))
                    currentChar++;

                return textSnapshot.GetText(Span.FromBounds(start, currentChar));
            }
        }

        private static bool BlockCommentEndsRightAfterCaret(SnapshotPoint caretPosition)
        {
            var snapshot = caretPosition.Snapshot;
            return (int)caretPosition + 2 <= snapshot.Length && snapshot.GetText(caretPosition, 2) == "*/";
        }

        public static bool IsCaretInsideBlockCommentSyntax(
            SnapshotPoint caretPosition, out Document document, out SyntaxTrivia trivia)
        {
            trivia = default;

            var snapshot = caretPosition.Snapshot;
            document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return false;

            var syntaxTree = document.GetSyntaxTreeSynchronously(CancellationToken.None);
            trivia = syntaxTree.FindTriviaAndAdjustForEndOfFile(caretPosition, CancellationToken.None);

            var isBlockComment = trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);
            if (isBlockComment)
            {
                var span = trivia.FullSpan;
                if (span.Start < caretPosition && caretPosition < span.End)
                    return true;

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
                        return true;

                    // If the block comment is not closed, SyntaxTrivia contains diagnostics
                    // So when the SyntaxTrivia is clean, the block comment should be closed
                    if (!trivia.ContainsDiagnostics)
                        return false;

                    var textBeforeCaret = snapshot.GetText(caretPosition.Position - 2, 2);
                    return textBeforeCaret != "*/";
                }
            }

            return false;
        }
    }
}
