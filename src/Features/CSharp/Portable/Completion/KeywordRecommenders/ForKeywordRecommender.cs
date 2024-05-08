// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class ForKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public ForKeywordRecommender()
        : base(SyntaxKind.ForKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsStatementContext || context.IsGlobalStatementContext)
            return true;

        var targetToken = context.TargetToken;

        // extension E $$
        if (targetToken.Kind() == SyntaxKind.IdentifierToken &&
            targetToken.Parent is TypeDeclarationSyntax(SyntaxKind.ExtensionDeclaration))
        {
            return true;
        }

        // extension E<X> $$
        if (targetToken.Kind() == SyntaxKind.GreaterThanToken &&
            targetToken.Parent is TypeParameterListSyntax { Parent: TypeDeclarationSyntax(SyntaxKind.ExtensionDeclaration) })
        {
            return true;
        }

        // extension E(int a) $$
        if (targetToken.Kind() == SyntaxKind.CloseParenToken &&
            targetToken.Parent is ParameterListSyntax { Parent: TypeDeclarationSyntax(SyntaxKind.ExtensionDeclaration) })
        {
            return true;
        }

        return false;
    }
}
