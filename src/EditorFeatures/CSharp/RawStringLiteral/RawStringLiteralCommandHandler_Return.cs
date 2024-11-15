// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.RawStringLiteral;

internal partial class RawStringLiteralCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
{
    public CommandState GetCommandState(ReturnKeyCommandArgs args)
        => CommandState.Unspecified;

    /// <summary>
    /// Checks to see if the user is typing <c>return</c> in <c>"""$$"""</c> and then properly indents the end
    /// delimiter of the raw string literal.
    /// </summary>
    public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
    {
        var textView = args.TextView;
        var subjectBuffer = args.SubjectBuffer;
        var spans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);

        if (spans.Count != 1)
            return false;

        var span = spans.First();
        if (span.Length != 0)
            return false;

        var caret = textView.GetCaretPoint(subjectBuffer);
        if (caret == null)
            return false;

        var position = caret.Value.Position;
        var currentSnapshot = subjectBuffer.CurrentSnapshot;
        if (position >= currentSnapshot.Length)
            return false;

        var cancellationToken = context.OperationContext.UserCancellationToken;

        var document = currentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
            return false;

        return currentSnapshot[position] == '"'
            ? ExecuteReturnCommandBeforeQuoteCharacter()
            : ExecuteReturnCommandNotBeforeQuoteCharacter();

        bool ExecuteReturnCommandBeforeQuoteCharacter()
        {
            var quotesBefore = 0;
            var quotesAfter = 0;

            // Ensure we're in between a balanced set of quotes, with at least 3 quotes on each side.

            var currentSnapshot = subjectBuffer.CurrentSnapshot;
            for (int i = position, n = currentSnapshot.Length; i < n; i++)
            {
                if (currentSnapshot[i] != '"')
                    break;

                quotesAfter++;
            }

            for (var i = position - 1; i >= 0; i--)
            {
                if (currentSnapshot[i] != '"')
                    break;

                quotesBefore++;
            }

            var isEmpty = quotesBefore > 0;
            if (isEmpty && quotesAfter != quotesBefore)
                return false;

            if (quotesAfter < 3)
                return false;

            // Looks promising based on text alone.  Now ensure we're actually on a raw string token/expression.
            var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);

            var token = parsedDocument.Root.FindToken(position);
            if (token.Kind() is not (SyntaxKind.SingleLineRawStringLiteralToken or
                                     SyntaxKind.MultiLineRawStringLiteralToken or
                                     SyntaxKind.InterpolatedSingleLineRawStringStartToken or
                                     SyntaxKind.InterpolatedMultiLineRawStringStartToken))
            {
                return false;
            }

