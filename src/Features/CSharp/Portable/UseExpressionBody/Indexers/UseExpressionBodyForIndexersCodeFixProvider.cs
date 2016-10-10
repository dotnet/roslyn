// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class UseExpressionBodyForIndexersCodeFixProvider : AbstractUseExpressionBodyCodeFixProvider<IndexerDeclarationSyntax>
    {
        public UseExpressionBodyForIndexersCodeFixProvider()
            : base(IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId,
                   CSharpCodeStyleOptions.PreferExpressionBodiedIndexers,
                   FeaturesResources.Use_expression_body_for_indexers,
                   FeaturesResources.Use_block_body_for_indexers)
        {
        }

        protected override SyntaxToken GetSemicolonToken(IndexerDeclarationSyntax declaration)
            => declaration.SemicolonToken;

        protected override ArrowExpressionClauseSyntax GetExpressionBody(IndexerDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        protected override BlockSyntax GetBody(IndexerDeclarationSyntax declaration)
            => declaration.AccessorList.Accessors[0].Body;

        protected override IndexerDeclarationSyntax WithSemicolonToken(IndexerDeclarationSyntax declaration, SyntaxToken token)
            => declaration.WithSemicolonToken(token);

        protected override IndexerDeclarationSyntax WithExpressionBody(IndexerDeclarationSyntax declaration, ArrowExpressionClauseSyntax expressionBody)
            => declaration.WithExpressionBody(expressionBody);

        protected override IndexerDeclarationSyntax WithBody(IndexerDeclarationSyntax declaration, BlockSyntax body)
            => body == null
                ? declaration.WithAccessorList(null)
                : declaration.WithAccessorList(SyntaxFactory.AccessorList(
                    SyntaxFactory.SingletonList(SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.GetAccessorDeclaration, body))));

        protected override bool CreateReturnStatementForExpression(IndexerDeclarationSyntax declaration) => true;
    }
}