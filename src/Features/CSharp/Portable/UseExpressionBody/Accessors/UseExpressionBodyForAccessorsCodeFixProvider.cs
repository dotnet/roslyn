// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class UseExpressionBodyForAccessorsCodeFixProvider : AbstractUseExpressionBodyCodeFixProvider<AccessorDeclarationSyntax>
    {
        public UseExpressionBodyForAccessorsCodeFixProvider()
            : base(IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId,
                   CSharpCodeStyleOptions.PreferExpressionBodiedAccessors,
                   FeaturesResources.Use_expression_body_for_accessors,
                   FeaturesResources.Use_block_body_for_accessors)
        {
        }

        protected override SyntaxToken GetSemicolonToken(AccessorDeclarationSyntax declaration)
            => declaration.SemicolonToken;

        protected override ArrowExpressionClauseSyntax GetExpressionBody(AccessorDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        protected override BlockSyntax GetBody(AccessorDeclarationSyntax declaration)
            => declaration.Body;

        protected override AccessorDeclarationSyntax WithSemicolonToken(AccessorDeclarationSyntax declaration, SyntaxToken token)
            => declaration.WithSemicolonToken(token);

        protected override AccessorDeclarationSyntax WithExpressionBody(AccessorDeclarationSyntax declaration, ArrowExpressionClauseSyntax expressionBody)
            => declaration.WithExpressionBody(expressionBody);

        protected override AccessorDeclarationSyntax WithBody(AccessorDeclarationSyntax declaration, BlockSyntax body)
            => declaration.WithBody(body);

        protected override bool CreateReturnStatementForExpression(AccessorDeclarationSyntax declaration)
            => declaration.IsKind(SyntaxKind.GetAccessorDeclaration);
    }
}