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

        if (currentSnapshot[position] == '"')
        {
            return HandleCaretOnQuote(textView, subjectBuffer, span, position, context.OperationContext.UserCancellationToken);
        }

        return HandleCaretNotOnQuote(textView, subjectBuffer, span, position, context.OperationContext.UserCancellationToken);
    }

    private bool HandleCaretNotOnQuote(ITextView textView, ITextBuffer subjectBuffer, SnapshotSpan span, int position, CancellationToken cancellationToken)
    {
        // If the caret is not on a quote, we need to find whether we are within the contents of a single-line raw string literal
        // but not inside an interpolation
        // If we are inside a raw string literal and the caret is not on top of a quote, it is part of the literal's text
        // Here we try to ensure that the literal's closing quotes are properly placed in their own line
        // We could reach this point after pressing enter on a single-line raw string

        var currentSnapshot = subjectBuffer.CurrentSnapshot;
        var document = currentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
            return false;

        var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);

        var token = parsedDocument.Root.FindToken(position);
        switch (token.Kind())
        {
            case SyntaxKind.SingleLineRawStringLiteralToken:
                break;

            case SyntaxKind.InterpolatedStringTextToken
            when token.Parent?.Parent is InterpolatedStringExpressionSyntax interpolated &&
                interpolated.StringStartToken.Kind() is SyntaxKind.InterpolatedSingleLineRawStringStartToken:
                break;

            default:
                return false;
        }

        var indentationOptions = subjectBuffer.GetIndentationOptions(_editorOptionsService, document.Project.GetFallbackAnalyzerOptions(), document.Project.Services, explicitFormat: false);
        var indentation = token.GetPreferredIndentation(parsedDocument, indentationOptions, cancellationToken);

        var newLine = indentationOptions.FormattingOptions.NewLine;

        using var transaction = CaretPreservingEditTransaction.TryCreate(
            CSharpEditorResources.Split_raw_string, textView, _undoHistoryRegistry, _editorOperationsFactoryService);

        var edit = subjectBuffer.CreateEdit();

        // Add a newline at the position of the end literal
        var closingStart = GetStartPositionOfClosingDelimiter(token);
        var newLineAndIndentation = newLine + indentation;
        var insertedLines = 1;
        edit.Insert(closingStart, newLineAndIndentation);
        // Add a newline at the requested position
        edit.Insert(position, newLineAndIndentation);
        // Also add a newline at the start of the text, only if there is text before the requested position
        var openingEnd = GetEndPositionOfOpeningDelimiter(token);
        if (openingEnd != position)
        {
            insertedLines++;
            edit.Insert(openingEnd, newLineAndIndentation);
        }
        var snapshot = edit.Apply();

        // move caret:
        var lineInNewSnapshot = snapshot.GetLineFromPosition(openingEnd);
        var nextLine = snapshot.GetLineFromLineNumber(lineInNewSnapshot.LineNumber + insertedLines);
        textView.Caret.MoveTo(new VirtualSnapshotPoint(nextLine, indentation.Length));

        transaction?.Complete();
        return true;
    }

    private static int GetEndPositionOfOpeningDelimiter(SyntaxToken currentStringLiteralToken)
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
                    var index = 0;
                    while (index < length)
                    {
                        var c = text[index];
                        if (c != '"')
                            return tokenStart + index;
                        index++;
                    }

                    Contract.Fail("This should only be triggered by raw string literals that contain text aside from the double quotes");
                    return -1;
                }

            case SyntaxKind.InterpolatedStringTextToken:
                var tokenParent = currentStringLiteralToken.Parent?.Parent;
                if (tokenParent is not InterpolatedStringExpressionSyntax interpolatedStringExpression)
                {
                    Contract.Fail("This token should only be contained in an interpolated string text syntax");
                    return -1;
                }

                return interpolatedStringExpression.StringStartToken.Span.End;

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
                    while (index > 0)
                    {
                        var c = text[index];
                        if (c != '"')
                            return tokenStart + index + 1;
                        index--;
                    }

                    Contract.Fail("This should only be triggered by raw string literals that contain text aside from the double quotes");
                    return -1;
                }

            case SyntaxKind.InterpolatedStringTextToken:
                var tokenParent = currentStringLiteralToken.Parent?.Parent;
                if (tokenParent is not InterpolatedStringExpressionSyntax interpolatedStringExpression)
                {
                    Contract.Fail("This token should only be contained in an interpolated string text syntax");
                    return -1;
                }

                return interpolatedStringExpression.StringEndToken.SpanStart;

            default:
                Contract.Fail("This should only be triggered on a known raw string literal kind");
                return -1;
        }
    }

    private bool HandleCaretOnQuote(ITextView textView, ITextBuffer subjectBuffer, SnapshotSpan span, int position, CancellationToken cancellationToken)
    {
        var quotesBefore = 0;
        var quotesAfter = 0;

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

        if (quotesAfter != quotesBefore)
            return false;

        if (quotesAfter < 3)
            return false;

        return SplitRawString(textView, subjectBuffer, span.Start.Position, cancellationToken);
    }

    private bool SplitRawString(ITextView textView, ITextBuffer subjectBuffer, int position, CancellationToken cancellationToken)
    {
        var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
            return false;

        var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);

        var token = parsedDocument.Root.FindToken(position);
        if (token.Kind() is not (SyntaxKind.SingleLineRawStringLiteralToken or
                                 SyntaxKind.MultiLineRawStringLiteralToken or
                                 SyntaxKind.InterpolatedSingleLineRawStringStartToken or
                                 SyntaxKind.InterpolatedMultiLineRawStringStartToken))
        {
            return false;
        }

        var indentationOptions = subjectBuffer.GetIndentationOptions(_editorOptionsService, document.Project.GetFallbackAnalyzerOptions(), document.Project.Services, explicitFormat: false);
        var indentation = token.GetPreferredIndentation(parsedDocument, indentationOptions, cancellationToken);

        var newLine = indentationOptions.FormattingOptions.NewLine;

        using var transaction = CaretPreservingEditTransaction.TryCreate(
            CSharpEditorResources.Split_raw_string, textView, _undoHistoryRegistry, _editorOperationsFactoryService);

        var edit = subjectBuffer.CreateEdit();

        // apply the change:
        edit.Insert(position, newLine + newLine + indentation);
        var snapshot = edit.Apply();

        // move caret:
        var lineInNewSnapshot = snapshot.GetLineFromPosition(position);
        var nextLine = snapshot.GetLineFromLineNumber(lineInNewSnapshot.LineNumber + 1);
        textView.Caret.MoveTo(new VirtualSnapshotPoint(nextLine, indentation.Length));

        transaction?.Complete();
        return true;
    }
}
