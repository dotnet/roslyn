// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal class UseExpressionBodyForConstructorsHelper :
        UseExpressionBodyHelper<ConstructorDeclarationSyntax>
    {
        public static readonly UseExpressionBodyForConstructorsHelper Instance = new UseExpressionBodyForConstructorsHelper();

        private UseExpressionBodyForConstructorsHelper()
            : base(IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_expression_body_for_constructors), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_block_body_for_constructors), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                   CSharpCodeStyleOptions.PreferExpressionBodiedConstructors,
                   ImmutableArray.Create(SyntaxKind.ConstructorDeclaration))
        {
        }

        protected override BlockSyntax GetBody(ConstructorDeclarationSyntax declaration)
            => declaration.Body;

        protected override ArrowExpressionClauseSyntax GetExpressionBody(ConstructorDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        protected override SyntaxToken GetSemicolonToken(ConstructorDeclarationSyntax declaration)
            => declaration.SemicolonToken;

        protected override ConstructorDeclarationSyntax WithSemicolonToken(ConstructorDeclarationSyntax declaration, SyntaxToken token)
            => declaration.WithSemicolonToken(token);

        protected override ConstructorDeclarationSyntax WithExpressionBody(ConstructorDeclarationSyntax declaration, ArrowExpressionClauseSyntax expressionBody)
            => declaration.WithExpressionBody(expressionBody);

        protected override ConstructorDeclarationSyntax WithBody(ConstructorDeclarationSyntax declaration, BlockSyntax body)
            => declaration.WithBody(body);

        protected override bool CreateReturnStatementForExpression(SemanticModel semanticModel, ConstructorDeclarationSyntax declaration) => false;
    }
}
