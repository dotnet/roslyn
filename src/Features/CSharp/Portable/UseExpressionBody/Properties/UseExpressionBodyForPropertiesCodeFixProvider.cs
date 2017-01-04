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