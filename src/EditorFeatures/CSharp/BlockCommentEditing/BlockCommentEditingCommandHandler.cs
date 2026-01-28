// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
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

namespace Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentEditing;

[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.CSharpContentType)]
[Name(nameof(BlockCommentEditingCommandHandler))]
[Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
internal sealed class BlockCommentEditingCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
{
    private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
    private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;
    private readonly EditorOptionsService _editorOptionsService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public BlockCommentEditingCommandHandler(
        ITextUndoHistoryRegistry undoHistoryRegistry,
        IEditorOperationsFactoryService editorOperationsFactoryService,
        EditorOptionsService editorOptionsService)
    {
        Contract.ThrowIfNull(undoHistoryRegistry);
        Contract.ThrowIfNull(editorOperationsFactoryService);

        _undoHistoryRegistry = undoHistoryRegistry;
        _editorOperationsFactoryService = editorOperationsFactoryService;
        _editorOptionsService = editorOptionsService;
    }

    public string DisplayName => EditorFeaturesResources.Block_Comment_Editing;

    public CommandState GetCommandState(ReturnKeyCommandArgs args)
        => CommandState.Unspecified;

    public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
        => TryHandleReturnKey(args.SubjectBuffer, args.TextView, context.OperationContext.UserCancellationToken);

    private bool TryHandleReturnKey(ITextBuffer subjectBuffer, ITextView textView, CancellationToken cancellationToken)
    {
        if (!_editorOptionsService.GlobalOptions.GetOption(BlockCommentEditingOptionsStorage.AutoInsertBlockCommentStartString, LanguageNames.CSharp))
            return false;

        var caretPosition = textView.GetCaretPoint(subjectBuffer);
        if (caretPosition == null)
            return false;

        var textToInsert = GetTextToInsert(caretPosition.Value, subjectBuffer, _editorOptionsService, cancellationToken);
        if (textToInsert == null)
            return false;

        using var transaction = _undoHistoryRegistry.GetHistory(textView.TextBuffer).CreateTransaction(EditorFeaturesResources.Insert_new_line);

        var editorOperations = _editorOperationsFactoryService.GetEditorOperations(textView);
        editorOperations.ReplaceText(GetReplacementSpan(caretPosition.Value), textToInsert);

        transaction.Complete();
        return true;
    }

    private static Span GetReplacementSpan(SnapshotPoint caretPosition)
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

    private static string? GetTextToInsert(SnapshotPoint caretPosition, ITextBuffer buffer, EditorOptionsService editorOptionsService, CancellationToken cancellationToken)
    {
        var currentLine = caretPosition.GetContainingLine();
        var firstNonWhitespacePosition = currentLine.GetFirstNonWhitespacePosition() ?? -1;
        if (firstNonWhitespacePosition == -1)
            return null;

        // Do quick textual checks to see if it looks like we're inside a comment. That way we only do the expensive
        // syntactic work when necessary.
        //
        // The line either has to contain `/*` or it has to start with `*`.  The former looks like we're starting a
        // comment in this line.  The latter looks like the continuation of a block comment.
        var containsBlockCommentStartString = currentLine.Contains(firstNonWhitespacePosition, "/*", ignoreCase: false);
        var startsWithBlockCommentMiddleString = currentLine.StartsWith(firstNonWhitespacePosition, "*", ignoreCase: false);

        if (!containsBlockCommentStartString &&
            !startsWithBlockCommentMiddleString)
        {
            return null;
        }

        // Now do more expensive syntactic check to see if we're actually in the block comment.
        if (!IsCaretInsideBlockCommentSyntax(caretPosition, buffer, editorOptionsService, out var blockComment, out var newLine, cancellationToken))
            return null;

        var textSnapshot = caretPosition.Snapshot;

        // Now that we've found the real start of the comment, ensure that it's accurate with our quick textual check.
        containsBlockCommentStartString = currentLine.LineNumber == textSnapshot.GetLineFromPosition(blockComment.FullSpan.Start).LineNumber;

        // The whitespace indentation on the line where the block-comment starts.
        var commentIndentation = GetCommentIndentation();

        // The whitespace indentation on the current line up to the first non-whitespace char.
        var lineIndentation = textSnapshot.GetText(Span.FromBounds(
            currentLine.Start,
            firstNonWhitespacePosition));

        var exteriorText = GetExteriorText();
        if (exteriorText == null)
            return null;

        return newLine + exteriorText;

        string GetCommentIndentation()
        {
            using var _ = PooledStringBuilder.GetInstance(out var sb);

            var commentStart = blockComment.FullSpan.Start;
            var commentLine = textSnapshot.GetLineFromPosition(commentStart);
            for (var i = commentLine.Start.Position; i < commentStart; i++)
            {
                var ch = textSnapshot[i];
                sb.Append(ch == '\t' ? ch : ' ');
            }

            return sb.ToString();
        }

        string? GetExteriorText()
        {
            if (containsBlockCommentStartString)
                return GetExteriorTextAfterBlockCommentStart();

            var startsWithBlockCommentEndString = currentLine.StartsWith(firstNonWhitespacePosition, "*/", ignoreCase: false);
            if (startsWithBlockCommentEndString)
                return GetExteriorTextBeforeBlockCommentEnd();

            if (startsWithBlockCommentMiddleString)
                return GetExteriorTextInBlockCommentMiddle();

            return null;
        }

        string? GetExteriorTextAfterBlockCommentStart()
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
                // /*|    or  /*   |
                //
                // In the latter case, keep the whitespace the user has typed.  in the former, insert at least one
                // space. This is the idiomatic style for C#.
                var whitespace = GetWhitespaceBetweenCommentAsteriskAndCaret();
                return commentIndentation + " *" + (whitespace == "" ? " " : whitespace);
            }
        }

        string? GetExteriorTextBeforeBlockCommentEnd()
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

        string? GetExteriorTextInBlockCommentMiddle()
        {
            if (BlockCommentEndsRightAfterCaret(caretPosition))
            {
                //      *|*/
                return lineIndentation;
            }
            else if (caretPosition > firstNonWhitespacePosition)
            {
                //     /*
                //      *
                //      *|
                //
                // We don't add a space here. If the user isn't adding spaces at this point, we respect that and
                // continue with that style.
                return lineIndentation + "*" + GetWhitespaceBetweenCommentAsteriskAndCaret();
            }
            else
            {
                //      /*
                //   |   *
                return commentIndentation + " ";
            }
        }

        // Returns the whitespace after the * in either '/*' or just '*' and the caret.
        string GetWhitespaceBetweenCommentAsteriskAndCaret()
        {
            var currentChar = containsBlockCommentStartString
                ? blockComment.FullSpan.Start
                : firstNonWhitespacePosition;

            if (textSnapshot[currentChar] == '/')
                currentChar++;

            if (textSnapshot[currentChar] == '*')
                currentChar++;

            var start = currentChar;
            while (currentChar < caretPosition && SyntaxFacts.IsWhitespace(textSnapshot[currentChar]))
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
        SnapshotPoint caretPosition,
        ITextBuffer buffer,
        EditorOptionsService editorOptionsService,
        out SyntaxTrivia trivia,
        [NotNullWhen(true)] out string? newLine,
        CancellationToken cancellationToken)
    {
        trivia = default;
        newLine = null;

        var snapshot = caretPosition.Snapshot;
        var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
            return false;

        var syntaxTree = document.GetRequiredSyntaxTreeSynchronously(cancellationToken);
        trivia = syntaxTree.FindTriviaAndAdjustForEndOfFile(caretPosition, cancellationToken);

        var isBlockComment = trivia.Kind() is SyntaxKind.MultiLineCommentTrivia or SyntaxKind.MultiLineDocumentationCommentTrivia;
        if (isBlockComment)
        {
            newLine = buffer.GetLineFormattingOptions(editorOptionsService, explicitFormat: false).NewLine;

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
