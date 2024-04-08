// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

[ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpConsoleSnippetProvider() : AbstractConsoleSnippetProvider<
    ExpressionStatementSyntax,
    ExpressionSyntax,
    ArgumentListSyntax>
{
    protected override ExpressionSyntax GetExpression(ExpressionStatementSyntax expressionStatement)
        => expressionStatement.Expression;

    protected override ArgumentListSyntax GetArgumentList(ExpressionSyntax expression)
        => ((InvocationExpressionSyntax)expression).ArgumentList;

    protected override SyntaxToken GetOpenParenToken(ArgumentListSyntax argumentList)
        => argumentList.OpenParenToken;
}
