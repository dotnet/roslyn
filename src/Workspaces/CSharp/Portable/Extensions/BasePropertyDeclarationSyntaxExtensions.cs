// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class BasePropertyDeclarationSyntaxExtensions
    {
        public static BasePropertyDeclarationSyntax WithAccessorList(this BasePropertyDeclarationSyntax node, AccessorListSyntax accessorList)
        {
            if (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.PropertyDeclaration: return ((PropertyDeclarationSyntax)node).WithAccessorList(accessorList);
                    case SyntaxKind.IndexerDeclaration: return ((IndexerDeclarationSyntax)node).WithAccessorList(accessorList);
                    case SyntaxKind.EventDeclaration: return ((EventDeclarationSyntax)node).WithAccessorList(accessorList);
                }
            }

            return node;
        }

        /// <summary>
        /// Available if <paramref name="node"/> is <see cref="PropertyDeclarationSyntax"/> or <see cref="IndexerDeclarationSyntax"/>.
        /// </summary>
        public static SyntaxToken TryGetSemicolonToken(this BasePropertyDeclarationSyntax node)
        {
            if (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.PropertyDeclaration: return ((PropertyDeclarationSyntax)node).SemicolonToken;
                    case SyntaxKind.IndexerDeclaration: return ((IndexerDeclarationSyntax)node).SemicolonToken;
                }
            }

            return default;
        }

        /// <summary>
        /// Available if <paramref name="node"/> is <see cref="PropertyDeclarationSyntax"/> or <see cref="IndexerDeclarationSyntax"/>.
        /// </summary>
        public static BasePropertyDeclarationSyntax TryWithSemicolonToken(this BasePropertyDeclarationSyntax node, SyntaxToken semicolonToken)
        {
            if (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.PropertyDeclaration: return ((PropertyDeclarationSyntax)node).WithSemicolonToken(semicolonToken);
                    case SyntaxKind.IndexerDeclaration: return ((IndexerDeclarationSyntax)node).WithSemicolonToken(semicolonToken);
                }
            }

            return node;
        }
    }
}
