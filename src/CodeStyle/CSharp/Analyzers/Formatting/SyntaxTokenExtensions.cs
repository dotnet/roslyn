// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class SyntaxTokenExtensions
    {
        public static bool IsKindOrHasMatchingText(this SyntaxToken token, SyntaxKind kind)
        {
            return token.Kind() == kind || token.HasMatchingText(kind);
        }

        public static bool HasMatchingText(this SyntaxToken token, SyntaxKind kind)
        {
            return token.ToString() == SyntaxFacts.GetText(kind);
        }

        public static bool IsKind(this SyntaxToken token, params SyntaxKind[] kinds)
        {
            return kinds.Contains(token.Kind());
        }

        public static bool IsOpenBraceOrCommaOfObjectInitializer(this SyntaxToken token)
        {
            return (token.IsKind(SyntaxKind.OpenBraceToken) || token.IsKind(SyntaxKind.CommaToken)) &&
                token.Parent.IsKind(SyntaxKind.ObjectInitializerExpression);
        }

        public static bool IsOpenBraceOfAccessorList(this SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.OpenBraceToken) && token.Parent.IsKind(SyntaxKind.AccessorList);
        }

        public static bool IsLastTokenOfNode<T>(this SyntaxToken token)
            where T : SyntaxNode
        {
            var node = token.GetAncestor<T>();
            return node != null && token == node.GetLastToken(includeZeroWidth: true);
        }
    }
}
