// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class BaseMethodDeclarationSyntaxExtensions
    {
        public static BaseMethodDeclarationSyntax WithSemicolonToken(this BaseMethodDeclarationSyntax node, SyntaxToken semicolonToken)
        {
            if (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.ConstructorDeclaration: return ((ConstructorDeclarationSyntax)node).WithSemicolonToken(semicolonToken);
                    case SyntaxKind.DestructorDeclaration: return ((DestructorDeclarationSyntax)node).WithSemicolonToken(semicolonToken);
                    case SyntaxKind.MethodDeclaration: return ((MethodDeclarationSyntax)node).WithSemicolonToken(semicolonToken);
                    case SyntaxKind.OperatorDeclaration: return ((OperatorDeclarationSyntax)node).WithSemicolonToken(semicolonToken);
                    case SyntaxKind.ConversionOperatorDeclaration: return ((ConversionOperatorDeclarationSyntax)node).WithSemicolonToken(semicolonToken);
                }
            }

            return node;
        }

        public static BaseMethodDeclarationSyntax WithBody(this BaseMethodDeclarationSyntax node, BlockSyntax body)
        {
            if (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.ConstructorDeclaration: return ((ConstructorDeclarationSyntax)node).WithBody(body);
                    case SyntaxKind.DestructorDeclaration: return ((DestructorDeclarationSyntax)node).WithBody(body);
                    case SyntaxKind.MethodDeclaration: return ((MethodDeclarationSyntax)node).WithBody(body);
                    case SyntaxKind.OperatorDeclaration: return ((OperatorDeclarationSyntax)node).WithBody(body);
                    case SyntaxKind.ConversionOperatorDeclaration: return ((ConversionOperatorDeclarationSyntax)node).WithBody(body);
                }
            }

            return node;
        }

        public static BaseMethodDeclarationSyntax WithExpressionBody(this BaseMethodDeclarationSyntax node, ArrowExpressionClauseSyntax expressionBody)
        {
            if (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.ConstructorDeclaration: return ((ConstructorDeclarationSyntax)node).WithExpressionBody(expressionBody);
                    case SyntaxKind.DestructorDeclaration: return ((DestructorDeclarationSyntax)node).WithExpressionBody(expressionBody);
                    case SyntaxKind.MethodDeclaration: return ((MethodDeclarationSyntax)node).WithExpressionBody(expressionBody);
                    case SyntaxKind.OperatorDeclaration: return ((OperatorDeclarationSyntax)node).WithExpressionBody(expressionBody);
                    case SyntaxKind.ConversionOperatorDeclaration: return ((ConversionOperatorDeclarationSyntax)node).WithExpressionBody(expressionBody);
                }
            }

            return node;
        }
    }
}
