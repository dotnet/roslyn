// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{

    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class UseExpressionBodyForPropertiesCodeFixProvider : AbstractUseExpressionBodyCodeFixProvider<PropertyDeclarationSyntax>
    {
        public UseExpressionBodyForPropertiesCodeFixProvider()
            : base(IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId,
                   CSharpCodeStyleOptions.PreferExpressionBodiedProperties,
                   FeaturesResources.Use_expression_body_for_properties,
                   FeaturesResources.Use_block_body_for_properties)
        {
        }

        protected override SyntaxToken GetSemicolonToken(PropertyDeclarationSyntax declaration)
            => declaration.SemicolonToken;

        protected override ArrowExpressionClauseSyntax GetExpressionBody(PropertyDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        protected override BlockSyntax GetBody(PropertyDeclarationSyntax declaration)
            => declaration.AccessorList.Accessors[0].Body;

        protected override PropertyDeclarationSyntax WithSemicolonToken(PropertyDeclarationSyntax declaration, SyntaxToken token)
            => declaration.WithSemicolonToken(token);

        protected override PropertyDeclarationSyntax WithExpressionBody(PropertyDeclarationSyntax declaration, ArrowExpressionClauseSyntax expressionBody)
            => declaration.WithExpressionBody(expressionBody);

        protected override PropertyDeclarationSyntax WithBody(PropertyDeclarationSyntax declaration, BlockSyntax body)
        {
            if (body == null)
            {
                return declaration.WithAccessorList(null);
            }

            return declaration;
        }

        protected override PropertyDeclarationSyntax WithGenerateBody(
            PropertyDeclarationSyntax declaration, OptionSet options)
        {
            var expressionBody = GetExpressionBody(declaration);
            var semicolonToken = GetSemicolonToken(declaration);

            var preferExpressionBodiedAccessors = options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors).Value;

            AccessorDeclarationSyntax accessor;
            if (preferExpressionBodiedAccessors)
            {
                accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                        .WithExpressionBody(expressionBody)
                                        .WithSemicolonToken(semicolonToken);
            }
            else
            {
                var block = expressionBody.ConvertToBlock(
                    GetSemicolonToken(declaration),
                    CreateReturnStatementForExpression(declaration));
                accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, block);
            }

            return declaration.WithAccessorList(SyntaxFactory.AccessorList(
                SyntaxFactory.SingletonList(accessor)));
        }

        protected override bool CreateReturnStatementForExpression(PropertyDeclarationSyntax declaration) => true;
    }

}