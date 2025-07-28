// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class SyntaxTokenExtensions
{
    extension(SyntaxToken token)
    {
        public SyntaxNode? GetAncestor(Func<SyntaxNode, bool>? predicate)
        => token.GetAncestor<SyntaxNode>(predicate);

        public T? GetAncestor<T>(Func<T, bool>? predicate = null) where T : SyntaxNode
            => token.Parent?.FirstAncestorOrSelf(predicate);

        public T GetRequiredAncestor<T>(Func<T, bool>? predicate = null) where T : SyntaxNode
            => GetAncestor(token, predicate) ?? throw new InvalidOperationException("Could not find a valid ancestor");

        public IEnumerable<T> GetAncestors<T>()
            where T : SyntaxNode
        {
            return token.Parent != null
                ? token.Parent.AncestorsAndSelf().OfType<T>()
                : [];
        }

        public IEnumerable<SyntaxNode> GetAncestors(Func<SyntaxNode, bool> predicate)
        {
            return token.Parent != null
                ? token.Parent.AncestorsAndSelf().Where(predicate)
                : [];
        }

        public bool CheckParent<T>(Func<T, bool> valueChecker) where T : SyntaxNode
        {
            if (token.Parent is not T parentNode)
            {
                return false;
            }

            return valueChecker(parentNode);
        }

        public int Width()
            => token.Span.Length;

        public int FullWidth()
            => token.FullSpan.Length;

        public SyntaxToken GetNextTokenOrEndOfFile(
            bool includeZeroWidth = false,
            bool includeSkipped = false,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
        {
            var nextToken = token.GetNextToken(includeZeroWidth, includeSkipped, includeDirectives, includeDocumentationComments);

            return nextToken.RawKind == 0
                ? ((ICompilationUnitSyntax)token.Parent!.SyntaxTree!.GetRoot(CancellationToken.None)).EndOfFileToken
                : nextToken;
        }

        public SyntaxToken WithoutTrivia(
    )
        {
            if (!token.LeadingTrivia.Any() && !token.TrailingTrivia.Any())
            {
                return token;
            }

            return token.With([], []);
        }

        public SyntaxToken With(SyntaxTriviaList leading, SyntaxTriviaList trailing)
            => token.WithLeadingTrivia(leading).WithTrailingTrivia(trailing);

        public SyntaxToken WithPrependedLeadingTrivia(
            params SyntaxTrivia[] trivia)
        {
            if (trivia.Length == 0)
            {
                return token;
            }

            return token.WithPrependedLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public SyntaxToken WithPrependedLeadingTrivia(
            SyntaxTriviaList trivia)
        {
            if (trivia.Count == 0)
            {
                return token;
            }

            return token.WithLeadingTrivia(trivia.Concat(token.LeadingTrivia));
        }

        public SyntaxToken WithPrependedLeadingTrivia(
            IEnumerable<SyntaxTrivia> trivia)
        {
            var list = new SyntaxTriviaList();
            list = list.AddRange(trivia);

            return token.WithPrependedLeadingTrivia(list);
        }

        public SyntaxToken WithAppendedTrailingTrivia(
            params SyntaxTrivia[] trivia)
        {
            return token.WithAppendedTrailingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public SyntaxToken WithAppendedTrailingTrivia(
            IEnumerable<SyntaxTrivia> trivia)
        {
            return token.WithTrailingTrivia(token.TrailingTrivia.Concat(trivia));
        }

        public SyntaxNode GetRequiredParent()
            => token.Parent ?? throw new InvalidOperationException("Token's parent was null");
    }

    extension(SyntaxToken token1)
    {
        public SyntaxNode GetCommonRoot(SyntaxToken token2)
        => token1.GetRequiredParent().GetCommonRoot(token2.GetRequiredParent());
    }

    extension(SyntaxNode root)
    {
        public SyntaxToken FindTokenFromEnd(int position, bool includeZeroWidth = true, bool findInsideTrivia = false)
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

    extension(IEnumerable<SyntaxToken> tokens)
    {
        public SyntaxTrivia[] GetTrivia()
        => [.. tokens.SelectMany(token => SyntaxNodeOrTokenExtensions.GetTrivia(token))];
    }
}
