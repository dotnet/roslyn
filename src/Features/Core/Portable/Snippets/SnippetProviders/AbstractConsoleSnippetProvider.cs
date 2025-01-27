// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Snippets;

internal abstract class AbstractConsoleSnippetProvider<
    TExpressionStatementSyntax,
    TExpressionSyntax,
    TArgumentListSyntax,
    TLambdaExpressionSyntax> : AbstractSingleChangeSnippetProvider<TExpressionSyntax>
    where TExpressionStatementSyntax : SyntaxNode
    where TExpressionSyntax : SyntaxNode
    where TArgumentListSyntax : SyntaxNode
    where TLambdaExpressionSyntax : TExpressionSyntax
{
    public sealed override string Identifier => CommonSnippetIdentifiers.ConsoleWriteLine;

    public sealed override string Description => FeaturesResources.console_writeline;

    public sealed override ImmutableArray<string> AdditionalFilterTexts { get; } = ["WriteLine"];

    protected abstract TArgumentListSyntax GetArgumentList(TExpressionSyntax expression);
    protected abstract SyntaxToken GetOpenParenToken(TArgumentListSyntax argumentList);

    protected sealed override async Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var generator = SyntaxGenerator.GetGenerator(document);

        var resultingNode = generator.InvocationExpression(generator.MemberAccessExpression(generator.IdentifierName(nameof(Console)), nameof(Console.WriteLine)));

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxContext = document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);

        // In case we are after an arrow token in lambda, Console.WriteLine acts like an expression,
        // so it doesn't need to be wrapped into a statement
        if (syntaxContext.TargetToken.Parent is not TLambdaExpressionSyntax)
        {
            resultingNode = generator.ExpressionStatement(resultingNode);
        }

        var change = new TextChange(TextSpan.FromBounds(position, position), resultingNode.ToFullString());
        return change;
    }

    /// <summary>
    /// Tries to get the location after the open parentheses in the argument list.
    /// If it can't, then we default to the end of the snippet's span.
    /// </summary>
    protected sealed override int GetTargetCaretPosition(TExpressionSyntax caretTarget, SourceText sourceText)
    {
        var argumentListNode = GetArgumentList(caretTarget);
        if (argumentListNode is null)
            return caretTarget.Span.End;

        var openParenToken = GetOpenParenToken(argumentListNode);
        return openParenToken.Span.End;
    }

    protected sealed override async Task<SyntaxNode> AnnotateNodesToReformatAsync(
        Document document, int position, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var snippetExpressionNode = FindAddedSnippetSyntaxNode(root, position);
        Contract.ThrowIfNull(snippetExpressionNode);

        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        var consoleSymbol = GetConsoleSymbolFromMetaDataName(compilation);
        var reformatSnippetNode = snippetExpressionNode.WithAdditionalAnnotations(FindSnippetAnnotation, Simplifier.Annotation, SymbolAnnotation.Create(consoleSymbol!), Formatter.Annotation);
        return root.ReplaceNode(snippetExpressionNode, reformatSnippetNode);
    }

    protected sealed override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(TExpressionSyntax node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        => [];

    protected static INamedTypeSymbol? GetConsoleSymbolFromMetaDataName(Compilation compilation)
        => compilation.GetBestTypeByMetadataName(typeof(Console).FullName!);

    protected sealed override TExpressionSyntax? FindAddedSnippetSyntaxNode(SyntaxNode root, int position)
    {
        var closestNode = root.FindNode(TextSpan.FromBounds(position, position));
        var nearestExpression = closestNode.FirstAncestorOrSelf<TExpressionSyntax>(static exp => exp.Parent is TExpressionStatementSyntax or TLambdaExpressionSyntax);
        if (nearestExpression is null)
            return null;

        // Checking to see if that expression that we found is
        // starting at the same position as the position we inserted
        // the Console WriteLine expression.
        if (nearestExpression.SpanStart != position)
            return null;

        return nearestExpression;
    }
}
