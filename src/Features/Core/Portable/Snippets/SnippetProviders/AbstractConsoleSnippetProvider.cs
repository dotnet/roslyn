// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Snippets;

internal abstract class AbstractConsoleSnippetProvider<TExpressionStatementSyntax> : AbstractStatementSnippetProvider<TExpressionStatementSyntax>
    where TExpressionStatementSyntax : SyntaxNode
{
    public sealed override string Identifier => CommonSnippetIdentifiers.ConsoleWriteLine;

    public sealed override string Description => FeaturesResources.console_writeline;

    public sealed override ImmutableArray<string> AdditionalFilterTexts { get; } = ["WriteLine"];

    protected sealed override bool IsValidSnippetLocation(in SnippetContext context, CancellationToken cancellationToken)
    {
        var consoleSymbol = GetConsoleSymbolFromMetaDataName(context.SyntaxContext.SemanticModel.Compilation);
        if (consoleSymbol is null)
            return false;

        return base.IsValidSnippetLocation(in context, cancellationToken);
    }

    protected sealed override Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var generator = SyntaxGenerator.GetGenerator(document);

        var invocation = generator.InvocationExpression(generator.MemberAccessExpression(generator.IdentifierName(nameof(Console)), nameof(Console.WriteLine)));
        var expressionStatement = generator.ExpressionStatement(invocation);

        var change = new TextChange(TextSpan.FromBounds(position, position), expressionStatement.ToFullString());
        return Task.FromResult(change);
    }

    /// <summary>
    /// Tries to get the location after the open parentheses in the argument list.
    /// If it can't, then we default to the end of the snippet's span.
    /// </summary>
    protected sealed override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, TExpressionStatementSyntax caretTarget, SourceText sourceText)
    {
        var invocationExpression = caretTarget.DescendantNodes().Where(syntaxFacts.IsInvocationExpression).FirstOrDefault();
        if (invocationExpression is null)
            return caretTarget.Span.End;

        var argumentListNode = syntaxFacts.GetArgumentListOfInvocationExpression(invocationExpression);
        if (argumentListNode is null)
            return caretTarget.Span.End;

        syntaxFacts.GetPartsOfArgumentList(argumentListNode, out var openParenToken, out _, out _);
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

    protected sealed override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(TExpressionStatementSyntax node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        => [];

    private static SyntaxToken? GetOpenParenToken(SyntaxNode node, ISyntaxFacts syntaxFacts)
    {
        var invocationExpression = node.DescendantNodes().Where(syntaxFacts.IsInvocationExpression).FirstOrDefault();
        if (invocationExpression is null)
        {
            return null;
        }

        var argumentListNode = syntaxFacts.GetArgumentListOfInvocationExpression(invocationExpression);
        if (argumentListNode is null)
        {
            return null;
        }

        syntaxFacts.GetPartsOfArgumentList(argumentListNode, out var openParenToken, out _, out _);

        return openParenToken;
    }

    private static INamedTypeSymbol? GetConsoleSymbolFromMetaDataName(Compilation compilation)
        => compilation.GetBestTypeByMetadataName(typeof(Console).FullName!);

    protected sealed override TExpressionStatementSyntax? FindAddedSnippetSyntaxNode(SyntaxNode root, int position)
    {
        var closestNode = root.FindNode(TextSpan.FromBounds(position, position));
        var nearestExpressionStatement = closestNode.FirstAncestorOrSelf<TExpressionStatementSyntax>();
        if (nearestExpressionStatement is null)
            return null;

        // Checking to see if that expression statement that we found is
        // starting at the same position as the position we inserted
        // the Console WriteLine expression statement.
        if (nearestExpressionStatement.SpanStart != position)
            return null;

        return nearestExpressionStatement;
    }
}
