// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

/// <summary>
/// Property or with explicit body:
///   T P => [|expr;|]
///   T this[...] => [|expr;|]
/// </summary>
internal sealed class PropertyOrIndexerWithExplicitBodyDeclarationBody(BasePropertyDeclarationSyntax propertyOrIndexer) : PropertyOrIndexerAccessorDeclarationBody
{
    public ArrowExpressionClauseSyntax Body
        => (propertyOrIndexer is PropertyDeclarationSyntax property) ? property.ExpressionBody! : ((IndexerDeclarationSyntax)propertyOrIndexer).ExpressionBody!;

    public ExpressionSyntax BodyExpression
        => Body.Expression;

    public override SyntaxNode? ExplicitBody
        => BodyExpression;

    public override SyntaxNode? HeaderActiveStatement
        => null;

    public override TextSpan HeaderActiveStatementSpan
        => default;

    public override SyntaxNode? MatchRoot
        => Body;

    public sealed override IEnumerable<SyntaxToken>? GetActiveTokens()
        => BodyExpression.DescendantTokens();
}
