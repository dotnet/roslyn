// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.Snippets;

internal abstract class AbstractIfSnippetProvider<TIfStatementSyntax, TExpressionSyntax> : AbstractConditionalBlockSnippetProvider<TIfStatementSyntax, TExpressionSyntax>
    where TIfStatementSyntax : SyntaxNode
    where TExpressionSyntax : SyntaxNode
{
    public sealed override ImmutableArray<string> AdditionalFilterTexts { get; } = ["statement"];

    protected sealed override TIfStatementSyntax GenerateStatement(
        SyntaxGenerator generator, SyntaxContext syntaxContext, SimplifierOptions simplifierOptions, InlineExpressionInfo? inlineExpressionInfo)
        => (TIfStatementSyntax)generator.IfStatement(inlineExpressionInfo?.Node.WithoutLeadingTrivia() ?? generator.TrueLiteralExpression(), []);
}
