// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class UseExpressionBodyForMethodsCodeFixProvider : AbstractUseExpressionBodyCodeFixProvider<MethodDeclarationSyntax>
    {
        public UseExpressionBodyForMethodsCodeFixProvider()
            : base(IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId,
                   CSharpCodeStyleOptions.PreferExpressionBodiedMethods,
                   FeaturesResources.Use_expression_body_for_methods,
                   FeaturesResources.Use_block_body_for_methods)
        {
        }

        protected override SyntaxToken GetSemicolonToken(MethodDeclarationSyntax declaration)
            => declaration.SemicolonToken;

        protected override ArrowExpressionClauseSyntax GetExpressionBody(MethodDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        protected override BlockSyntax GetBody(MethodDeclarationSyntax declaration)
            => declaration.Body;

        protected override MethodDeclarationSyntax WithSemicolonToken(MethodDeclarationSyntax declaration, SyntaxToken token)
            => declaration.WithSemicolonToken(token);

        protected override MethodDeclarationSyntax WithExpressionBody(MethodDeclarationSyntax declaration, ArrowExpressionClauseSyntax expressionBody)
            => declaration.WithExpressionBody(expressionBody);

        protected override MethodDeclarationSyntax WithBody(MethodDeclarationSyntax declaration, BlockSyntax body)
            => declaration.WithBody(body);

        protected override bool CreateReturnStatementForExpression(MethodDeclarationSyntax declaration)
            => !declaration.ReturnType.IsVoid();
    }

}