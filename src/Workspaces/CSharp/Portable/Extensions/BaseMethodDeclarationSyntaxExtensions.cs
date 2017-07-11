// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class BaseMethodDeclarationSyntaxExtensions
    {
        public static BaseMethodDeclarationSyntax WithSemicolonToken(this BaseMethodDeclarationSyntax node, SyntaxToken token)
        {
            if (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.ConstructorDeclaration: return ((ConstructorDeclarationSyntax)node).WithSemicolonToken(token);
                    case SyntaxKind.DestructorDeclaration: return ((DestructorDeclarationSyntax)node).WithSemicolonToken(token);
                    case SyntaxKind.MethodDeclaration: return ((MethodDeclarationSyntax)node).WithSemicolonToken(token);
                    case SyntaxKind.OperatorDeclaration: return ((OperatorDeclarationSyntax)node).WithSemicolonToken(token);
                    case SyntaxKind.ConversionOperatorDeclaration: return ((ConversionOperatorDeclarationSyntax)node).WithSemicolonToken(token);
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
    }
}
