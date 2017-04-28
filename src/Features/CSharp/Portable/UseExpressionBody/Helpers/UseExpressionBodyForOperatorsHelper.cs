// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal class UseExpressionBodyForOperatorsHelper :
        AbstractUseExpressionBodyHelper<OperatorDeclarationSyntax>
    {
        public static readonly UseExpressionBodyForOperatorsHelper Instance = new UseExpressionBodyForOperatorsHelper();

        private UseExpressionBodyForOperatorsHelper()
            : base(new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_operators), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_block_body_for_operators), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   CSharpCodeStyleOptions.PreferExpressionBodiedOperators)
        {
        }

        public override BlockSyntax GetBody(OperatorDeclarationSyntax declaration)
            => declaration.Body;

        public override ArrowExpressionClauseSyntax GetExpressionBody(OperatorDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        protected override SyntaxToken GetSemicolonToken(OperatorDeclarationSyntax declaration)
            => declaration.SemicolonToken;

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