            return MakeEdit(parsedDocument, token, preferredIndentationToken: token, isEmpty);
        }

        bool ExecuteReturnCommandNotBeforeQuoteCharacter()
        {
            // If the caret is not on a quote, we need to find whether we are within the contents of a single-line raw
            // string literal but not inside an interpolation. If we are inside a raw string literal and the caret is not on
            // top of a quote, it is part of the literal's text. Here we try to ensure that the literal's closing quotes are
            // properly placed in their own line We could reach this point after pressing enter within a single-line raw
            // string

            var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);

            var token = parsedDocument.Root.FindToken(position);
            var preferredIndentationToken = token;
            switch (token.Kind())
            {
                case SyntaxKind.SingleLineRawStringLiteralToken:
                    break;

                case SyntaxKind.InterpolatedStringTextToken:
                case SyntaxKind.OpenBraceToken:
                    if (token.Parent?.Parent is not InterpolatedStringExpressionSyntax interpolated ||
                        interpolated.StringStartToken.Kind() is not SyntaxKind.InterpolatedSingleLineRawStringStartToken)
                    {
                        return false;
                    }

                    if (token.Kind() is SyntaxKind.OpenBraceToken)
                    {
                        // If we are not at the start of the interpolation braces, we do not intend to handle converting the raw string
                        // into a new one
                        if (position != token.SpanStart)
                            return false;

                        // We prefer the indentation options of the string start delimiter because the indentation of the interpolation
                        // is empty and thus we cannot properly indent the lines that we insert
                        preferredIndentationToken = interpolated.StringStartToken;
                    }

                    break;

                default:
                    return false;
            }

            return MakeEdit(parsedDocument, token, preferredIndentationToken, isEmpty: false);
        }

        bool MakeEdit(
            ParsedDocument parsedDocument,
            SyntaxToken token,
            SyntaxToken preferredIndentationToken,
            bool isEmpty)
        {
            var project = document.Project;
            var indentationOptions = subjectBuffer.GetIndentationOptions(_editorOptionsService, project.GetFallbackAnalyzerOptions(), project.Services, explicitFormat: false);
            var indentation = preferredIndentationToken.GetPreferredIndentation(parsedDocument, indentationOptions, cancellationToken);

            var newLine = indentationOptions.FormattingOptions.NewLine;

            using var transaction = CaretPreservingEditTransaction.TryCreate(
                CSharpEditorResources.Split_raw_string, textView, _undoHistoryRegistry, _editorOperationsFactoryService);
            var edit = subjectBuffer.CreateEdit();

            var openingEnd = GetEndPositionOfOpeningDelimiter(token, isEmpty);

            if (isEmpty)
            {
                // If the literal is empty, we just want to help the user transform it into a multiline raw string
                // literal with the extra empty newline between the delimiters to place the caret at
                edit.Insert(position, newLine + newLine + indentation);

                var snapshot = edit.Apply();

                // move caret to the right location in virtual space for the blank line we added.
                var lineInNewSnapshot = snapshot.GetLineFromPosition(position);
                var nextLine = snapshot.GetLineFromLineNumber(lineInNewSnapshot.LineNumber + 1);
                textView.Caret.MoveTo(new VirtualSnapshotPoint(nextLine, indentation.Length));
            }
            else
            {
                // Otherwise, we're starting with a raw string that has content in it.  That's something like:
                // """GooBar""".  If we hit enter at the `G` we only want to insert a single new line before the caret.
                // However, if we were to hit enter anywhere after that, we want two new lines inserted.  One after the
                // `"""` and one at the caret itself.
                var newLineAndIndentation = newLine + indentation;

                // Add a newline at the position of the end literal
                var closingStart = GetStartPositionOfClosingDelimiter(token);
                edit.Insert(closingStart, newLineAndIndentation);

                // Add a newline at the caret's position, to insert the newline that the user requested
                edit.Insert(position, newLineAndIndentation);

                // Also add a newline at the start of the text, only if there is text before the caret's position
                var insertedLinesBeforeCaret = 1;
                if (openingEnd != position)
                {
                    insertedLinesBeforeCaret++;
                    edit.Insert(openingEnd, newLineAndIndentation);
                }

                var snapshot = edit.Apply();

                // move caret:
                var lineInNewSnapshot = snapshot.GetLineFromPosition(openingEnd);
                var nextLine = snapshot.GetLineFromLineNumber(lineInNewSnapshot.LineNumber + insertedLinesBeforeCaret);
                textView.Caret.MoveTo(new VirtualSnapshotPoint(nextLine, indentation.Length));
            }

            transaction?.Complete();
            return true;
        }
    }

    private static int GetEndPositionOfOpeningDelimiter(SyntaxToken currentStringLiteralToken, bool isEmpty)
    {
        switch (currentStringLiteralToken.Kind())
        {
            case SyntaxKind.SingleLineRawStringLiteralToken:
            case SyntaxKind.MultiLineRawStringLiteralToken:
                {
                    var text = currentStringLiteralToken.Text;
                    var tokenSpan = currentStringLiteralToken.Span;
                    var tokenStart = tokenSpan.Start;
                    var length = tokenSpan.Length;
                    // Traverse through the literal's text to discover the first position that is not a double quote
                    var index = 0;
                    while (index < length)
                    {
                        var c = text[index];
                        if (c != '"')
                        {
                            // If the literal is empty, we expect a continuous segment of double quotes
                            // where in the worst case a single quote is missing in the end
                            // So for example, """""" is an emtpy raw string literal where the user intends to delimit it
                            // with 3 double quotes, where the contents would be placed after the first 3 quotes only
                            var quotes = isEmpty ? index / 2 : index;
                            return tokenStart + quotes;
                        }

                        index++;
                    }

                    // We have evaluated an emtpy raw string literal here and so we split the continuous double quotes into the start and end delimiters
                    Contract.ThrowIfFalse(isEmpty);
                    return tokenStart + length / 2;
                }

            case SyntaxKind.InterpolatedStringTextToken:
            case SyntaxKind.OpenBraceToken:
            case SyntaxKind.CloseBraceToken:
                var tokenParent = currentStringLiteralToken.Parent?.Parent;
                if (tokenParent is not InterpolatedStringExpressionSyntax interpolatedStringExpression)
                {
                    Contract.Fail("This token should only be contained in an interpolated string expression syntax");
                    return -1;
                }

                return interpolatedStringExpression.StringStartToken.Span.End;

            case SyntaxKind.InterpolatedRawStringEndToken:
                return currentStringLiteralToken.SpanStart;

            // This represents the case of a seemingly empty single-line interpolated raw string literal
            // looking like this: $"""""", where all the quotes are parsed as the start delimiter
            // We handle this as an empty interpolated string, so we return the index at where the text would begin
            case SyntaxKind.InterpolatedSingleLineRawStringStartToken:
                {
                    var firstQuoteOffset = currentStringLiteralToken.Text.IndexOf('"');
                    var length = currentStringLiteralToken.Span.Length;
                    var quotes = length - firstQuoteOffset;
                    return currentStringLiteralToken.SpanStart + firstQuoteOffset + quotes / 2;
                }

            default:
                Contract.Fail("This should only be triggered on a known raw string literal kind");
                return -1;
        }
    }

    private static int GetStartPositionOfClosingDelimiter(SyntaxToken currentStringLiteralToken)
    {
        switch (currentStringLiteralToken.Kind())
        {
            case SyntaxKind.SingleLineRawStringLiteralToken:
            case SyntaxKind.MultiLineRawStringLiteralToken:
                {
                    var text = currentStringLiteralToken.Text;
                    var tokenSpan = currentStringLiteralToken.Span;
                    var tokenStart = tokenSpan.Start;
                    var index = tokenSpan.Length - 1;
                    // Traverse through the literal's text from the end to discover the first position that is not a double quote
                    while (index > 0)
                    {
                        var c = text[index];
                        if (c != '"')
                            return tokenStart + index + 1;
                        index--;
                    }

                    // We have evaluated an empty raw string literal here and so we split the continuous double quotes into the start and end delimiters
                    return tokenStart + tokenSpan.Length / 2;
                }

            case SyntaxKind.InterpolatedStringTextToken:
            case SyntaxKind.OpenBraceToken:
            case SyntaxKind.CloseBraceToken:
                var tokenParent = currentStringLiteralToken.Parent?.Parent;
                if (tokenParent is not InterpolatedStringExpressionSyntax interpolatedStringExpression)
                {
                    Contract.Fail("This token should only be contained in an interpolated string expression syntax");
                    return -1;
                }

                return interpolatedStringExpression.StringEndToken.SpanStart;

            case SyntaxKind.InterpolatedRawStringEndToken:
                return currentStringLiteralToken.SpanStart;

            // This represents the case of a seemingly empty single-line interpolated raw string literal
            // looking like this: $"""""", where all the quotes are parsed as the start delimiter
            // We handle this as an empty interpolated string, so we return the index at where the text would begin
            case SyntaxKind.InterpolatedSingleLineRawStringStartToken:
                {
                    var firstQuoteOffset = currentStringLiteralToken.Text.IndexOf('"');
                    var length = currentStringLiteralToken.Span.Length;
                    var quotes = length - firstQuoteOffset;
                    return currentStringLiteralToken.SpanStart + firstQuoteOffset + quotes / 2;
                }

            default:
                Contract.Fail("This should only be triggered on a known raw string literal kind");
                return -1;
        }
    }
}
