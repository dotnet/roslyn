// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal class UseExpressionBodyForPropertiesHelper :
        UseExpressionBodyHelper<PropertyDeclarationSyntax>
    {
        public static readonly UseExpressionBodyForPropertiesHelper Instance = new();

        private UseExpressionBodyForPropertiesHelper()
            : base(IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId,
                   EnforceOnBuildValues.UseExpressionBodyForProperties,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_expression_body_for_property), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_block_body_for_property), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                   CSharpCodeStyleOptions.PreferExpressionBodiedProperties,
                   ImmutableArray.Create(SyntaxKind.PropertyDeclaration))
        {
        }

        public override CodeStyleOption2<ExpressionBodyPreference> GetExpressionBodyPreference(CSharpCodeGenerationOptions options)
            => options.PreferExpressionBodiedProperties;

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
            => WithAccessorList(semanticModel, declaration);

        protected override bool CreateReturnStatementForExpression(SemanticModel semanticModel, PropertyDeclarationSyntax declaration) => true;

        protected override bool TryConvertToExpressionBody(
            PropertyDeclarationSyntax declaration,
            ExpressionBodyPreference conversionPreference,
            CancellationToken cancellationToken,
            out ArrowExpressionClauseSyntax arrowExpression,
            out SyntaxToken semicolonToken)
        {
            return TryConvertToExpressionBodyForBaseProperty(declaration, conversionPreference, cancellationToken, out arrowExpression, out semicolonToken);
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
