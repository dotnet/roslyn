// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal class UseExpressionBodyForMethodsHelper :
        UseExpressionBodyHelper<MethodDeclarationSyntax>
    {
        public static readonly UseExpressionBodyForMethodsHelper Instance = new UseExpressionBodyForMethodsHelper();

        private UseExpressionBodyForMethodsHelper()
            : base(IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_methods), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_block_body_for_methods), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   CSharpCodeStyleOptions.PreferExpressionBodiedMethods,
                   ImmutableArray.Create(SyntaxKind.MethodDeclaration))
        {
        }

        protected override BlockSyntax GetBody(MethodDeclarationSyntax declaration)
            => declaration.Body;

        protected override ArrowExpressionClauseSyntax GetExpressionBody(MethodDeclarationSyntax declaration)
            => declaration.ExpressionBody;

        protected override SyntaxToken GetSemicolonToken(MethodDeclarationSyntax declaration)
            => declaration.SemicolonToken;

        protected override MethodDeclarationSyntax WithSemicolonToken(MethodDeclarationSyntax declaration, SyntaxToken token)
            => declaration.WithSemicolonToken(token);

        protected override MethodDeclarationSyntax WithExpressionBody(MethodDeclarationSyntax declaration, ArrowExpressionClauseSyntax expressionBody)
            => declaration.WithExpressionBody(expressionBody);

        protected override MethodDeclarationSyntax WithBody(MethodDeclarationSyntax declaration, BlockSyntax body)
            => declaration.WithBody(body);

        protected override bool CreateReturnStatementForExpression(
            SemanticModel semanticModel, MethodDeclarationSyntax declaration)
        {
            if (declaration.Modifiers.Any(SyntaxKind.AsyncKeyword))
            {
                // if it's 'async TaskLike' (where TaskLike is non-generic) we do *not* want to
                // create a return statement.  This is just the 'async' version of a 'void' method.
                var method = semanticModel.GetDeclaredSymbol(declaration);
                return method.ReturnType is INamedTypeSymbol namedType && namedType.Arity != 0;
            }

            return !declaration.ReturnType.IsVoid();
        }
    }
}
