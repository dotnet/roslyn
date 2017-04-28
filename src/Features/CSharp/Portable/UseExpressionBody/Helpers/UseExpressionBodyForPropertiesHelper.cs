// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal class UseExpressionBodyForPropertiesHelper :
        AbstractUseExpressionBodyHelper<PropertyDeclarationSyntax>
    {
        public static readonly UseExpressionBodyForPropertiesHelper Instance = new UseExpressionBodyForPropertiesHelper();

        private UseExpressionBodyForPropertiesHelper()
            : base(new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_properties), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_block_body_for_properties), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   CSharpCodeStyleOptions.PreferExpressionBodiedProperties)
        {
        }

        public override BlockSyntax GetBody(PropertyDeclarationSyntax declaration)
            => GetBodyFromSingleGetAccessor(declaration.AccessorList);

        public override ArrowExpressionClauseSyntax GetExpressionBody(PropertyDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        protected override SyntaxToken GetSemicolonToken(PropertyDeclarationSyntax declaration)
            => declaration.SemicolonToken;

        protected override PropertyDeclarationSyntax WithSemicolonToken(PropertyDeclarationSyntax declaration, SyntaxToken token)
            => declaration.WithSemicolonToken(token);

        protected override PropertyDeclarationSyntax WithExpressionBody(PropertyDeclarationSyntax declaration, ArrowExpressionClauseSyntax expressionBody)
            => declaration.WithExpressionBody(expressionBody);

        protected override PropertyDeclarationSyntax WithAccessorList(PropertyDeclarationSyntax declaration, AccessorListSyntax accessorListSyntax)
            => declaration.WithAccessorList(accessorListSyntax);

        protected override PropertyDeclarationSyntax WithBody(PropertyDeclarationSyntax declaration, BlockSyntax body)
        {
            if (body == null)
            {
                return declaration.WithAccessorList(null);
            }

            throw new InvalidOperationException();
        }

        protected override PropertyDeclarationSyntax WithGenerateBody(
            PropertyDeclarationSyntax declaration, OptionSet options)
        {
            return WithAccessorList(declaration, options);
        }

        protected override bool CreateReturnStatementForExpression(PropertyDeclarationSyntax declaration) => true;
    }
}