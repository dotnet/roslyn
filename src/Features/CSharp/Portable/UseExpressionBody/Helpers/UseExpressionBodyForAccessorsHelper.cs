// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal class UseExpressionBodyForAccessorsHelper : 
        AbstractUseExpressionBodyHelper<AccessorDeclarationSyntax>
    {
        private readonly UseExpressionBodyForPropertiesDiagnosticAnalyzer propertyAnalyzer = new UseExpressionBodyForPropertiesDiagnosticAnalyzer();
        private readonly UseExpressionBodyForIndexersDiagnosticAnalyzer indexerAnalyzer = new UseExpressionBodyForIndexersDiagnosticAnalyzer();

        public UseExpressionBodyForAccessorsHelper()
            : base(new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_accessors), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_block_body_for_accessors), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   CSharpCodeStyleOptions.PreferExpressionBodiedAccessors)
        {
        }

        public override BlockSyntax GetBody(AccessorDeclarationSyntax declaration)
            => declaration.Body;

        public override ArrowExpressionClauseSyntax GetExpressionBody(AccessorDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        public override bool CanOfferUseExpressionBody(
            OptionSet optionSet, AccessorDeclarationSyntax accessor, bool forAnalyzer)
        {
            var grandParent = accessor.Parent.Parent;

            if (grandParent.IsKind(SyntaxKind.PropertyDeclaration))
            {
                var propertyDeclaration = (PropertyDeclarationSyntax)grandParent;
                if (UseExpressionBodyForPropertiesHelper.Instance.CanOfferUseExpressionBody(
                        optionSet, propertyDeclaration, forAnalyzer))
                {
                    return false;
                }
            }
            else if (grandParent.IsKind(SyntaxKind.IndexerDeclaration))
            {
                var indexerDeclaration = (IndexerDeclarationSyntax)grandParent;
                if (UseExpressionBodyForIndexersHelper.Instance.CanOfferUseExpressionBody(
                        optionSet, indexerDeclaration, forAnalyzer))
                {
                    return false;
                }
            }

            return base.CanOfferUseExpressionBody(optionSet, accessor, forAnalyzer);
        }

        public override bool CanOfferUseBlockBody(
            OptionSet optionSet, AccessorDeclarationSyntax accessor, bool forAnalyzer)
        {
            var grandParent = accessor.Parent.Parent;

            if (grandParent.IsKind(SyntaxKind.PropertyDeclaration))
            {
                var propertyDeclaration = (PropertyDeclarationSyntax)grandParent;
                if (UseExpressionBodyForPropertiesHelper.Instance.CanOfferUseBlockBody(
                        optionSet, propertyDeclaration, forAnalyzer))
                {
                    return false;
                }
            }
            else if (grandParent.IsKind(SyntaxKind.IndexerDeclaration))
            {
                var indexerDeclaration = (IndexerDeclarationSyntax)grandParent;
                if (UseExpressionBodyForIndexersHelper.Instance.CanOfferUseBlockBody(
                        optionSet, indexerDeclaration, forAnalyzer))
                {
                    return false;
                }
            }

            return base.CanOfferUseBlockBody(optionSet, accessor, forAnalyzer);
        }

        protected override SyntaxToken GetSemicolonToken(AccessorDeclarationSyntax declaration)
            => declaration.SemicolonToken;

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