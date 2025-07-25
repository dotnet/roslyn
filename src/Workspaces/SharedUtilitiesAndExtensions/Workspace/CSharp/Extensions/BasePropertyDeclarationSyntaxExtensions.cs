// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class BasePropertyDeclarationSyntaxExtensions
{
    extension(BasePropertyDeclarationSyntax? node)
    {
        /// <summary>
        /// Available if <paramref name="node"/> is <see cref="PropertyDeclarationSyntax"/> or <see cref="IndexerDeclarationSyntax"/>.
        /// </summary>
        public SyntaxToken TryGetSemicolonToken()
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
        public BasePropertyDeclarationSyntax? TryWithSemicolonToken(SyntaxToken semicolonToken)
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
        public BasePropertyDeclarationSyntax? TryWithExpressionBody(ArrowExpressionClauseSyntax? expressionBody)
            => node switch
            {
                PropertyDeclarationSyntax property => property.WithExpressionBody(expressionBody),
                IndexerDeclarationSyntax indexer => indexer.WithExpressionBody(expressionBody),
                _ => node,
            };
    }
}
