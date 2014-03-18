// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class CommonSyntaxTokenExtensions
    {
        public static SyntaxNode GetAncestor(this SyntaxToken token, Func<SyntaxNode, bool> predicate)
        {
            return token.GetAncestors(predicate).FirstOrDefault();
        }

        public static T GetAncestor<T>(this SyntaxToken token)
            where T : SyntaxNode
        {
            return token.GetAncestors<T>().FirstOrDefault();
        }

        public static T GetAncestor<T>(this SyntaxToken token, Func<T, bool> predicate)
            where T : SyntaxNode
        {
            return token.GetAncestors<T>().FirstOrDefault(predicate);
        }

        public static IEnumerable<T> GetAncestors<T>(this SyntaxToken token)
            where T : SyntaxNode
        {
            return token.Parent != null
                ? token.Parent.AncestorsAndSelf().OfType<T>()
                : SpecializedCollections.EmptyEnumerable<T>();
        }

        public static IEnumerable<SyntaxNode> GetAncestors(this SyntaxToken token, Func<SyntaxNode, bool> predicate)
        {
            return token.Parent != null
                ? token.Parent.AncestorsAndSelf().Where(predicate)
                : SpecializedCollections.EmptyEnumerable<SyntaxNode>();
        }

        public static SyntaxNode GetCommonRoot(this SyntaxToken token1, SyntaxToken token2)
        {
            Contract.ThrowIfTrue(token1.RawKind == 0 || token2.RawKind == 0);

            // find common starting node from two tokens.
            // as long as two tokens belong to same tree, there must be at least on common root (Ex, compilation unit)
            if (token1.Parent == null || token2.Parent == null)
            {
                return null;
            }

            return token1.Parent.GetCommonRoot(token2.Parent);
        }

        public static bool CheckParent<T>(this SyntaxToken token, Func<T, bool> valueChecker) where T : SyntaxNode
        {
            var parentNode = token.Parent as T;
            if (parentNode == null)
            {
                return false;
            }

            return valueChecker(parentNode);
        }

        public static int Width(this SyntaxToken token)
        {
            return token.Span.Length;
        }

        public static int FullWidth(this SyntaxToken token)
        {
            return token.FullSpan.Length;
        }

        public static SyntaxToken FindTokenFromEnd(this SyntaxNode root, int position, bool includeZeroWidth = true, bool findInsideTrivia = false)
        {
            var token = root.FindToken(position, findInsideTrivia);
            var previousToken = token.GetPreviousToken(
                includeZeroWidth, findInsideTrivia, findInsideTrivia, findInsideTrivia);

            if (token.SpanStart == position &&
                previousToken.RawKind != 0 &&
                previousToken.Span.End == position)
            {
                return previousToken;
            }

            return token;
        }
    }
}
