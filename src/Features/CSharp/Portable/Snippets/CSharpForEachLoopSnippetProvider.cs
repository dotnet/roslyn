// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
    internal sealed class CSharpForEachLoopSnippetProvider : AbstractForEachLoopSnippetProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpForEachLoopSnippetProvider()
        {
        }

        /// <summary>
        /// Creates the foreach statement syntax.
        /// Must be done in language specific file since there is no generic way to generate the syntax.
        /// </summary>
        protected override async Task<SyntaxNode> CreateForEachLoopStatementSyntaxAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var varIdentifier = SyntaxFactory.IdentifierName("var");
            var enumerationSymbol = semanticModel.LookupSymbols(position).FirstOrDefault(symbol => symbol.GetSymbolType() != null &&
                symbol.GetSymbolType()!.AllInterfaces.Any(
                    namedSymbol => namedSymbol.SpecialType is SpecialType.System_Collections_Generic_IEnumerable_T or SpecialType.System_Collections_IEnumerable) &&
                    symbol.Kind is SymbolKind.Local or SymbolKind.Field or SymbolKind.Parameter or SymbolKind.Property);
            var collectionIdentifier = enumerationSymbol is null
                ? SyntaxFactory.IdentifierName("collection")
                : SyntaxFactory.IdentifierName(enumerationSymbol.Name);
            var itemString = NameGenerator.GenerateUniqueName(
                "item", name => semanticModel.LookupSymbols(position, name: name).IsEmpty);
            var foreachLoopSyntax = SyntaxFactory.ForEachStatement(varIdentifier, itemString, collectionIdentifier, SyntaxFactory.Block());

            return foreachLoopSyntax;
        }

        /// <summary>
        /// Goes through each piece of the foreach statement and extracts the identifiers
        /// as well as their locations to create SnippetPlaceholder's of each.
        /// </summary>
        protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SnippetPlaceholder>.GetInstance(out var arrayBuilder);
            GetPartsOfForEachStatement(node, out var identifier, out var expression, out var _1);
            arrayBuilder.Add(new SnippetPlaceholder(identifier.ToString(), ImmutableArray.Create(identifier.SpanStart)));
            arrayBuilder.Add(new SnippetPlaceholder(expression.ToString(), ImmutableArray.Create(expression.SpanStart)));

            return arrayBuilder.ToImmutableArray();

        }

        private static string GetIndentation(Document document, SyntaxNode node, SyntaxFormattingOptions syntaxFormattingOptions, CancellationToken cancellationToken)
        {
            var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);
            var foreachStatement = (ForEachStatementSyntax)node;
            var openBraceLine = parsedDocument.Text.Lines.GetLineFromPosition(foreachStatement.Statement.SpanStart).LineNumber;

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

            var syntaxFormattingOptions = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions: null, cancellationToken).ConfigureAwait(false);
            var indentationString = GetIndentation(document, snippet, syntaxFormattingOptions, cancellationToken);

            var foreachStatement = (ForEachStatementSyntax)snippet;
            var blockStatement = (BlockSyntax)foreachStatement.Statement;
            blockStatement = blockStatement.WithCloseBraceToken(blockStatement.CloseBraceToken.WithPrependedLeadingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, indentationString)));
            var newForEachStatement = foreachStatement.ReplaceNode(foreachStatement.Statement, blockStatement);

            var newRoot = root.ReplaceNode(foreachStatement, newForEachStatement);
            return document.WithSyntaxRoot(newRoot);
        }

        /// <summary>
        /// Gets the start of the BlockSyntax of the for statement
        /// to be able to insert the caret position at that location.
        /// </summary>
        protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget, SourceText sourceText)
        {
            var foreachStatement = (ForEachStatementSyntax)caretTarget;
            var blockStatement = (BlockSyntax)foreachStatement.Statement;

            var triviaSpan = blockStatement.CloseBraceToken.LeadingTrivia.Span;
            var line = sourceText.Lines.GetLineFromPosition(triviaSpan.Start);
            // Getting the location at the end of the line before the newline.
            return line.Span.End;
        }

        private static void GetPartsOfForEachStatement(SyntaxNode node, out SyntaxToken identifier, out SyntaxNode expression, out SyntaxNode statement)
        {
            var forEachStatement = (ForEachStatementSyntax)node;
            identifier = forEachStatement.Identifier;
            expression = forEachStatement.Expression;
            statement = forEachStatement.Statement;
        }
    }
}
