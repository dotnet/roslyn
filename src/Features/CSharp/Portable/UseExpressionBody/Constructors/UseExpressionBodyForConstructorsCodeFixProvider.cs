// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class UseExpressionBodyForConstructorsCodeFixProvider : AbstractUseExpressionBodyCodeFixProvider<ConstructorDeclarationSyntax>
    {
        public UseExpressionBodyForConstructorsCodeFixProvider()
            : base(IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId,
                   CSharpCodeStyleOptions.PreferExpressionBodiedConstructors,
                   FeaturesResources.Use_expression_body_for_constructors,
                   FeaturesResources.Use_block_body_for_constructors)
        {
        }

        protected override SyntaxToken GetSemicolonToken(ConstructorDeclarationSyntax declaration)
            => declaration.SemicolonToken;

        protected override ArrowExpressionClauseSyntax GetExpressionBody(ConstructorDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        protected override BlockSyntax GetBody(ConstructorDeclarationSyntax declaration)
            => declaration.Body;

        protected override ConstructorDeclarationSyntax WithSemicolonToken(ConstructorDeclarationSyntax declaration, SyntaxToken token)
            => declaration.WithSemicolonToken(token);

        protected override ConstructorDeclarationSyntax WithExpressionBody(ConstructorDeclarationSyntax declaration, ArrowExpressionClauseSyntax expressionBody)
            => declaration.WithExpressionBody(expressionBody);

        protected override ConstructorDeclarationSyntax WithBody(ConstructorDeclarationSyntax declaration, BlockSyntax body)
            => declaration.WithBody(body);

        protected override bool CreateReturnStatementForExpression(ConstructorDeclarationSyntax declaration) => false;
    }
}