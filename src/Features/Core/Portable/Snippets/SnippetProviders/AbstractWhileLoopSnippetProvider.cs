// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal abstract class AbstractWhileLoopSnippetProvider<TWhileStatementSyntax, TExpressionSyntax>
    : AbstractConditionalBlockSnippetProvider<TWhileStatementSyntax, TExpressionSyntax>
    where TWhileStatementSyntax : SyntaxNode
    where TExpressionSyntax : SyntaxNode
{
    protected sealed override Func<SyntaxNode?, bool> GetSnippetContainerFunction(ISyntaxFacts syntaxFacts) => syntaxFacts.IsWhileStatement;

    protected sealed override TWhileStatementSyntax GenerateStatement(SyntaxGenerator generator, SyntaxContext syntaxContext, InlineExpressionInfo? inlineExpressionInfo)
        => (TWhileStatementSyntax)generator.WhileStatement(inlineExpressionInfo?.Node.WithoutLeadingTrivia() ?? generator.TrueLiteralExpression(), []);
}
