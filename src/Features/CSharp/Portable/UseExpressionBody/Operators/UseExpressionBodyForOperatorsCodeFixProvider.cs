// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class UseExpressionBodyForOperatorsCodeFixProvider : AbstractUseExpressionBodyCodeFixProvider<OperatorDeclarationSyntax>
    {
        public UseExpressionBodyForOperatorsCodeFixProvider()
            : base(IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId,
                   CSharpCodeStyleOptions.PreferExpressionBodiedOperators,
                   FeaturesResources.Use_expression_body_for_operators,
                   FeaturesResources.Use_block_body_for_operators)
        {
        }

        protected override SyntaxToken GetSemicolonToken(OperatorDeclarationSyntax declaration)
            => declaration.SemicolonToken;

        protected override ArrowExpressionClauseSyntax GetExpressionBody(OperatorDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        protected override BlockSyntax GetBody(OperatorDeclarationSyntax declaration)
            => declaration.Body;

        protected override OperatorDeclarationSyntax WithSemicolonToken(OperatorDeclarationSyntax declaration, SyntaxToken token)
            => declaration.WithSemicolonToken(token);

        protected override OperatorDeclarationSyntax WithExpressionBody(OperatorDeclarationSyntax declaration, ArrowExpressionClauseSyntax expressionBody)
            => declaration.WithExpressionBody(expressionBody);

        protected override OperatorDeclarationSyntax WithBody(OperatorDeclarationSyntax declaration, BlockSyntax body)
            => declaration.WithBody(body);

        protected override bool CreateReturnStatementForExpression(OperatorDeclarationSyntax declaration)
            => true;
    }

}