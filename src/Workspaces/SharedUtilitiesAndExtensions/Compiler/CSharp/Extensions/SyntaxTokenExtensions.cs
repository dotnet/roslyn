﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class SyntaxTokenExtensions
    {
        public static bool IsLastTokenOfNode<T>(this SyntaxToken token)
            where T : SyntaxNode
        {
            var node = token.GetAncestor<T>();
            return node != null && token == node.GetLastToken(includeZeroWidth: true);
        }

        public static bool IsKindOrHasMatchingText(this SyntaxToken token, SyntaxKind kind)
        {
            return token.Kind() == kind || token.HasMatchingText(kind);
        }

        public static bool HasMatchingText(this SyntaxToken token, SyntaxKind kind)
        {
            return token.ToString() == SyntaxFacts.GetText(kind);
        }

        public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2)
        {
            return token.Kind() == kind1
                || token.Kind() == kind2;
        }

        public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3)
        {
            return token.Kind() == kind1
                || token.Kind() == kind2
                || token.Kind() == kind3;
        }

        public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4)
        {
            return token.Kind() == kind1
                || token.Kind() == kind2
                || token.Kind() == kind3
                || token.Kind() == kind4;
        }

        public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4, SyntaxKind kind5)
        {
            return token.Kind() == kind1
                || token.Kind() == kind2
                || token.Kind() == kind3
                || token.Kind() == kind4
                || token.Kind() == kind5;
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

        /// <summary>
        /// Returns true if this token is something that looks like a C# keyword. This includes 
        /// actual keywords, contextual keywords, and even 'var' and 'dynamic'
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static bool CouldBeKeyword(this SyntaxToken token)
        {
            if (token.IsKeyword())
            {
                return true;
            }

            if (token.Kind() == SyntaxKind.IdentifierToken)
            {
                var simpleNameText = token.ValueText;
                return simpleNameText == "var" ||
                       simpleNameText == "dynamic" ||
                       SyntaxFacts.GetContextualKeywordKind(simpleNameText) != SyntaxKind.None;
            }

            return false;
        }
    }
}
