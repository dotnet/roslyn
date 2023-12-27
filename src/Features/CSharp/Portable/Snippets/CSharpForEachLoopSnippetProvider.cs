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

        protected override async Task<bool> IsValidSnippetLocationAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var syntaxContext = document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);
            var targetToken = syntaxContext.TargetToken;

            // Allow `foreach` snippet after `await` as expression statement
            // So `await $$` is a valid position, but `var result = await $$` is not
            // The second check if for case when completions are invoked after `await` in non-async context. In such cases parser treats `await` as identifier
            if (targetToken is { RawKind: (int)SyntaxKind.AwaitKeyword, Parent: ExpressionSyntax { Parent: ExpressionStatementSyntax } } ||
                targetToken is { RawKind: (int)SyntaxKind.IdentifierToken, ValueText: "await", Parent: IdentifierNameSyntax { Parent: ExpressionStatementSyntax } })
            {
                return true;
            }

            return await base.IsValidSnippetLocationAsync(document, position, cancellationToken).ConfigureAwait(false);
        }

        protected override SyntaxNode GenerateStatement(SyntaxGenerator generator, SyntaxContext syntaxContext, SyntaxNode? inlineExpression)
        {
            var semanticModel = syntaxContext.SemanticModel;
            var position = syntaxContext.Position;

            var varIdentifier = SyntaxFactory.IdentifierName("var");
            var collectionIdentifier = (ExpressionSyntax?)inlineExpression;

            if (collectionIdentifier is null)
            {
                var isAsync = syntaxContext.TargetToken is { RawKind: (int)SyntaxKind.AwaitKeyword } or { RawKind: (int)SyntaxKind.IdentifierToken, ValueText: "await" };
                var enumerationSymbol = semanticModel.LookupSymbols(position).FirstOrDefault(symbol => symbol.GetSymbolType() is { } symbolType &&
                    (isAsync ? symbolType.CanBeAsynchronouslyEnumerated(semanticModel.Compilation) : symbolType.CanBeEnumerated()) &&
                    symbol.Kind is SymbolKind.Local or SymbolKind.Field or SymbolKind.Parameter or SymbolKind.Property);
                collectionIdentifier = enumerationSymbol is null
                    ? SyntaxFactory.IdentifierName("collection")
                    : SyntaxFactory.IdentifierName(enumerationSymbol.Name);
            }

            var itemString = NameGenerator.GenerateUniqueName(
                "item", name => semanticModel.LookupSymbols(position, name: name).IsEmpty);

            ForEachStatementSyntax forEachStatement;

            if (inlineExpression is not null &&
                semanticModel.GetTypeInfo(inlineExpression).Type!.CanBeAsynchronouslyEnumerated(semanticModel.Compilation))
            {
                forEachStatement = SyntaxFactory.ForEachStatement(
                    SyntaxFactory.Token(SyntaxKind.AwaitKeyword),
                    SyntaxFactory.Token(SyntaxKind.ForEachKeyword),
                    SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                    varIdentifier,
                    SyntaxFactory.Identifier(itemString),
                    SyntaxFactory.Token(SyntaxKind.InKeyword),
                    collectionIdentifier.WithoutLeadingTrivia(),
                    SyntaxFactory.Token(SyntaxKind.CloseParenToken),
                    SyntaxFactory.Block());
            }
            else
            {
                forEachStatement = SyntaxFactory.ForEachStatement(
                    varIdentifier,
                    itemString,
                    collectionIdentifier.WithoutLeadingTrivia(),
                    SyntaxFactory.Block());
            }

            return forEachStatement.NormalizeWhitespace();
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

        protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget, SourceText sourceText)
        {
            return CSharpSnippetHelpers.GetTargetCaretPositionInBlock<ForEachStatementSyntax>(
                caretTarget,
                static s => (BlockSyntax)s.Statement,
                sourceText);
        }

        protected override Task<Document> AddIndentationToDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            return CSharpSnippetHelpers.AddBlockIndentationToDocumentAsync<ForEachStatementSyntax>(
                document,
                FindSnippetAnnotation,
                static s => (BlockSyntax)s.Statement,
                cancellationToken);
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
