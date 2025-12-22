// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

/// <summary>
/// Base class for "if" and "while" snippet providers
/// </summary>
internal abstract class AbstractConditionalBlockSnippetProvider<TStatementSyntax, TExpressionSyntax> : AbstractInlineStatementSnippetProvider<TStatementSyntax>
    where TStatementSyntax : SyntaxNode
    where TExpressionSyntax : SyntaxNode
{
    protected abstract TExpressionSyntax GetCondition(TStatementSyntax node);

    protected sealed override bool IsValidAccessingType(ITypeSymbol type, Compilation compilation)
        => type.SpecialType == SpecialType.System_Boolean;

    protected sealed override async ValueTask<ImmutableArray<SnippetPlaceholder>> GetPlaceHolderLocationsListAsync(
        Document document, TStatementSyntax node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
    {
        if (ConstructedFromInlineExpression)
            return [];

        var condition = GetCondition(node);
        return [new SnippetPlaceholder(condition.ToString(), condition.SpanStart)];
    }
}
