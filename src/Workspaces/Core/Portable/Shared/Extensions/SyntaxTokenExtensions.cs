﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SyntaxTokenExtensions
    {
        public static SyntaxNode GetAncestor(this SyntaxToken token, Func<SyntaxNode, bool> predicate)
        {
            return token.GetAncestor<SyntaxNode>(predicate);
        }

        public static T GetAncestor<T>(this SyntaxToken token, Func<T, bool> predicate = null)
            where T : SyntaxNode
        {
            return token.Parent != null
                ? token.Parent.FirstAncestorOrSelf(predicate)
                : default;
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

        public static SyntaxToken GetNextTokenOrEndOfFile(
            this SyntaxToken token,
            bool includeZeroWidth = false,
            bool includeSkipped = false,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
        {
            var nextToken = token.GetNextToken(includeZeroWidth, includeSkipped, includeDirectives, includeDocumentationComments);

            return nextToken.RawKind == 0
                ? ((ICompilationUnitSyntax)token.Parent.SyntaxTree.GetRoot(CancellationToken.None)).EndOfFileToken
                : nextToken;
        }

        public static SyntaxToken WithoutTrivia(
            this SyntaxToken token)
        {
            if (!token.LeadingTrivia.Any() && !token.TrailingTrivia.Any())
            {
                return token;
            }

            return token.With(new SyntaxTriviaList(), new SyntaxTriviaList());
        }

        public static SyntaxToken With(this SyntaxToken token, SyntaxTriviaList leading, SyntaxTriviaList trailing)
        {
            return token.WithLeadingTrivia(leading).WithTrailingTrivia(trailing);
        }

        public static SyntaxToken WithPrependedLeadingTrivia(
            this SyntaxToken token,
            params SyntaxTrivia[] trivia)
        {
            if (trivia.Length == 0)
            {
                return token;
            }

            return token.WithPrependedLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public static SyntaxToken WithPrependedLeadingTrivia(
            this SyntaxToken token,
            SyntaxTriviaList trivia)
        {
            if (trivia.Count == 0)
            {
                return token;
            }

            return token.WithLeadingTrivia(trivia.Concat(token.LeadingTrivia));
        }

        public static SyntaxToken WithPrependedLeadingTrivia(
            this SyntaxToken token,
            IEnumerable<SyntaxTrivia> trivia)
        {
            var list = new SyntaxTriviaList();
            list = list.AddRange(trivia);

            return token.WithPrependedLeadingTrivia(list);
        }

        public static SyntaxToken WithAppendedTrailingTrivia(
            this SyntaxToken token,
            params SyntaxTrivia[] trivia)
        {
            return token.WithAppendedTrailingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public static SyntaxToken WithAppendedTrailingTrivia(
            this SyntaxToken token,
            IEnumerable<SyntaxTrivia> trivia)
        {
            return token.WithTrailingTrivia(token.TrailingTrivia.Concat(trivia));
        }
    }
}
