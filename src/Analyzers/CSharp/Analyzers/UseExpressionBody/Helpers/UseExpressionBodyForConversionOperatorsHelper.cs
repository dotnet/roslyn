// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody;

internal sealed class UseExpressionBodyForConversionOperatorsHelper :
    UseExpressionBodyHelper<ConversionOperatorDeclarationSyntax>
{
    public static readonly UseExpressionBodyForConversionOperatorsHelper Instance = new();

    private UseExpressionBodyForConversionOperatorsHelper()
        : base(IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId,
               EnforceOnBuildValues.UseExpressionBodyForConversionOperators,
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_expression_body_for_conversion_operator), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_block_body_for_conversion_operator), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               CSharpCodeStyleOptions.PreferExpressionBodiedOperators,
               [SyntaxKind.ConversionOperatorDeclaration])
    {
    }

    public override CodeStyleOption2<ExpressionBodyPreference> GetExpressionBodyPreference(CSharpCodeGenerationOptions options)
        => options.PreferExpressionBodiedOperators;

    protected override BlockSyntax? GetBody(ConversionOperatorDeclarationSyntax declaration)
        => declaration.Body;

    protected override ArrowExpressionClauseSyntax? GetExpressionBody(ConversionOperatorDeclarationSyntax declaration)
        => declaration.ExpressionBody;

    protected override SyntaxToken GetSemicolonToken(ConversionOperatorDeclarationSyntax declaration)
        => declaration.SemicolonToken;

    protected override ConversionOperatorDeclarationSyntax WithSemicolonToken(ConversionOperatorDeclarationSyntax declaration, SyntaxToken token)
        => declaration.WithSemicolonToken(token);

    protected override ConversionOperatorDeclarationSyntax WithExpressionBody(ConversionOperatorDeclarationSyntax declaration, ArrowExpressionClauseSyntax? expressionBody)
        => declaration.WithExpressionBody(expressionBody);

    protected override ConversionOperatorDeclarationSyntax WithBody(ConversionOperatorDeclarationSyntax declaration, BlockSyntax? body)
        => declaration.WithBody(body);

    protected override bool CreateReturnStatementForExpression(SemanticModel semanticModel, ConversionOperatorDeclarationSyntax declaration, CancellationToken cancellationToken)
        => true;
}
