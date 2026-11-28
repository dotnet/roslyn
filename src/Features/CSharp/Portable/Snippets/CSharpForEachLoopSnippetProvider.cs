// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpForEachLoopSnippetProvider() : AbstractForEachLoopSnippetProvider<ForEachStatementSyntax>
{
    public override string Identifier => CSharpSnippetIdentifiers.ForEach;

    public override string Description => FeaturesResources.foreach_loop;

    protected override bool IsValidAccessingType(ITypeSymbol type, SnippetContext context, CancellationToken cancellationToken)
        => base.IsValidAccessingType(type, context, cancellationToken) || context.SemanticModel.GetAsyncIterationType(context.Position, type, cancellationToken) is not null;

    protected override bool IsValidSnippetLocationCore(SnippetContext context, CancellationToken cancellationToken)
    {
        var syntaxContext = context.SyntaxContext;
        var token = syntaxContext.TargetToken;

        // Allow `foreach` snippet after `await` as expression statement
        // So `await $$` is a valid position, but `var result = await $$` is not
        // The second check if for case when completions are invoked after `await` in non-async context. In such cases parser treats `await` as identifier
        if (token is { RawKind: (int)SyntaxKind.AwaitKeyword, Parent: ExpressionSyntax { Parent: ExpressionStatementSyntax } } ||
            token is { RawKind: (int)SyntaxKind.IdentifierToken, ValueText: "await", Parent: IdentifierNameSyntax { Parent: ExpressionStatementSyntax } })
        {
            return true;
        }

        return base.IsValidSnippetLocationCore(context, cancellationToken);
    }

    protected override bool CanInsertStatementAfterToken(SyntaxToken token)
        => token.IsBeginningOfStatementContext() || token.IsBeginningOfGlobalStatementContext();

    protected override ValueTask<ForEachStatementSyntax> AdjustSnippetExpressionAsync(Document document, ForEachStatementSyntax snippetExpressionNode, CancellationToken cancellationToken)
        => new(snippetExpressionNode.WithType(snippetExpressionNode.Type.WithAdditionalAnnotations(Simplifier.Annotation)));

    protected override ForEachStatementSyntax GenerateStatement(
        SyntaxGenerator generator,
        SyntaxContext syntaxContext,
        SimplifierOptions simplifierOptions,
        InlineExpressionInfo? inlineExpressionInfo,
        CancellationToken cancellationToken)
    {
        var semanticModel = syntaxContext.SemanticModel;
        var position = syntaxContext.Position;

        var collectionIdentifier = (ExpressionSyntax?)inlineExpressionInfo?.Node;

        if (collectionIdentifier is null)
        {
            var isAsync = syntaxContext.TargetToken.IsKindOrHasMatchingText(SyntaxKind.AwaitKeyword);

            ISymbol? enumerationSymbol = null;
            foreach (var symbol in semanticModel.LookupSymbols(position))
            {
                if (symbol.GetSymbolType() is not { } symbolType ||
                    symbol.Kind is not (SymbolKind.Local or SymbolKind.Field or SymbolKind.Parameter or SymbolKind.Property))
                {
                    continue;
                }

                var iterationType = isAsync
                    ? semanticModel.GetAsyncIterationType(position, symbolType, cancellationToken)
                    : semanticModel.GetIterationType(position, symbolType, cancellationToken);

                if (iterationType is not null)
                {
                    enumerationSymbol = symbol;
                    break;
                }
            }

            collectionIdentifier = enumerationSymbol is null
                ? IdentifierName("collection")
                : IdentifierName(enumerationSymbol.Name);
        }

        var itemString = NameGenerator.GenerateUniqueName(
            "item", name => semanticModel.LookupSymbols(position, name: name).IsEmpty);

        ForEachStatementSyntax forEachStatement;

        var inlineExpressionType = inlineExpressionInfo?.TypeInfo.Type;

        if (inlineExpressionType is not null &&
            semanticModel.GetAsyncIterationType(position, inlineExpressionType, cancellationToken) is { } asyncIterationType)
        {
            forEachStatement = ForEachStatement(
                AwaitKeyword,
                ForEachKeyword,
                OpenParenToken,
                asyncIterationType.GenerateTypeSyntax(),
                Identifier(itemString),
                InKeyword,
                collectionIdentifier.WithoutLeadingTrivia(),
                CloseParenToken,
                Block());
        }
        else
        {
            var iterationTypeSyntax = inlineExpressionType is not null &&
                                      semanticModel.GetIterationType(position, inlineExpressionType, cancellationToken) is { } iterationType
                ? iterationType.GenerateTypeSyntax()
                : IdentifierName("var");

            forEachStatement = ForEachStatement(
                iterationTypeSyntax,
                itemString,
                collectionIdentifier.WithoutLeadingTrivia(),
                Block());
        }

        return forEachStatement.NormalizeWhitespace();
    }

    /// <summary>
    /// Goes through each piece of the foreach statement and extracts the identifiers
    /// as well as their locations to create SnippetPlaceholder's of each.
    /// </summary>
    protected override ValueTask<ImmutableArray<SnippetPlaceholder>> GetPlaceHolderLocationsListAsync(
        Document document, ForEachStatementSyntax node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<SnippetPlaceholder>.GetInstance(out var arrayBuilder);
        arrayBuilder.Add(new SnippetPlaceholder(node.Identifier.ToString(), node.Identifier.SpanStart));

        if (!ConstructedFromInlineExpression)
            arrayBuilder.Add(new SnippetPlaceholder(node.Expression.ToString(), node.Expression.SpanStart));

        return new(arrayBuilder.ToImmutableAndClear());
    }

    protected override int GetTargetCaretPosition(ForEachStatementSyntax forEachStatement, SourceText sourceText)
        => CSharpSnippetHelpers.GetTargetCaretPositionInBlock(
            forEachStatement,
            static s => (BlockSyntax)s.Statement,
            sourceText);

    protected override Task<Document> AddIndentationToDocumentAsync(Document document, ForEachStatementSyntax forEachStatement, CancellationToken cancellationToken)
        => CSharpSnippetHelpers.AddBlockIndentationToDocumentAsync(
            document,
            forEachStatement,
            static s => (BlockSyntax)s.Statement,
            cancellationToken);
}
