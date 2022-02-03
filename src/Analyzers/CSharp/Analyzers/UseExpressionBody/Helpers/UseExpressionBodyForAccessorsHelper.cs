// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal class UseExpressionBodyForAccessorsHelper :
        UseExpressionBodyHelper<AccessorDeclarationSyntax>
    {
        public static readonly UseExpressionBodyForAccessorsHelper Instance = new();

        private UseExpressionBodyForAccessorsHelper()
            : base(IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId,
                   EnforceOnBuildValues.UseExpressionBodyForAccessors,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_expression_body_for_accessors), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_block_body_for_accessors), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                   CSharpCodeStyleOptions.PreferExpressionBodiedAccessors,
                   ImmutableArray.Create(SyntaxKind.GetAccessorDeclaration, SyntaxKind.SetAccessorDeclaration, SyntaxKind.InitAccessorDeclaration, SyntaxKind.AddAccessorDeclaration, SyntaxKind.RemoveAccessorDeclaration))
        {
        }

        protected override BlockSyntax? GetBody(AccessorDeclarationSyntax declaration)
            => declaration.Body;

        protected override ArrowExpressionClauseSyntax? GetExpressionBody(AccessorDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        protected override SyntaxToken GetSemicolonToken(AccessorDeclarationSyntax declaration)
            => declaration.SemicolonToken;

        protected override AccessorDeclarationSyntax WithSemicolonToken(AccessorDeclarationSyntax declaration, SyntaxToken token)
            => declaration.WithSemicolonToken(token);

        protected override AccessorDeclarationSyntax WithExpressionBody(AccessorDeclarationSyntax declaration, ArrowExpressionClauseSyntax? expressionBody)
            => declaration.WithExpressionBody(expressionBody);

        protected override AccessorDeclarationSyntax WithBody(AccessorDeclarationSyntax declaration, BlockSyntax? body)
            => declaration.WithBody(body);

        protected override bool CreateReturnStatementForExpression(SemanticModel semanticModel, AccessorDeclarationSyntax declaration)
            => declaration.IsKind(SyntaxKind.GetAccessorDeclaration);
    }
}
