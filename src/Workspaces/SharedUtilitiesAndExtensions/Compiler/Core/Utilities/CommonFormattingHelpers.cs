// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal static class CommonFormattingHelpers
{
    public static readonly Comparison<SuppressOperation> SuppressOperationComparer = (o1, o2) =>
    {
        return o1.TextSpan.Start - o2.TextSpan.Start;
    };

    public static readonly Comparison<IndentBlockOperation> IndentBlockOperationComparer = (o1, o2) =>
    {
        // smaller one goes left
        var d = o1.TextSpan.Start - o2.TextSpan.Start;
        if (d != 0)
            return d;

        // bigger one goes left
        d = o2.TextSpan.End - o1.TextSpan.End;
        if (d != 0)
            return d;

        if (o1.IsRelativeIndentation && o2.IsRelativeIndentation)
        {
            // if they're at the same location, but indenting separate amounts, have the bigger delta go first.
            d = o2.IndentationDeltaOrPosition - o1.IndentationDeltaOrPosition;
            if (d != 0)
                return d;
        }

        return 0;
    };

    public static IEnumerable<(SyntaxToken, SyntaxToken)> ConvertToTokenPairs(this SyntaxNode root, IReadOnlyList<TextSpan> spans)
    {
        Contract.ThrowIfNull(root);
        Contract.ThrowIfFalse(spans.Count > 0);

        if (spans.Count == 1)
        {
            // special case, if there is only one span, return right away
            yield return root.ConvertToTokenPair(spans[0]);
            yield break;
        }

        var previousOne = root.ConvertToTokenPair(spans[0]);

        // iterate through each spans and make sure each one doesn't overlap each other
        for (var i = 1; i < spans.Count; i++)
        {
            var currentOne = root.ConvertToTokenPair(spans[i]);
            if (currentOne.Item1.SpanStart <= previousOne.Item2.Span.End)
            {
                // oops, looks like two spans are overlapping each other. merge them
                previousOne = ValueTuple.Create(previousOne.Item1, previousOne.Item2.Span.End < currentOne.Item2.Span.End ? currentOne.Item2 : previousOne.Item2);
                continue;
            }

            // okay, looks like things are in good shape
            yield return previousOne;

            // move to next one
            previousOne = currentOne;
        }

        // give out the last one
        yield return previousOne;
    }

    public static ValueTuple<SyntaxToken, SyntaxToken> ConvertToTokenPair(this SyntaxNode root, TextSpan textSpan)
    {
        Contract.ThrowIfNull(root);
        Contract.ThrowIfTrue(textSpan.IsEmpty);

        var startToken = root.FindToken(textSpan.Start);

        // empty token, get previous non-zero length token
        if (startToken.IsMissing)
        {
            // if there is no previous token, startToken will be set to SyntaxKind.None
            startToken = startToken.GetPreviousToken();
        }

        // span is on leading trivia
        if (textSpan.Start < startToken.SpanStart)
        {
            // if there is no previous token, startToken will be set to SyntaxKind.None
            startToken = startToken.GetPreviousToken();
        }

        // adjust position where we try to search end token
        var endToken = (root.FullSpan.End <= textSpan.End) ?
            root.GetLastToken(includeZeroWidth: true) : root.FindToken(textSpan.End);

        // empty token, get next token
        if (endToken.IsMissing)
        {
            endToken = endToken.GetNextToken();
        }

        // span is on trailing trivia
        if (endToken.Span.End < textSpan.End)
        {
            endToken = endToken.GetNextToken();
        }

        // make sure tokens are not SyntaxKind.None
        startToken = (startToken.RawKind != 0) ? startToken : root.GetFirstToken(includeZeroWidth: true);
        endToken = (endToken.RawKind != 0) ? endToken : root.GetLastToken(includeZeroWidth: true);

        // token is in right order
        Contract.ThrowIfFalse(startToken.Equals(endToken) || startToken.Span.End <= endToken.SpanStart);
        return ValueTuple.Create(startToken, endToken);
    }

    public static bool IsInvalidTokenRange(this SyntaxNode root, SyntaxToken startToken, SyntaxToken endToken)
    {
        // given token must be token exist excluding EndOfFile token.
        if (startToken.RawKind == 0 || endToken.RawKind == 0)
        {
            return true;
        }

        if (startToken.Equals(endToken))
        {
            return false;
        }

        // regular case. 
        // start token can't be end of file token and start token must be before end token if it's not the same token.
        return root.FullSpan.End == startToken.SpanStart || startToken.FullSpan.End > endToken.FullSpan.Start;
    }

    public static int GetTokenColumn(this SyntaxTree tree, SyntaxToken token, int tabSize)
    {
        Contract.ThrowIfNull(tree);
        Contract.ThrowIfTrue(token.RawKind == 0);

        var startPosition = token.SpanStart;
        var line = tree.GetText().Lines.GetLineFromPosition(startPosition);

        return line.GetColumnFromLineOffset(startPosition - line.Start, tabSize);
    }

    public static string GetText(this SourceText text, SyntaxToken token1, SyntaxToken token2)
        => (token1.RawKind == 0) ? text.ToString(TextSpan.FromBounds(0, token2.SpanStart)) : text.ToString(TextSpan.FromBounds(token1.Span.End, token2.SpanStart));

    public static string GetTextBetween(SyntaxToken token1, SyntaxToken token2)
    {
        var builder = new StringBuilder();
        AppendTextBetween(token1, token2, builder);

        return builder.ToString();
    }

    public static void AppendTextBetween(SyntaxToken token1, SyntaxToken token2, StringBuilder builder)
    {
        Contract.ThrowIfTrue(token1.RawKind == 0 && token2.RawKind == 0);
        Contract.ThrowIfTrue(token1.Equals(token2));

        if (token1.RawKind == 0)
        {
            AppendLeadingTriviaText(token2, builder);
            return;
        }

        if (token2.RawKind == 0)
        {
            AppendTrailingTriviaText(token1, builder);
            return;
        }

        if (token1.FullSpan.End == token2.FullSpan.Start)
        {
            AppendTextBetweenTwoAdjacentTokens(token1, token2, builder);
            return;
        }

        AppendTrailingTriviaText(token1, builder);

        for (var token = token1.GetNextToken(includeZeroWidth: true); token.FullSpan.End <= token2.FullSpan.Start; token = token.GetNextToken(includeZeroWidth: true))
        {
            builder.Append(token.ToFullString());
        }

        AppendPartialLeadingTriviaText(token2, builder, token1.TrailingTrivia.FullSpan.End);
    }

    private static void AppendTextBetweenTwoAdjacentTokens(SyntaxToken token1, SyntaxToken token2, StringBuilder builder)
    {
        AppendTrailingTriviaText(token1, builder);
        AppendLeadingTriviaText(token2, builder);
    }

    private static void AppendLeadingTriviaText(SyntaxToken token, StringBuilder builder)
    {
        if (!token.HasLeadingTrivia)
        {
            return;
        }

        foreach (var trivia in token.LeadingTrivia)
        {
            builder.Append(trivia.ToFullString());
        }
    }

    /// <summary>
    /// If the token1 is expected to be part of the leading trivia of the token2 then the trivia
    /// before the token1FullSpanEnd, which the fullspan end of the token1 should be ignored
    /// </summary>
    private static void AppendPartialLeadingTriviaText(SyntaxToken token, StringBuilder builder, int token1FullSpanEnd)
    {
        if (!token.HasLeadingTrivia)
        {
            return;
        }

        foreach (var trivia in token.LeadingTrivia)
        {
            if (trivia.FullSpan.End <= token1FullSpanEnd)
            {
                continue;
            }

            builder.Append(trivia.ToFullString());
        }
    }

    private static void AppendTrailingTriviaText(SyntaxToken token, StringBuilder builder)
    {
        if (!token.HasTrailingTrivia)
        {
            return;
        }

        foreach (var trivia in token.TrailingTrivia)
        {
            builder.Append(trivia.ToFullString());
        }
    }

    /// <summary>
    /// this will create a span that includes its trailing trivia of its previous token and leading trivia of its next token
    /// for example, for code such as "class A { int ...", if given tokens are "A" and "{", this will return span [] of "class[ A { ]int ..."
    /// which included trailing trivia of "class" which is previous token of "A", and leading trivia of "int" which is next token of "{"
    /// </summary>
    public static TextSpan GetSpanIncludingTrailingAndLeadingTriviaOfAdjacentTokens(SyntaxToken startToken, SyntaxToken endToken)
    {
        // most of cases we can just ask previous and next token to create the span, but in some corner cases such as omitted token case,
        // those navigation function doesn't work, so we have to explore the tree ourselves to create correct span
        var startPosition = GetStartPositionOfSpan(startToken);
        var endPosition = GetEndPositionOfSpan(endToken);

        return TextSpan.FromBounds(startPosition, endPosition);
    }

    private static int GetEndPositionOfSpan(SyntaxToken token)
    {
        var nextToken = token.GetNextToken();
        if (nextToken.RawKind != 0)
        {
            return nextToken.SpanStart;
        }

        var backwardPosition = token.FullSpan.End;
        var parentNode = GetParentThatContainsGivenSpan(token.Parent, backwardPosition, forward: false);
        if (parentNode == null)
        {
            // reached the end of tree
            return token.FullSpan.End;
        }

        Contract.ThrowIfFalse(backwardPosition < parentNode.FullSpan.End);

        nextToken = parentNode.FindToken(backwardPosition + 1);

        Contract.ThrowIfTrue(nextToken.RawKind == 0);

        return nextToken.SpanStart;
    }

    public static int GetStartPositionOfSpan(SyntaxToken token)
    {
        var previousToken = token.GetPreviousToken();
        if (previousToken.RawKind != 0)
        {
            return previousToken.Span.End;
        }

        // first token in the tree
        var forwardPosition = token.FullSpan.Start;
        if (forwardPosition <= 0)
        {
            return 0;
        }

        var parentNode = GetParentThatContainsGivenSpan(token.Parent, forwardPosition, forward: true);
        Contract.ThrowIfNull(parentNode);
        Contract.ThrowIfFalse(parentNode.FullSpan.Start < forwardPosition);

        previousToken = parentNode.FindToken(forwardPosition + 1);

        Contract.ThrowIfTrue(previousToken.RawKind == 0);

        return previousToken.Span.End;
    }

    private static SyntaxNode? GetParentThatContainsGivenSpan(SyntaxNode? node, int position, bool forward)
    {
        while (node != null)
        {
            var fullSpan = node.FullSpan;
            if (forward)
            {
                if (fullSpan.Start < position)
                {
                    return node;
                }
            }
            else
            {
                if (position > fullSpan.End)
                {
                    return node;
                }
            }

            node = node.Parent;
        }

        return null;
    }

    public static bool HasAnyWhitespaceElasticTrivia(SyntaxToken previousToken, SyntaxToken currentToken)
    {
        if (!previousToken.ContainsAnnotations && !currentToken.ContainsAnnotations)
            return false;

        if (!previousToken.HasTrailingTrivia && !currentToken.HasLeadingTrivia)
            return false;

        return previousToken.TrailingTrivia.HasAnyWhitespaceElasticTrivia() || currentToken.LeadingTrivia.HasAnyWhitespaceElasticTrivia();
    }

    public static TextSpan GetFormattingSpan(SyntaxNode root, TextSpan span)
    {
        Contract.ThrowIfNull(root);

        var startToken = root.FindToken(span.Start).GetPreviousToken();
        var endToken = root.FindTokenFromEnd(span.End).GetNextToken();

        var startPosition = startToken.SpanStart;
        var endPosition = endToken.RawKind == 0 ? root.Span.End : endToken.Span.End;

        return TextSpan.FromBounds(startPosition, endPosition);
    }
}
