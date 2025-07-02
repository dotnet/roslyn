// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
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
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

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
        var cancellationToken = context.OperationContext.UserCancellationToken;

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

            // We must have at least one following quote, as we only got into ExecuteReturnCommandBeforeQuoteCharacter
            // if there was a quote character in front of it.
            Debug.Assert(quotesAfter > 0);

            for (var i = position - 1; i >= 0; i--)
            {
                if (currentSnapshot[i] != '"')
                    break;

                quotesBefore++;
            }

            // We support two cases here.  Something simple like `"""$$"""`.  In this case, we have to be hitting enter
            // inside balanced quotes.  But we also support `"""goo$$"""`.  In this case it's ok if quotes are not
            // balanced.  We're going to go through the non-empty path involving adding multiple newlines to the final
            // text.

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
                                     SyntaxKind.InterpolatedMultiLineRawStringStartToken) ||
                token.Parent is not ExpressionSyntax expression)
            {
                return false;
            }

            if (!isEmpty)
            {
                // in the non empty case (e.g. `"""goo$$"""`) we have to make sure sure that the caret is before the
                // final quotes, not the initial ones.
                if (token.Span.End - quotesAfter != position)
                    return false;
            }

            return MakeEdit(parsedDocument, expression, isEmpty);
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
            ExpressionSyntax expression;
            switch (token.Kind())
            {
                case SyntaxKind.SingleLineRawStringLiteralToken when token.Parent is ExpressionSyntax parentExpression:
                    expression = parentExpression;
                    break;

                case SyntaxKind.InterpolatedStringTextToken:
                case SyntaxKind.OpenBraceToken:
                    if (token is not
                        {
                            Parent.Parent: InterpolatedStringExpressionSyntax
                            {
                                StringStartToken.RawKind: (int)SyntaxKind.InterpolatedSingleLineRawStringStartToken,
                            } interpolatedStringExpression,
                        })
                    {
                        return false;
                    }

                    if (token.Kind() is SyntaxKind.OpenBraceToken && position != token.SpanStart)
                        return false;

                    expression = interpolatedStringExpression;
                    break;

                default:
                    return false;
            }

            return MakeEdit(parsedDocument, expression, isEmpty: false);
        }

        bool MakeEdit(
            ParsedDocument parsedDocument,
            ExpressionSyntax expression,
            bool isEmpty)
        {
            var project = document.Project;
            var indentationOptions = subjectBuffer.GetIndentationOptions(_editorOptionsService, project.GetFallbackAnalyzerOptions(), project.Services, explicitFormat: false);
            var indentation = expression.GetFirstToken().GetPreferredIndentation(parsedDocument, indentationOptions, cancellationToken);

            var newLine = indentationOptions.FormattingOptions.NewLine;

            using var transaction = CaretPreservingEditTransaction.TryCreate(
                CSharpEditorResources.Split_raw_string, textView, _undoHistoryRegistry, _editorOperationsFactoryService);
            var edit = subjectBuffer.CreateEdit();

            if (isEmpty)
            {
                // If the literal is empty, we just want to help the user transform it into a multiline raw string
                // literal with the extra empty newline between the delimiters to place the caret at
                edit.Insert(position, newLine + newLine + indentation);

                var finalSnapshot = edit.Apply();

                // move caret to the right location in virtual space for the blank line we added.
                var lineInNewSnapshot = finalSnapshot.GetLineFromPosition(position);
                var nextLine = finalSnapshot.GetLineFromLineNumber(lineInNewSnapshot.LineNumber + 1);
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
                var closingStart = GetStartPositionOfClosingDelimiter(expression);
                edit.Insert(closingStart, newLineAndIndentation);

                // Add a newline at the caret's position, to insert the newline that the user requested
                edit.Insert(position, newLineAndIndentation);

                // Also add a newline at the start of the text, only if there is text before the caret's position
                var insertedLinesBeforeCaret = 1;
                var openingEnd = GetEndPositionOfOpeningDelimiter(expression);
                if (openingEnd != position)
                {
                    insertedLinesBeforeCaret = 2;
                    edit.Insert(openingEnd, newLineAndIndentation);
                }

                var finalSnapshot = edit.Apply();

                var lineInNewSnapshot = finalSnapshot.GetLineFromPosition(openingEnd);
                var nextLine = finalSnapshot.GetLineFromLineNumber(lineInNewSnapshot.LineNumber + insertedLinesBeforeCaret);
                textView.Caret.MoveTo(new VirtualSnapshotPoint(nextLine, indentation.Length));
            }

            transaction?.Complete();
            return true;
        }

        int GetStartPositionOfClosingDelimiter(ExpressionSyntax expression)
        {
            if (expression is InterpolatedStringExpressionSyntax interpolatedStringExpression)
                return interpolatedStringExpression.StringEndToken.Span.Start;

            var index = expression.Span.End;
            while (currentSnapshot[index - 1] == '"')
                index--;

            return index;
        }

        int GetEndPositionOfOpeningDelimiter(ExpressionSyntax expression)
        {
            if (expression is InterpolatedStringExpressionSyntax interpolatedStringExpression)
                return interpolatedStringExpression.StringStartToken.Span.End;

            var index = expression.Span.Start;
            while (currentSnapshot[index] == '"')
                index++;

            return index;
        }
    }
}
