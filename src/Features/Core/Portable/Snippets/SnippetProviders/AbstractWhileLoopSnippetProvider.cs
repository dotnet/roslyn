// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal abstract class AbstractWhileLoopSnippetProvider<TWhileStatementSyntax, TExpressionSyntax>
    : AbstractConditionalBlockSnippetProvider<TWhileStatementSyntax, TExpressionSyntax>
    where TWhileStatementSyntax : SyntaxNode
    where TExpressionSyntax : SyntaxNode
{
    protected sealed override TWhileStatementSyntax GenerateStatement(
        SyntaxGenerator generator, SyntaxContext syntaxContext, SimplifierOptions simplifierOptions, InlineExpressionInfo? inlineExpressionInfo)
        => (TWhileStatementSyntax)generator.WhileStatement(inlineExpressionInfo?.Node.WithoutLeadingTrivia() ?? generator.TrueLiteralExpression(), []);
}
