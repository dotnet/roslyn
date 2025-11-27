// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

/// <summary>
/// Base class for snippets, that can be both executed as normal statement snippets
/// or constructed from a member access expression when accessing members of a specific type
/// </summary>
internal abstract class AbstractInlineStatementSnippetProvider<TStatementSyntax> : AbstractStatementSnippetProvider<TStatementSyntax>
    where TStatementSyntax : SyntaxNode
{
    /// <summary>
    /// Tells if accessing type of a member access expression is valid for that snippet
    /// </summary>
    /// <param name="type">Type of right-hand side of an accessing expression</param>
    /// <param name="compilation">Current compilation instance</param>
    protected abstract bool IsValidAccessingType(ITypeSymbol type, Compilation compilation);

    protected abstract bool CanInsertStatementAfterToken(SyntaxToken token);

    /// <summary>
    /// Generate statement node
    /// </summary>
    /// <param name="inlineExpressionInfo">Information about inline expression or <see langword="null"/> if snippet is executed in normal statement context</param>
    protected abstract TStatementSyntax GenerateStatement(
        SyntaxGenerator generator, SyntaxContext syntaxContext, SimplifierOptions simplifierOptions, InlineExpressionInfo? inlineExpressionInfo);

    /// <summary>
    /// Tells whether the original snippet was constructed from member access expression.
    /// Can be used by snippet providers to not mark that expression as a placeholder
    /// </summary>
    protected bool ConstructedFromInlineExpression { get; private set; }

    protected override bool IsValidSnippetLocationCore(SnippetContext context, CancellationToken cancellationToken)
    {
        var syntaxContext = context.SyntaxContext;
        var semanticModel = context.SemanticModel;
        var targetToken = syntaxContext.TargetToken;

        var syntaxFacts = context.Document.GetRequiredLanguageService<ISyntaxFactsService>();
        if (TryGetInlineExpressionInfo(targetToken, syntaxFacts, semanticModel, out var expressionInfo, cancellationToken) && expressionInfo.TypeInfo.Type is { } type)
        {
            return IsValidAccessingType(type, semanticModel.Compilation);
        }

        return base.IsValidSnippetLocationCore(context, cancellationToken);
    }

    protected sealed override async Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
        var simplifierOptions = await document.GetSimplifierOptionsAsync(cancellationToken).ConfigureAwait(false);
        var syntaxContext = document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);
        var targetToken = syntaxContext.TargetToken;

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        _ = TryGetInlineExpressionInfo(targetToken, syntaxFacts, semanticModel, out var inlineExpressionInfo, cancellationToken);

        var statement = GenerateStatement(SyntaxGenerator.GetGenerator(document), syntaxContext, simplifierOptions, inlineExpressionInfo);
        ConstructedFromInlineExpression = inlineExpressionInfo is not null;

        return new TextChange(TextSpan.FromBounds(inlineExpressionInfo?.Node.SpanStart ?? position, position), statement.ToFullString());
    }

    protected sealed override TStatementSyntax? FindAddedSnippetSyntaxNode(SyntaxNode root, int position)
    {
        var closestNode = root.FindNode(TextSpan.FromBounds(position, position), getInnermostNodeForTie: true);
        return closestNode.FirstAncestorOrSelf<TStatementSyntax>();
    }

    private bool CanInsertStatementBeforeToken(SyntaxToken token)
    {
        var previousToken = token.GetPreviousToken();
        if (previousToken == default)
        {
            // Token is the first token in the file
            return true;
        }

        return CanInsertStatementAfterToken(previousToken);
    }

    private bool TryGetInlineExpressionInfo(
        SyntaxToken targetToken,
        ISyntaxFactsService syntaxFacts,
        SemanticModel semanticModel,
        [NotNullWhen(true)] out InlineExpressionInfo? expressionInfo,
        CancellationToken cancellationToken)
    {
        var parentNode = targetToken.Parent;

        if (syntaxFacts.IsMemberAccessExpression(parentNode) &&
            CanInsertStatementBeforeToken(parentNode.GetFirstToken()))
        {
            syntaxFacts.GetPartsOfMemberAccessExpression(parentNode, out var expression, out var dotToken, out var name);
            var sourceText = parentNode.SyntaxTree.GetText(cancellationToken);

            if (sourceText.AreOnSameLine(dotToken, name.GetFirstToken()))
            {
                expressionInfo = null;
                return false;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);

            // Forbid a case when we are dotting of a type, e.g. `string.$$`.
            // Inline statement snippets are not valid in this context
            if (symbolInfo.Symbol is ITypeSymbol)
            {
                expressionInfo = null;
                return false;
            }

            var typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);
            expressionInfo = new(expression, typeInfo);
            return true;
        }

        // There are some edge cases when user intent is to write a member access expression,
        // but due to the current state of the document parser ends up parsing it as a qualified name, e.g.
        // ...
        // flag.$$
        // var a = 0;
        // ...
        // Here `flag.var` is parsed as a qualified name, so this case requires its own handling
        if (syntaxFacts.IsQualifiedName(parentNode) && CanInsertStatementBeforeToken(parentNode.GetFirstToken()))
        {
            syntaxFacts.GetPartsOfQualifiedName(parentNode, out var expression, out var dotToken, out var right);
            var sourceText = parentNode.SyntaxTree.GetText(cancellationToken);

            if (sourceText.AreOnSameLine(dotToken, right.GetFirstToken()))
            {
                expressionInfo = null;
                return false;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);

            // Forbid a case when we are dotting of a type, e.g. `string.$$`.
            // Inline statement snippets are not valid in this context
            if (symbolInfo.Symbol is ITypeSymbol)
            {
                expressionInfo = null;
                return false;
            }

            var typeInfo = semanticModel.GetSpeculativeTypeInfo(expression.SpanStart, expression, SpeculativeBindingOption.BindAsExpression);
            expressionInfo = new(expression, typeInfo);
            return true;
        }

        expressionInfo = null;
        return false;
    }
}
