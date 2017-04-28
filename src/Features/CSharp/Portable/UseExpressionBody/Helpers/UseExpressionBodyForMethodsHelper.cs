// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal class UseExpressionBodyForMethodsHelper : 
        AbstractUseExpressionBodyHelper<MethodDeclarationSyntax>
    {
        public static readonly UseExpressionBodyForMethodsHelper Instance = new UseExpressionBodyForMethodsHelper();

        private UseExpressionBodyForMethodsHelper()
            : base(new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_methods), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_block_body_for_methods), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   CSharpCodeStyleOptions.PreferExpressionBodiedMethods)
        {
        }

        public override BlockSyntax GetBody(MethodDeclarationSyntax declaration)
            => declaration.Body;

        public override ArrowExpressionClauseSyntax GetExpressionBody(MethodDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        protected override SyntaxToken GetSemicolonToken(MethodDeclarationSyntax declaration)
            => declaration.SemicolonToken;

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