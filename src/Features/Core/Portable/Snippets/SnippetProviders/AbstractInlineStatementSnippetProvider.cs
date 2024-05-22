// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

/// <summary>
/// Base class for snippets, that can be both executed as normal statement snippets
/// or constructed from a member access expression when accessing members of a specific type
/// </summary>
internal abstract class AbstractInlineStatementSnippetProvider : AbstractStatementSnippetProvider
{
    /// <summary>
    /// Tells if accessing type of a member access expression is valid for that snippet
    /// </summary>
    /// <param name="type">Type of right-hand side of an accessing expression</param>
    /// <param name="compilation">Current compilation instance</param>
    protected abstract bool IsValidAccessingType(ITypeSymbol type, Compilation compilation);

    /// <summary>
    /// Generate statement node
    /// </summary>
    /// <param name="inlineExpressionInfo">Information about inline expression or <see langword="null"/> if snippet is executed in normal statement context</param>
    protected abstract SyntaxNode GenerateStatement(SyntaxGenerator generator, SyntaxContext syntaxContext, InlineExpressionInfo? inlineExpressionInfo);

    /// <summary>
    /// Tells whether the original snippet was constructed from member access expression.
    /// Can be used by snippet providers to not mark that expression as a placeholder
    /// </summary>
    protected bool ConstructedFromInlineExpression { get; private set; }

    protected override bool IsValidSnippetLocation(in SnippetContext context, CancellationToken cancellationToken)
    {
        var syntaxContext = context.SyntaxContext;
        var semanticModel = syntaxContext.SemanticModel;
        var targetToken = syntaxContext.TargetToken;

        var syntaxFacts = context.Document.GetRequiredLanguageService<ISyntaxFactsService>();
        if (TryGetInlineExpressionInfo(targetToken, syntaxFacts, semanticModel, out var expressionInfo, cancellationToken) && expressionInfo.TypeInfo.Type is { } type)
        {
            return IsValidAccessingType(type, semanticModel.Compilation);
        }

        return base.IsValidSnippetLocation(in context, cancellationToken);
    }

    protected sealed override async Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
        var syntaxContext = document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);
        var targetToken = syntaxContext.TargetToken;

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        _ = TryGetInlineExpressionInfo(targetToken, syntaxFacts, semanticModel, out var inlineExpressionInfo, cancellationToken);

        var statement = GenerateStatement(SyntaxGenerator.GetGenerator(document), syntaxContext, inlineExpressionInfo);
        ConstructedFromInlineExpression = inlineExpressionInfo is not null;

        return new TextChange(TextSpan.FromBounds(inlineExpressionInfo?.Node.SpanStart ?? position, position), statement.ToFullString());
    }

    protected sealed override SyntaxNode? FindAddedSnippetSyntaxNode(SyntaxNode root, int position, Func<SyntaxNode?, bool> isCorrectContainer)
    {
        var closestNode = root.FindNode(TextSpan.FromBounds(position, position), getInnermostNodeForTie: true);
        return closestNode.FirstAncestorOrSelf<SyntaxNode>(isCorrectContainer);
    }

    private static bool TryGetInlineExpressionInfo(SyntaxToken targetToken, ISyntaxFactsService syntaxFacts, SemanticModel semanticModel, [NotNullWhen(true)] out InlineExpressionInfo? expressionInfo, CancellationToken cancellationToken)
    {
        var parentNode = targetToken.Parent;

        if (syntaxFacts.IsMemberAccessExpression(parentNode) &&
            syntaxFacts.IsExpressionStatement(parentNode?.Parent))
        {
            var expression = syntaxFacts.GetExpressionOfMemberAccessExpression(parentNode)!;
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
        if (syntaxFacts.IsQualifiedName(parentNode))
        {
            syntaxFacts.GetPartsOfQualifiedName(parentNode, out var expression, out _, out _);
            var typeInfo = semanticModel.GetSpeculativeTypeInfo(expression.SpanStart, expression, SpeculativeBindingOption.BindAsExpression);
            expressionInfo = new(expression, typeInfo);
            return true;
        }

        expressionInfo = null;
        return false;
    }
}
