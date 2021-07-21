// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Analyzer.Utilities.Extensions
{
    internal static class SyntaxNodeExtensions
    {
        /// <summary>
        /// Look inside a trivia list for a skipped token that contains the given position.
        /// </summary>
        private static readonly Func<SyntaxTriviaList, int, SyntaxToken> s_findSkippedTokenForward = FindSkippedTokenForward;

        /// <summary>
        /// Look inside a trivia list for a skipped token that contains the given position.
        /// </summary>
        private static readonly Func<SyntaxTriviaList, int, SyntaxToken> s_findSkippedTokenBackward = FindSkippedTokenBackward;

        public static int Width(this SyntaxNode node)
            => node.Span.Length;

        public static int FullWidth(this SyntaxNode node)
            => node.FullSpan.Length;

        public static bool OverlapsHiddenPosition(this SyntaxNode node, CancellationToken cancellationToken)
            => node.OverlapsHiddenPosition(node.Span, cancellationToken);

        public static bool OverlapsHiddenPosition(this SyntaxNode node, TextSpan span, CancellationToken cancellationToken)
            => node.SyntaxTree.OverlapsHiddenPosition(span, cancellationToken);

        /// <summary>
        /// If the position is inside of token, return that token; otherwise, return the token to the right.
        /// </summary>
        public static SyntaxToken FindTokenOnRightOfPosition(
            this SyntaxNode root,
            int position,
            bool includeSkipped = false,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
        {
            var findSkippedToken = includeSkipped ? s_findSkippedTokenForward : ((l, p) => default);

            var token = GetInitialToken(root, position, includeSkipped, includeDirectives, includeDocumentationComments);

            if (position < token.SpanStart)
            {
                var skippedToken = findSkippedToken(token.LeadingTrivia, position);
                token = skippedToken.RawKind != 0 ? skippedToken : token;
            }
            else if (token.Span.End <= position)
            {
                do
                {
                    var skippedToken = findSkippedToken(token.TrailingTrivia, position);
                    token = skippedToken.RawKind != 0
                        ? skippedToken
                        : token.GetNextToken(includeZeroWidth: false, includeSkipped: includeSkipped, includeDirectives: includeDirectives, includeDocumentationComments: includeDocumentationComments);
                }
                while (token.RawKind != 0 && token.Span.End <= position && token.Span.End <= root.FullSpan.End);
            }

            if (token.Span.IsEmpty)
            {
                token = token.GetNextToken();
            }

            return token;
        }

        /// <summary>
        /// If the position is inside of token, return that token; otherwise, return the token to the left.
        /// </summary>
        public static SyntaxToken FindTokenOnLeftOfPosition(
            this SyntaxNode root,
            int position,
            bool includeSkipped = false,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
        {
            var findSkippedToken = includeSkipped ? s_findSkippedTokenBackward : ((l, p) => default);

            var token = GetInitialToken(root, position, includeSkipped, includeDirectives, includeDocumentationComments);

            if (position <= token.SpanStart)
            {
                do
                {
                    var skippedToken = findSkippedToken(token.LeadingTrivia, position);
                    token = skippedToken.RawKind != 0
                        ? skippedToken
                        : token.GetPreviousToken(includeZeroWidth: false, includeSkipped: includeSkipped, includeDirectives: includeDirectives, includeDocumentationComments: includeDocumentationComments);
                }
                while (position <= token.SpanStart && root.FullSpan.Start < token.SpanStart);
            }
            else if (token.Span.End < position)
            {
                var skippedToken = findSkippedToken(token.TrailingTrivia, position);
                token = skippedToken.RawKind != 0 ? skippedToken : token;
            }

            if (token.Span.IsEmpty)
            {
                token = token.GetPreviousToken();
            }

            return token;
        }

        private static SyntaxToken GetInitialToken(
            SyntaxNode root,
            int position,
            bool includeSkipped = false,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
        {
            return (position < root.FullSpan.End || !(root is ICompilationUnitSyntax))
                ? root.FindToken(position, includeSkipped || includeDirectives || includeDocumentationComments)
                : root.GetLastToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true)
                      .GetPreviousToken(includeZeroWidth: false, includeSkipped: includeSkipped, includeDirectives: includeDirectives, includeDocumentationComments: includeDocumentationComments);
        }

        /// <summary>
        /// Look inside a trivia list for a skipped token that contains the given position.
        /// </summary>
        private static SyntaxToken FindSkippedTokenForward(SyntaxTriviaList triviaList, int position)
        {
            foreach (var trivia in triviaList)
            {
                if (trivia.HasStructure &&
                    trivia.GetStructure() is ISkippedTokensTriviaSyntax skippedTokensTrivia)
                {
                    foreach (var token in skippedTokensTrivia.Tokens)
                    {
                        if (!token.Span.IsEmpty && position <= token.Span.End)
                        {
                            return token;
                        }
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Look inside a trivia list for a skipped token that contains the given position.
        /// </summary>
        private static SyntaxToken FindSkippedTokenBackward(SyntaxTriviaList triviaList, int position)
        {
            foreach (var trivia in triviaList.Reverse())
            {
                if (trivia.HasStructure &&
                    trivia.GetStructure() is ISkippedTokensTriviaSyntax skippedTokensTrivia)
                {
                    foreach (var token in skippedTokensTrivia.Tokens)
                    {
                        if (!token.Span.IsEmpty && token.SpanStart <= position)
                        {
                            return token;
                        }
                    }
                }
            }

            return default;
        }
    }
}
