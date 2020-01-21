// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal class UseExpressionBodyForAccessorsHelper :
        UseExpressionBodyHelper<AccessorDeclarationSyntax>
    {
        public static readonly UseExpressionBodyForAccessorsHelper Instance = new UseExpressionBodyForAccessorsHelper();

        private UseExpressionBodyForAccessorsHelper()
            : base(IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_accessors), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_block_body_for_accessors), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   CSharpCodeStyleOptions.PreferExpressionBodiedAccessors,
                   ImmutableArray.Create(SyntaxKind.GetAccessorDeclaration, SyntaxKind.SetAccessorDeclaration))
        {
        }

        protected override BlockSyntax GetBody(AccessorDeclarationSyntax declaration)
            => declaration.Body;

        protected override ArrowExpressionClauseSyntax GetExpressionBody(AccessorDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        protected override SyntaxToken GetSemicolonToken(AccessorDeclarationSyntax declaration)
            => declaration.SemicolonToken;

        protected override AccessorDeclarationSyntax WithSemicolonToken(AccessorDeclarationSyntax declaration, SyntaxToken token)
            => declaration.WithSemicolonToken(token);

        protected override AccessorDeclarationSyntax WithExpressionBody(AccessorDeclarationSyntax declaration, ArrowExpressionClauseSyntax expressionBody)
            => declaration.WithExpressionBody(expressionBody);

        protected override AccessorDeclarationSyntax WithBody(AccessorDeclarationSyntax declaration, BlockSyntax body)
            => declaration.WithBody(body);

        protected override bool CreateReturnStatementForExpression(SemanticModel semanticModel, AccessorDeclarationSyntax declaration)
            => declaration.IsKind(SyntaxKind.GetAccessorDeclaration);
    }
}
