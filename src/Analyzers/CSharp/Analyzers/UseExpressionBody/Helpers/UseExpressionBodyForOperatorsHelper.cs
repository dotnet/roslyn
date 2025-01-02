// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody;

internal class UseExpressionBodyForOperatorsHelper :
    UseExpressionBodyHelper<OperatorDeclarationSyntax>
{
    public static readonly UseExpressionBodyForOperatorsHelper Instance = new();

    private UseExpressionBodyForOperatorsHelper()
        : base(IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId,
               EnforceOnBuildValues.UseExpressionBodyForOperators,
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_expression_body_for_operator), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_block_body_for_operator), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               CSharpCodeStyleOptions.PreferExpressionBodiedOperators,
               [SyntaxKind.OperatorDeclaration])
    {
    }

    public override CodeStyleOption2<ExpressionBodyPreference> GetExpressionBodyPreference(CSharpCodeGenerationOptions options)
        => options.PreferExpressionBodiedOperators;

    protected override BlockSyntax GetBody(OperatorDeclarationSyntax declaration)
        => declaration.Body;

    protected override ArrowExpressionClauseSyntax GetExpressionBody(OperatorDeclarationSyntax declaration)
        => declaration.ExpressionBody;

    protected override SyntaxToken GetSemicolonToken(OperatorDeclarationSyntax declaration)
        => declaration.SemicolonToken;

    protected override OperatorDeclarationSyntax WithSemicolonToken(OperatorDeclarationSyntax declaration, SyntaxToken token)
        => declaration.WithSemicolonToken(token);

    protected override OperatorDeclarationSyntax WithExpressionBody(OperatorDeclarationSyntax declaration, ArrowExpressionClauseSyntax expressionBody)
        => declaration.WithExpressionBody(expressionBody);

    protected override OperatorDeclarationSyntax WithBody(OperatorDeclarationSyntax declaration, BlockSyntax body)
        => declaration.WithBody(body);

    protected override bool CreateReturnStatementForExpression(SemanticModel semanticModel, OperatorDeclarationSyntax declaration)
        => true;
}
