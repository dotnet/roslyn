﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
    internal sealed class CSharpIfSnippetProvider : AbstractIfSnippetProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpIfSnippetProvider()
        {
        }

        protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget, SourceText sourceText)
        {
            var ifStatement = (IfStatementSyntax)caretTarget;
            var blockStatement = (BlockSyntax)ifStatement.Statement;

            var triviaSpan = blockStatement.CloseBraceToken.LeadingTrivia.Span;
            var line = sourceText.Lines.GetLineFromPosition(triviaSpan.Start);
            // Getting the location at the end of the line before the newline.
            return line.Span.End;
        }

        protected override SyntaxNode GetCondition(SyntaxNode node)
        {
            var ifStatement = (IfStatementSyntax)node;
            return ifStatement.Condition;
        }

        private static string GetIndentation(Document document, IfStatementSyntax ifStatementSyntax, SyntaxFormattingOptions syntaxFormattingOptions, CancellationToken cancellationToken)
        {
            var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);
            var openBraceLine = parsedDocument.Text.Lines.GetLineFromPosition(ifStatementSyntax.Statement.SpanStart).LineNumber;

            var indentationOptions = new IndentationOptions(syntaxFormattingOptions);
            var newLine = indentationOptions.FormattingOptions.NewLine;

            var indentationService = parsedDocument.LanguageServices.GetRequiredService<IIndentationService>();
            var indentation = indentationService.GetIndentation(parsedDocument, openBraceLine + 1, indentationOptions, cancellationToken);

            // Adding the offset calculated with one tab so that it is indented once past the line containing the opening brace
            var newIndentation = new IndentationResult(indentation.BasePosition, indentation.Offset + syntaxFormattingOptions.TabSize);
            return newIndentation.GetIndentationString(parsedDocument.Text, syntaxFormattingOptions.UseTabs, syntaxFormattingOptions.TabSize) + newLine;
        }

        protected override async Task<Document> AddIndentationToDocumentAsync(Document document, int position, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var snippet = root.GetAnnotatedNodes(_findSnippetAnnotation).FirstOrDefault();

            if (snippet is not IfStatementSyntax ifStatementSyntax)
                return document;

            var syntaxFormattingOptions = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions: null, cancellationToken).ConfigureAwait(false);
            var indentationString = GetIndentation(document, ifStatementSyntax, syntaxFormattingOptions, cancellationToken);

            var blockStatement = (BlockSyntax)ifStatementSyntax.Statement;
            blockStatement = blockStatement.WithCloseBraceToken(blockStatement.CloseBraceToken.WithPrependedLeadingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, indentationString)));
            var newIfStatementSyntax = ifStatementSyntax.ReplaceNode(ifStatementSyntax.Statement, blockStatement);

            var newRoot = root.ReplaceNode(ifStatementSyntax, newIfStatementSyntax);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
