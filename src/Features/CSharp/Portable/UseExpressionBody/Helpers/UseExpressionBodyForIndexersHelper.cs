// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal class UseExpressionBodyForIndexersHelper :
        UseExpressionBodyHelper<IndexerDeclarationSyntax>
    {
        public static readonly UseExpressionBodyForIndexersHelper Instance = new UseExpressionBodyForIndexersHelper();

        private UseExpressionBodyForIndexersHelper()
            : base(IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_indexers), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_block_body_for_indexers), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   CSharpCodeStyleOptions.PreferExpressionBodiedIndexers,
                   ImmutableArray.Create(SyntaxKind.IndexerDeclaration))
        {
        }

        protected override BlockSyntax GetBody(IndexerDeclarationSyntax declaration)
            => GetBodyFromSingleGetAccessor(declaration.AccessorList);

        protected override ArrowExpressionClauseSyntax GetExpressionBody(IndexerDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        protected override SyntaxToken GetSemicolonToken(IndexerDeclarationSyntax declaration)
            => declaration.SemicolonToken;

        protected override IndexerDeclarationSyntax WithSemicolonToken(IndexerDeclarationSyntax declaration, SyntaxToken token)
            => declaration.WithSemicolonToken(token);

        protected override IndexerDeclarationSyntax WithExpressionBody(IndexerDeclarationSyntax declaration, ArrowExpressionClauseSyntax expressionBody)
            => declaration.WithExpressionBody(expressionBody);

        protected override IndexerDeclarationSyntax WithAccessorList(IndexerDeclarationSyntax declaration, AccessorListSyntax accessorList)
            => declaration.WithAccessorList(accessorList);

        protected override IndexerDeclarationSyntax WithBody(IndexerDeclarationSyntax declaration, BlockSyntax body)
        {
            if (body == null)
            {
                return declaration.WithAccessorList(null);
            }

            throw new InvalidOperationException();
        }

        protected override IndexerDeclarationSyntax WithGenerateBody(
            SemanticModel semanticModel, IndexerDeclarationSyntax declaration, OptionSet options, ParseOptions parseOptions)
        {
            return WithAccessorList(semanticModel, declaration, options, parseOptions);
        }

        protected override bool CreateReturnStatementForExpression(SemanticModel semanticModel, IndexerDeclarationSyntax declaration) => true;

        protected override bool TryConvertToExpressionBody(
            IndexerDeclarationSyntax declaration, ParseOptions options,
            ExpressionBodyPreference conversionPreference,
            out ArrowExpressionClauseSyntax arrowExpression,
            out SyntaxToken semicolonToken)
        {
            return this.TryConvertToExpressionBodyForBaseProperty(
                declaration, options, conversionPreference,
                out arrowExpression, out semicolonToken);
        }

        protected override Location GetDiagnosticLocation(IndexerDeclarationSyntax declaration)
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
