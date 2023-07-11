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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
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

        protected override SyntaxNode GenerateStatement(SyntaxGenerator generator, SyntaxContext syntaxContext, SyntaxNode? inlineExpression)
        {
            var semanticModel = syntaxContext.SemanticModel;
            var position = syntaxContext.Position;

            var varIdentifier = SyntaxFactory.IdentifierName("var");
            var collectionIdentifier = (ExpressionSyntax?)inlineExpression;

            if (collectionIdentifier is null)
            {
                var enumerationSymbol = semanticModel.LookupSymbols(position).FirstOrDefault(symbol => symbol.GetSymbolType() is { } symbolType &&
                    symbolType.CanBeEnumerated() &&
                    symbol.Kind is SymbolKind.Local or SymbolKind.Field or SymbolKind.Parameter or SymbolKind.Property);
                collectionIdentifier = enumerationSymbol is null
                    ? SyntaxFactory.IdentifierName("collection")
                    : SyntaxFactory.IdentifierName(enumerationSymbol.Name);
            }

            var itemString = NameGenerator.GenerateUniqueName(
                "item", name => semanticModel.LookupSymbols(position, name: name).IsEmpty);

            return SyntaxFactory.ForEachStatement(varIdentifier, itemString, collectionIdentifier.WithoutLeadingTrivia(), SyntaxFactory.Block()).NormalizeWhitespace();
        }

        /// <summary>
        /// Goes through each piece of the foreach statement and extracts the identifiers
        /// as well as their locations to create SnippetPlaceholder's of each.
        /// </summary>
        protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SnippetPlaceholder>.GetInstance(out var arrayBuilder);
            GetPartsOfForEachStatement(node, out var identifier, out var expression, out var _1);
            arrayBuilder.Add(new SnippetPlaceholder(identifier.ToString(), identifier.SpanStart));

            if (!ConstructedFromInlineExpression)
                arrayBuilder.Add(new SnippetPlaceholder(expression.ToString(), expression.SpanStart));

            return arrayBuilder.ToImmutableArray();

        }

        protected override Task<Document> AddIndentationToDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            return СSharpSnippetIndentationHelpers.AddBlockIndentationToDocumentAsync<ForEachStatementSyntax>(
                document,
                FindSnippetAnnotation,
                static s => (BlockSyntax)s.Statement,
                cancellationToken);
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
