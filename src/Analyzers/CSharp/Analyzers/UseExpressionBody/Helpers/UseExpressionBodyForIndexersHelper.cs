// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody;

internal sealed class UseExpressionBodyForIndexersHelper :
    UseExpressionBodyHelper<IndexerDeclarationSyntax>
{
    public static readonly UseExpressionBodyForIndexersHelper Instance = new();

    private UseExpressionBodyForIndexersHelper()
        : base(IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId,
               EnforceOnBuildValues.UseExpressionBodyForIndexers,
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_expression_body_for_indexer), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_block_body_for_indexer), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               CSharpCodeStyleOptions.PreferExpressionBodiedIndexers,
               [SyntaxKind.IndexerDeclaration])
    {
    }

    public override CodeStyleOption2<ExpressionBodyPreference> GetExpressionBodyPreference(CSharpCodeGenerationOptions options)
        => options.PreferExpressionBodiedIndexers;

    protected override BlockSyntax GetBody(IndexerDeclarationSyntax declaration)
        => GetBodyFromSingleGetAccessor(declaration.AccessorList);

    protected override ArrowExpressionClauseSyntax GetExpressionBody(IndexerDeclarationSyntax declaration)
        => declaration.ExpressionBody;

    protected override SyntaxToken GetSemicolonToken(IndexerDeclarationSyntax declaration)
        => declaration.SemicolonToken;

    protected override IndexerDeclarationSyntax WithSemicolonToken(IndexerDeclarationSyntax declaration, SyntaxToken token)
        => declaration.WithSemicolonToken(token);

    protected override IndexerDeclarationSyntax WithExpressionBody(IndexerDeclarationSyntax declaration, ArrowExpressionClauseSyntax expressionBody)
        => declaration.WithExpressionBody(expressionBody);

    protected override IndexerDeclarationSyntax WithAccessorList(IndexerDeclarationSyntax declaration, AccessorListSyntax accessorList)
        => declaration.WithAccessorList(accessorList);

    protected override IndexerDeclarationSyntax WithBody(IndexerDeclarationSyntax declaration, BlockSyntax body)
    {
        if (body == null)
        {
            return declaration.WithAccessorList(null);
        }

        throw new InvalidOperationException();
    }

    protected override IndexerDeclarationSyntax WithGenerateBody(SemanticModel semanticModel, IndexerDeclarationSyntax declaration, CancellationToken cancellationToken)
        => WithAccessorList(semanticModel, declaration, cancellationToken);

    protected override bool CreateReturnStatementForExpression(SemanticModel semanticModel, IndexerDeclarationSyntax declaration, CancellationToken cancellationToken)
        => true;

    protected override bool TryConvertToExpressionBody(
        IndexerDeclarationSyntax declaration,
        ExpressionBodyPreference conversionPreference,
        CancellationToken cancellationToken,
        out ArrowExpressionClauseSyntax arrowExpression,
        out SyntaxToken semicolonToken)
    {
        return TryConvertToExpressionBodyForBaseProperty(declaration, conversionPreference, cancellationToken, out arrowExpression, out semicolonToken);
    }

    protected override Location GetDiagnosticLocation(IndexerDeclarationSyntax declaration)
    {
        var body = GetBody(declaration);
        if (body != null)
        {
            return body.Statements[0].GetLocation();
        }

        var getAccessor = GetSingleGetAccessor(declaration.AccessorList);
        return getAccessor.ExpressionBody.GetLocation();
    }
}
