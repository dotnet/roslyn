// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class FindTokenHelper
    {
        /// <summary>
        /// If the position is inside of token, return that token; otherwise, return the token to the right.
        /// </summary>
        public static SyntaxToken FindTokenOnRightOfPosition<TRoot>(
            SyntaxNode root,
            int position,
            Func<SyntaxTriviaList, int, SyntaxToken> skippedTokenFinder,
            bool includeSkipped = false,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
            where TRoot : SyntaxNode
        {
            var findSkippedToken = skippedTokenFinder ?? ((l, p) => default(SyntaxToken));

            var token = GetInitialToken<TRoot>(root, position, includeSkipped, includeDirectives, includeDocumentationComments);

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

            if (token.Span.Length == 0)
            {
                token = token.GetNextToken();
            }

            return token;
        }

        /// <summary>
        /// If the position is inside of token, return that token; otherwise, return the token to the left.
        /// </summary>
        public static SyntaxToken FindTokenOnLeftOfPosition<TRoot>(
            SyntaxNode root,
            int position,
            Func<SyntaxTriviaList, int, SyntaxToken> skippedTokenFinder,
            bool includeSkipped = false,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
            where TRoot : SyntaxNode
        {
            var findSkippedToken = skippedTokenFinder ?? ((l, p) => default(SyntaxToken));

            var token = GetInitialToken<TRoot>(root, position, includeSkipped, includeDirectives, includeDocumentationComments);

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

            if (token.Span.Length == 0)
            {
                token = token.GetPreviousToken();
            }

            return token;
        }

        private static SyntaxToken GetInitialToken<TRoot>(
            SyntaxNode root,
            int position,
            bool includeSkipped = false,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
            where TRoot : SyntaxNode
        {
            var token = (position < root.FullSpan.End || !(root is TRoot))
                ? root.FindToken(position, includeSkipped || includeDirectives || includeDocumentationComments)
                : root.GetLastToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true)
                      .GetPreviousToken(includeZeroWidth: false, includeSkipped: includeSkipped, includeDirectives: includeDirectives, includeDocumentationComments: includeDocumentationComments);
            return token;
        }

        /// <summary>
        /// Look inside a trivia list for a skipped token that contains the given position.
        /// </summary>
        public static SyntaxToken FindSkippedTokenBackward(IEnumerable<SyntaxToken> skippedTokenList, int position)
        {
            // the given skipped token list is already in order
            // PERF: Expansion of return skippedTokenList.LastOrDefault(skipped => skipped.Span.Length > 0 && skipped.SpanStart <= position);
            var skippedTokenContainingPosition = default(SyntaxToken);
            foreach (var skipped in skippedTokenList)
            {
                if (skipped.Span.Length > 0 && skipped.SpanStart <= position)
                {
                    skippedTokenContainingPosition = skipped;
                }
            }

            return skippedTokenContainingPosition;
        }

        /// <summary>
        /// Look inside a trivia list for a skipped token that contains the given position.
        /// </summary>
        public static SyntaxToken FindSkippedTokenForward(IEnumerable<SyntaxToken> skippedTokenList, int position)
        {
            // the given token list is already in order
            var skippedTokenContainingPosition = skippedTokenList.FirstOrDefault(skipped => skipped.Span.Length > 0 && position <= skipped.Span.End);
            if (skippedTokenContainingPosition != default(SyntaxToken))
            {
                return skippedTokenContainingPosition;
            }

            return default(SyntaxToken);
        }
    }
}
