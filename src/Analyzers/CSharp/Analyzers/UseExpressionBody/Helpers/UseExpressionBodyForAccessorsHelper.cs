// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody;

using static SyntaxFactory;

internal sealed class UseExpressionBodyForAccessorsHelper :
    UseExpressionBodyHelper<AccessorDeclarationSyntax>
{
    public static readonly UseExpressionBodyForAccessorsHelper Instance = new();

    private UseExpressionBodyForAccessorsHelper()
        : base(IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId,
               EnforceOnBuildValues.UseExpressionBodyForAccessors,
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_expression_body_for_accessor), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_block_body_for_accessor), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               CSharpCodeStyleOptions.PreferExpressionBodiedAccessors,
               [
                   SyntaxKind.GetAccessorDeclaration,
                   SyntaxKind.SetAccessorDeclaration,
                   SyntaxKind.InitAccessorDeclaration,
                   SyntaxKind.AddAccessorDeclaration,
                   SyntaxKind.RemoveAccessorDeclaration,
               ])
    {
    }

    public override CodeStyleOption2<ExpressionBodyPreference> GetExpressionBodyPreference(CSharpCodeGenerationOptions options)
        => options.PreferExpressionBodiedAccessors;

    protected override BlockSyntax? GetBody(AccessorDeclarationSyntax declaration)
        => declaration.Body;

    protected override ArrowExpressionClauseSyntax? GetExpressionBody(AccessorDeclarationSyntax declaration)
        => declaration.ExpressionBody;

    protected override SyntaxToken GetSemicolonToken(AccessorDeclarationSyntax declaration)
        => declaration.SemicolonToken;

    protected override AccessorDeclarationSyntax WithSemicolonToken(AccessorDeclarationSyntax declaration, SyntaxToken token)
        => declaration.WithSemicolonToken(token);

    protected override AccessorDeclarationSyntax WithExpressionBody(AccessorDeclarationSyntax declaration, ArrowExpressionClauseSyntax? expressionBody)
        => declaration.WithExpressionBody(expressionBody);

    protected override AccessorDeclarationSyntax WithBody(AccessorDeclarationSyntax declaration, BlockSyntax? body)
    {
        // If the accessor was on the same line as its parent, add an elastic marker so we can place it properly now
        // that it will have a multi-line block body.  If it's already on its own line, do nothing as we want to just
        // keep it there and not move it.
        var result = declaration.WithBody(body);
        return !declaration.GetLeadingTrivia().Any(t => t.Kind() == SyntaxKind.EndOfLineTrivia)
            ? result.WithPrependedLeadingTrivia(ElasticMarker)
            : result;
    }

    protected override bool CreateReturnStatementForExpression(SemanticModel semanticModel, AccessorDeclarationSyntax declaration, CancellationToken cancellationToken)
        => declaration.IsKind(SyntaxKind.GetAccessorDeclaration);
}
