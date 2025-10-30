// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

internal static class CSharpSnippetHelpers
{
    public static int GetTargetCaretPositionInBlock<TTargetNode>(TTargetNode caretTarget, Func<TTargetNode, BlockSyntax> getBlock, SourceText sourceText)
        where TTargetNode : SyntaxNode
    {
        var block = getBlock(caretTarget);

        var triviaSpan = block.CloseBraceToken.LeadingTrivia.Span;
        var line = sourceText.Lines.GetLineFromPosition(triviaSpan.Start);
        // Getting the location at the end of the line before the newline.
        return line.Span.End;
    }

    public static string GetBlockLikeIndentationString(Document document, int startPositionOfOpenCurlyBrace, SyntaxFormattingOptions syntaxFormattingOptions, CancellationToken cancellationToken)
    {
        var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);
        var openBraceLine = parsedDocument.Text.Lines.GetLineFromPosition(startPositionOfOpenCurlyBrace).LineNumber;

        var indentationOptions = new IndentationOptions(syntaxFormattingOptions);
        var newLine = indentationOptions.FormattingOptions.NewLine;

        var indentationService = parsedDocument.LanguageServices.GetRequiredService<IIndentationService>();
        var indentation = indentationService.GetIndentation(parsedDocument, openBraceLine, indentationOptions, cancellationToken);

        // Adding the offset calculated with one tab so that it is indented once past the line containing the opening brace
        var newIndentation = new IndentationResult(indentation.BasePosition, indentation.Offset + syntaxFormattingOptions.TabSize);
        return newIndentation.GetIndentationString(parsedDocument.Text, syntaxFormattingOptions.UseTabs, syntaxFormattingOptions.TabSize) + newLine;
    }

    public static async Task<Document> AddBlockIndentationToDocumentAsync<TTargetNode>(
        Document document, TTargetNode targetNode, Func<TTargetNode, BlockSyntax> getBlock, CancellationToken cancellationToken)
        where TTargetNode : SyntaxNode
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var block = getBlock(targetNode);

        var syntaxFormattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var indentationString = GetBlockLikeIndentationString(document, block.SpanStart, syntaxFormattingOptions, cancellationToken);

        var updatedBlock = block.WithCloseBraceToken(block.CloseBraceToken.WithPrependedLeadingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, indentationString)));
        var updatedTargetStatement = targetNode.ReplaceNode(block, updatedBlock);

        var newRoot = root.ReplaceNode(targetNode, updatedTargetStatement);
        return document.WithSyntaxRoot(newRoot);
    }
}
