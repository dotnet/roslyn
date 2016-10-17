// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class UseExpressionBodyForConversionOperatorsCodeFixProvider : AbstractUseExpressionBodyCodeFixProvider<ConversionOperatorDeclarationSyntax>
    {
        public UseExpressionBodyForConversionOperatorsCodeFixProvider()
            : base(IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId,
                   CSharpCodeStyleOptions.PreferExpressionBodiedOperators,
                   FeaturesResources.Use_expression_body_for_operators,
                   FeaturesResources.Use_block_body_for_operators)
        {
        }

        protected override SyntaxToken GetSemicolonToken(ConversionOperatorDeclarationSyntax declaration)
            => declaration.SemicolonToken;

        protected override ArrowExpressionClauseSyntax GetExpressionBody(ConversionOperatorDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        protected override BlockSyntax GetBody(ConversionOperatorDeclarationSyntax declaration)
            => declaration.Body;

        protected override ConversionOperatorDeclarationSyntax WithSemicolonToken(ConversionOperatorDeclarationSyntax declaration, SyntaxToken token)
            => declaration.WithSemicolonToken(token);

        protected override ConversionOperatorDeclarationSyntax WithExpressionBody(ConversionOperatorDeclarationSyntax declaration, ArrowExpressionClauseSyntax expressionBody)
            => declaration.WithExpressionBody(expressionBody);

        protected override ConversionOperatorDeclarationSyntax WithBody(ConversionOperatorDeclarationSyntax declaration, BlockSyntax body)
            => declaration.WithBody(body);

        protected override bool CreateReturnStatementForExpression(ConversionOperatorDeclarationSyntax declaration)
            => true;
    }

}