// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class BasePropertyDeclarationSyntaxExtensions
    {
        /// <summary>
        /// Available if <paramref name="node"/> is <see cref="PropertyDeclarationSyntax"/> or <see cref="IndexerDeclarationSyntax"/>.
        /// </summary>
        public static SyntaxToken TryGetSemicolonToken(this BasePropertyDeclarationSyntax? node)
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
        [return: NotNullIfNotNull(nameof(node))]
        public static BasePropertyDeclarationSyntax? TryWithSemicolonToken(this BasePropertyDeclarationSyntax? node, SyntaxToken semicolonToken)
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

        /// <summary>
        /// Available if <paramref name="node"/> is <see cref="PropertyDeclarationSyntax"/> or <see cref="IndexerDeclarationSyntax"/>.
        /// </summary>
        [return: NotNullIfNotNull(nameof(node))]
        public static BasePropertyDeclarationSyntax? TryWithExpressionBody(this BasePropertyDeclarationSyntax? node, ArrowExpressionClauseSyntax expressionBody)
        {
            if (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.PropertyDeclaration: return ((PropertyDeclarationSyntax)node).WithExpressionBody(expressionBody);
                    case SyntaxKind.IndexerDeclaration: return ((IndexerDeclarationSyntax)node).WithExpressionBody(expressionBody);
                }
            }

            return node;
        }
    }
}
