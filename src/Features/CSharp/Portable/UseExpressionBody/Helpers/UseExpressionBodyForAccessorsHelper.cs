// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal class UseExpressionBodyForAccessorsHelper : 
        AbstractUseExpressionBodyHelper<AccessorDeclarationSyntax>
    {
        public static readonly UseExpressionBodyForAccessorsHelper Instance = new UseExpressionBodyForAccessorsHelper();

        //private static readonly Func<OptionSet, PropertyDeclarationSyntax, bool, bool> _propertyCanOfferUseExpressionBody = UseExpressionBodyForPropertiesHelper.Instance.CanOfferUseExpressionBody;
        //private static readonly Func<OptionSet, PropertyDeclarationSyntax, bool, bool> _propertyCanOfferUseBlockBody = UseExpressionBodyForPropertiesHelper.Instance.CanOfferUseBlockBody;
        //private static readonly Func<OptionSet, IndexerDeclarationSyntax, bool, bool> _indexerCanOfferUseExpressionBody = UseExpressionBodyForIndexersHelper.Instance.CanOfferUseExpressionBody;
        //private static readonly Func<OptionSet, IndexerDeclarationSyntax, bool, bool> _indexerCanOfferUseBlockBody = UseExpressionBodyForIndexersHelper.Instance.CanOfferUseBlockBody;

        //private readonly Func<OptionSet, AccessorDeclarationSyntax, bool, bool> _baseCanOfferUseExpressionBody;
        //private readonly Func<OptionSet, AccessorDeclarationSyntax, bool, bool> _baseCanOfferUseBlockBody;

        private UseExpressionBodyForAccessorsHelper()
            : base(IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_accessors), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_block_body_for_accessors), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   CSharpCodeStyleOptions.PreferExpressionBodiedAccessors,
                   ImmutableArray.Create(SyntaxKind.GetAccessorDeclaration, SyntaxKind.SetAccessorDeclaration))
        {
            //_baseCanOfferUseExpressionBody = base.CanOfferUseExpressionBody;
            //_baseCanOfferUseBlockBody = base.CanOfferUseBlockBody;
        }

        protected override BlockSyntax GetBody(AccessorDeclarationSyntax declaration)
            => declaration.Body;

        protected override ArrowExpressionClauseSyntax GetExpressionBody(AccessorDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        //private bool CanOffer(
        //    OptionSet optionSet, AccessorDeclarationSyntax accessor, bool forAnalyzer,
        //    Func<OptionSet, PropertyDeclarationSyntax, bool, bool> propertyPredicate,
        //    Func<OptionSet, IndexerDeclarationSyntax, bool, bool> indexerPredicate,
        //    Func<OptionSet, AccessorDeclarationSyntax, bool, bool> basePredicate)
        //{
        //    var grandParent = accessor.Parent.Parent;

        //    if (grandParent.IsKind(SyntaxKind.PropertyDeclaration))
        //    {
        //        var propertyDeclaration = (PropertyDeclarationSyntax)grandParent;
        //        if (propertyPredicate(optionSet, propertyDeclaration, forAnalyzer))
        //        {
        //            return false;
        //        }
        //    }
        //    else if (grandParent.IsKind(SyntaxKind.IndexerDeclaration))
        //    {
        //        var indexerDeclaration = (IndexerDeclarationSyntax)grandParent;
        //        if (indexerPredicate(optionSet, indexerDeclaration, forAnalyzer))
        //        {
        //            return false;
        //        }
        //    }

        //    return basePredicate(optionSet, accessor, forAnalyzer);
        //}

        //public override bool CanOfferUseExpressionBody(OptionSet optionSet, AccessorDeclarationSyntax accessor, bool forAnalyzer)
        //{
        //    return CanOffer(
        //        optionSet, accessor, forAnalyzer,
        //        _propertyCanOfferUseExpressionBody, _indexerCanOfferUseExpressionBody, _baseCanOfferUseExpressionBody);
        //}

        //public override bool CanOfferUseBlockBody(
        //    OptionSet optionSet, AccessorDeclarationSyntax accessor, bool forAnalyzer)
        //{
        //    return CanOffer(
        //        optionSet, accessor, forAnalyzer,
        //        _propertyCanOfferUseBlockBody, _indexerCanOfferUseBlockBody, _baseCanOfferUseBlockBody);
        //}

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