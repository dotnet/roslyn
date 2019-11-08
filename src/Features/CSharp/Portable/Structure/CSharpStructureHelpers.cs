// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal static class CSharpStructureHelpers
    {
        public const string Ellipsis = "...";
        public const string MultiLineCommentSuffix = "*/";
        public const int MaxXmlDocCommentBannerLength = 120;
        private static readonly char[] s_newLineCharacters = new char[] { '\r', '\n' };

        private static int GetCollapsibleStart(SyntaxToken firstToken)
        {
            // If the *next* token has any leading comments, we use the end of the last one.
            // If not, we check *this* token to see if it has any trailing comments and use the last one;
            // otherwise, we use the end of this token.

            var start = firstToken.Span.End;

            var nextToken = firstToken.GetNextToken();
            if (nextToken.Kind() != SyntaxKind.None && nextToken.HasLeadingTrivia)
            {
                var lastLeadingCommentTrivia = nextToken.LeadingTrivia.GetLastComment();
                if (lastLeadingCommentTrivia != null)
                {
                    start = lastLeadingCommentTrivia.Value.Span.End;
                }
            }

            if (firstToken.HasTrailingTrivia)
            {
                var lastTrailingCommentOrWhitespaceTrivia = firstToken.TrailingTrivia.GetLastCommentOrWhitespace();
                if (lastTrailingCommentOrWhitespaceTrivia != null)
                {
                    start = lastTrailingCommentOrWhitespaceTrivia.Value.Span.End;
                }
            }

            return start;
        }

        private static int GetCollapsibleEnd(SyntaxToken lastToken)
        {
            // If the token has any trailing comments, we use the end of the token;
            // otherwise, we skip to the start of the first new line trivia.

            var end = lastToken.Span.End;

            if (lastToken.HasTrailingTrivia &&
                !lastToken.TrailingTrivia.Any(SyntaxKind.SingleLineCommentTrivia, SyntaxKind.MultiLineCommentTrivia))
            {
                var firstNewLineTrivia = lastToken.TrailingTrivia.GetFirstNewLine();
                if (firstNewLineTrivia != null)
                {
                    end = firstNewLineTrivia.Value.SpanStart;
                }
            }

            return end;
        }

        public static SyntaxToken GetLastInlineMethodBlockToken(SyntaxNode node)
        {
            var lastToken = node.GetLastToken(includeZeroWidth: true);
            if (lastToken.Kind() == SyntaxKind.None)
            {
                return default;
            }

            // If the next token is a semicolon, and we aren't in the initializer of a for-loop, use that token as the end.

            var nextToken = lastToken.GetNextToken(includeSkipped: true);
            if (nextToken.Kind() != SyntaxKind.None && nextToken.Kind() == SyntaxKind.SemicolonToken)
            {
                var forStatement = nextToken.GetAncestor<ForStatementSyntax>();
                if (forStatement is { FirstSemicolonToken: nextToken })
                {
                    return default;
                }

                lastToken = nextToken;
            }

            return lastToken;
        }

        private static string CreateCommentBannerTextWithPrefix(string text, string prefix)
        {
            Contract.ThrowIfNull(text);
            Contract.ThrowIfNull(prefix);

            var prefixLength = prefix.Length;
            return prefix + " " + text.Substring(prefixLength).Trim() + " " + Ellipsis;
        }

        private static string GetCommentBannerText(SyntaxTrivia comment)
        {
            Contract.ThrowIfFalse(comment.IsSingleLineComment() || comment.IsMultiLineComment());

            if (comment.IsSingleLineComment())
            {
                return CreateCommentBannerTextWithPrefix(comment.ToString(), "//");
            }
            else if (comment.IsMultiLineComment())
            {
                var lineBreakStart = comment.ToString().IndexOfAny(s_newLineCharacters);

                var text = comment.ToString();
                if (lineBreakStart >= 0)
                {
                    text = text.Substring(0, lineBreakStart);
                }
                else
                {
                    text = text.Length >= "/**/".Length && text.EndsWith(MultiLineCommentSuffix)
                        ? text.Substring(0, text.Length - MultiLineCommentSuffix.Length)
                        : text;
                }

                return CreateCommentBannerTextWithPrefix(text, "/*");
            }
            else
            {
                return string.Empty;
            }
        }

        private static BlockSpan CreateCommentBlockSpan(
            SyntaxTrivia startComment, SyntaxTrivia endComment)
        {
            var span = TextSpan.FromBounds(startComment.SpanStart, endComment.Span.End);

            return new BlockSpan(
                isCollapsible: true,
                textSpan: span,
                hintSpan: span,
                type: BlockTypes.Comment,
                bannerText: GetCommentBannerText(startComment),
                autoCollapse: true);
        }

        // For testing purposes
        internal static ImmutableArray<BlockSpan> CreateCommentBlockSpan(
            SyntaxTriviaList triviaList)
        {
            var result = ArrayBuilder<BlockSpan>.GetInstance();
            CollectCommentBlockSpans(triviaList, result);
            return result.ToImmutableAndFree();
        }

        public static void CollectCommentBlockSpans(
            SyntaxTriviaList triviaList, ArrayBuilder<BlockSpan> spans)
        {
            if (triviaList.Count > 0)
            {
                SyntaxTrivia? startComment = null;
                SyntaxTrivia? endComment = null;

                void completeSingleLineCommentGroup()
                {
                    if (startComment != null)
                    {
                        var singleLineCommentGroupRegion = CreateCommentBlockSpan(startComment.Value, endComment.Value);
                        spans.Add(singleLineCommentGroupRegion);
                        startComment = null;
                        endComment = null;
                    }
                }

                // Iterate through trivia and collect the following:
                //    1. Groups of contiguous single-line comments that are only separated by whitespace
                //    2. Multi-line comments
                foreach (var trivia in triviaList)
                {
                    if (trivia.IsSingleLineComment())
                    {
                        startComment ??= trivia;
                        endComment = trivia;
                    }
                    else if (trivia.IsMultiLineComment())
                    {
                        completeSingleLineCommentGroup();

                        var multilineCommentRegion = CreateCommentBlockSpan(trivia, trivia);
                        spans.Add(multilineCommentRegion);
                    }
                    else if (!trivia.MatchesKind(SyntaxKind.WhitespaceTrivia,
                                                 SyntaxKind.EndOfLineTrivia,
                                                 SyntaxKind.EndOfFileToken))
                    {
                        completeSingleLineCommentGroup();
                    }
                }

                completeSingleLineCommentGroup();
            }
        }

        public static void CollectCommentBlockSpans(
            SyntaxNode node, ArrayBuilder<BlockSpan> spans)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var triviaList = node.GetLeadingTrivia();

            CollectCommentBlockSpans(triviaList, spans);
        }

        private static BlockSpan CreateBlockSpan(
            TextSpan textSpan, string bannerText, bool autoCollapse,
            string type, bool isCollapsible)
        {
            return CreateBlockSpan(
                textSpan, textSpan, bannerText, autoCollapse, type, isCollapsible);
        }

        private static BlockSpan CreateBlockSpan(
            TextSpan textSpan, TextSpan hintSpan,
            string bannerText, bool autoCollapse,
            string type, bool isCollapsible)
        {
            return new BlockSpan(
                textSpan: textSpan,
                hintSpan: hintSpan,
                bannerText: bannerText,
                autoCollapse: autoCollapse,
                type: type,
                isCollapsible: isCollapsible);
        }

        public static BlockSpan CreateBlockSpan(
            SyntaxNode node, string bannerText, bool autoCollapse,
            string type, bool isCollapsible)
        {
            return CreateBlockSpan(
                node.Span,
                bannerText,
                autoCollapse,
                type,
                isCollapsible);
        }

        public static BlockSpan? CreateBlockSpan(
            SyntaxNode node, SyntaxToken syntaxToken,
            string bannerText, bool autoCollapse,
            string type, bool isCollapsible)
        {
            return CreateBlockSpan(
                node, syntaxToken, node.GetLastToken(),
                bannerText, autoCollapse, type, isCollapsible);
        }

        public static BlockSpan? CreateBlockSpan(
            SyntaxNode node, SyntaxToken startToken,
            int endPos, string bannerText, bool autoCollapse,
            string type, bool isCollapsible)
        {
            // If the SyntaxToken is actually missing, don't attempt to create an outlining region.
            if (startToken.IsMissing)
            {
                return null;
            }

            // Since we creating a span for everything after syntaxToken to ensure
            // that it collapses properly. However, the hint span begins at the start
            // of the next token so indentation in the tooltip is accurate.

            var span = TextSpan.FromBounds(GetCollapsibleStart(startToken), endPos);
            var hintSpan = GetHintSpan(node, endPos);

            return CreateBlockSpan(
                span,
                hintSpan,
                bannerText,
                autoCollapse,
                type,
                isCollapsible);
        }

        private static TextSpan GetHintSpan(SyntaxNode node, int endPos)
        {
            // Don't include attributes in the BlockSpan for a node.  When the user
            // hovers over the indent-guide we don't want to show them the line with
            // the attributes, we want to show them the line with the start of the
            // actual structure.
            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.Kind() != SyntaxKind.AttributeList)
                {
                    return TextSpan.FromBounds(child.SpanStart, endPos);
                }
            }

            return TextSpan.FromBounds(node.SpanStart, endPos);
        }

        public static BlockSpan? CreateBlockSpan(
            SyntaxNode node, SyntaxToken startToken,
            SyntaxToken endToken, string bannerText, bool autoCollapse,
            string type, bool isCollapsible)
        {
            return CreateBlockSpan(
                node, startToken, GetCollapsibleEnd(endToken),
                bannerText, autoCollapse, type, isCollapsible);
        }

        public static BlockSpan CreateBlockSpan(
            SyntaxNode node, bool autoCollapse, string type, bool isCollapsible)
        {
            return CreateBlockSpan(
                node,
                bannerText: Ellipsis,
                autoCollapse: autoCollapse,
                type: type,
                isCollapsible: isCollapsible);
        }

        // Adds everything after 'syntaxToken' up to and including the end 
        // of node as a region.  The snippet to display is just "..."
        public static BlockSpan? CreateBlockSpan(
            SyntaxNode node, SyntaxToken syntaxToken,
            bool autoCollapse, string type, bool isCollapsible)
        {
            return CreateBlockSpan(
                node, syntaxToken,
                bannerText: Ellipsis,
                autoCollapse: autoCollapse,
                type: type,
                isCollapsible: isCollapsible);
        }

        // Adds everything after 'syntaxToken' up to and including the end 
        // of node as a region.  The snippet to display is just "..."
        public static BlockSpan? CreateBlockSpan(
            SyntaxNode node, SyntaxToken startToken, SyntaxToken endToken,
            bool autoCollapse, string type, bool isCollapsible)
        {
            return CreateBlockSpan(
                node, startToken, endToken,
                bannerText: Ellipsis,
                autoCollapse: autoCollapse,
                type: type,
                isCollapsible: isCollapsible);
        }

        // Adds the span surrounding the syntax list as a region.  The
        // snippet shown is the text from the first line of the first 
        // node in the list.
        public static BlockSpan? CreateBlockSpan(
            IEnumerable<SyntaxNode> syntaxList, bool autoCollapse,
            string type, bool isCollapsible)
        {
            if (syntaxList.IsEmpty())
            {
                return null;
            }

            var end = GetCollapsibleEnd(syntaxList.Last().GetLastToken());

            var spanStart = syntaxList.First().GetFirstToken().FullSpan.End;
            var spanEnd = end >= spanStart
                ? end
                : spanStart;

            var hintSpanStart = syntaxList.First().SpanStart;
            var hintSpanEnd = end >= hintSpanStart
                ? end
                : hintSpanStart;

            return CreateBlockSpan(
                textSpan: TextSpan.FromBounds(spanStart, spanEnd),
                hintSpan: TextSpan.FromBounds(hintSpanStart, hintSpanEnd),
                bannerText: Ellipsis,
                autoCollapse: autoCollapse,
                type: type,
                isCollapsible: isCollapsible);
        }
    }
}
