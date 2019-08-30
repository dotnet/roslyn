// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal class UseExpressionBodyForPropertiesHelper :
        UseExpressionBodyHelper<PropertyDeclarationSyntax>
    {
        public static readonly UseExpressionBodyForPropertiesHelper Instance = new UseExpressionBodyForPropertiesHelper();

        private UseExpressionBodyForPropertiesHelper()
            : base(IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_properties), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_block_body_for_properties), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   CSharpCodeStyleOptions.PreferExpressionBodiedProperties,
                   ImmutableArray.Create(SyntaxKind.PropertyDeclaration))
        {
        }

        protected override BlockSyntax GetBody(PropertyDeclarationSyntax declaration)
            => GetBodyFromSingleGetAccessor(declaration.AccessorList);

        protected override ArrowExpressionClauseSyntax GetExpressionBody(PropertyDeclarationSyntax declaration)
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

        protected override PropertyDeclarationSyntax WithGenerateBody(SemanticModel semanticModel, PropertyDeclarationSyntax declaration)
        {
            return WithAccessorList(semanticModel, declaration);
        }

        protected override bool CreateReturnStatementForExpression(SemanticModel semanticModel, PropertyDeclarationSyntax declaration) => true;

        protected override bool TryConvertToExpressionBody(
            PropertyDeclarationSyntax declaration, ParseOptions options,
            ExpressionBodyPreference conversionPreference,
            out ArrowExpressionClauseSyntax arrowExpression,
            out SyntaxToken semicolonToken)
        {
            return this.TryConvertToExpressionBodyForBaseProperty(
                declaration, options, conversionPreference,
                out arrowExpression, out semicolonToken);
        }

        protected override Location GetDiagnosticLocation(PropertyDeclarationSyntax declaration)
        {
            var body = GetBody(declaration);
            if (body != null)
            {
                return base.GetDiagnosticLocation(declaration);
            }

            var getAccessor = GetSingleGetAccessor(declaration.AccessorList);
            return getAccessor.ExpressionBody.GetLocation();
        }
    }
}
