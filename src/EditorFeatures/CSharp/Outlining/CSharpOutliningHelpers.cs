// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining
{
    internal static class CSharpOutliningHelpers
    {
        public const string Ellipsis = "...";
        public const string MultiLineCommentSuffix = "*/";
        public const int MaxXmlDocCommentBannerLength = 120;

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
                return default(SyntaxToken);
            }

            // If the next token is a semicolon, and we aren't in the initializer of a for-loop, use that token as the end.

            SyntaxToken nextToken = lastToken.GetNextToken(includeSkipped: true);
            if (nextToken.Kind() != SyntaxKind.None && nextToken.Kind() == SyntaxKind.SemicolonToken)
            {
                var forStatement = nextToken.GetAncestor<ForStatementSyntax>();
                if (forStatement != null && forStatement.FirstSemicolonToken == nextToken)
                {
                    return default(SyntaxToken);
                }

                lastToken = nextToken;
            }

            return lastToken;
        }

        private static string CreateCommentBannerTextWithPrefix(string text, string prefix)
        {
            Contract.ThrowIfNull(text);
            Contract.ThrowIfNull(prefix);

            int prefixLength = prefix.Length;
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
                int lineBreakStart = comment.ToString().IndexOfAny(new char[] { '\r', '\n' });

                var text = comment.ToString();
                if (lineBreakStart >= 0)
                {
                    text = text.Substring(0, lineBreakStart);
                }
                else
                {
                    text = text.EndsWith(MultiLineCommentSuffix) ? text.Substring(0, text.Length - MultiLineCommentSuffix.Length) : text;
                }

                return CreateCommentBannerTextWithPrefix(text, "/*");
            }
            else
            {
                return string.Empty;
            }
        }

        private static OutliningSpan CreateCommentRegion(SyntaxTrivia startComment, SyntaxTrivia endComment)
        {
            var span = TextSpan.FromBounds(startComment.SpanStart, endComment.Span.End);

            return new OutliningSpan(
                span,
                hintSpan: span,
                bannerText: GetCommentBannerText(startComment),
                autoCollapse: true);
        }

        // For testing purposes
        internal static IEnumerable<OutliningSpan> CreateCommentRegions(SyntaxTriviaList triviaList)
        {
            var result = new List<OutliningSpan>();
            CollectCommentRegions(triviaList, result);
            return result;
        }

        public static void CollectCommentRegions(SyntaxTriviaList triviaList, List<OutliningSpan> spans)
        {
            if (triviaList.Count > 0)
            {
                SyntaxTrivia? startComment = null;
                SyntaxTrivia? endComment = null;

                Action completeSingleLineCommentGroup = () =>
                {
                    if (startComment != null)
                    {
                        var singleLineCommentGroupRegion = CreateCommentRegion(startComment.Value, endComment.Value);
                        spans.Add(singleLineCommentGroupRegion);
                        startComment = null;
                        endComment = null;
                    }
                };

                // Iterate through trivia and collect the following:
                //    1. Groups of contiguous single-line comments that are only separated by whitespace
                //    2. Multi-line comments
                foreach (var trivia in triviaList)
                {
                    if (trivia.IsSingleLineComment())
                    {
                        startComment = startComment ?? trivia;
                        endComment = trivia;
                    }
                    else if (trivia.IsMultiLineComment())
                    {
                        completeSingleLineCommentGroup();

                        var multilineCommentRegion = CreateCommentRegion(trivia, trivia);
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

        public static void CollectCommentRegions(SyntaxNode node, List<OutliningSpan> spans)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var triviaList = node.GetLeadingTrivia();

            CollectCommentRegions(triviaList, spans);
        }

        private static OutliningSpan CreateRegion(TextSpan textSpan, string bannerText, bool autoCollapse)
        {
            return CreateRegion(textSpan, textSpan, bannerText, autoCollapse);
        }

        private static OutliningSpan CreateRegion(TextSpan textSpan, TextSpan hintSpan, string bannerText, bool autoCollapse)
        {
            return new OutliningSpan(textSpan, hintSpan, bannerText, autoCollapse);
        }

        public static OutliningSpan CreateRegion(SyntaxNode node, string bannerText, bool autoCollapse)
        {
            return CreateRegion(
                node.Span,
                bannerText,
                autoCollapse);
        }

        public static OutliningSpan CreateRegion(SyntaxNode node, SyntaxToken syntaxToken, string bannerText, bool autoCollapse)
        {
            return CreateRegion(
                node,
                syntaxToken,
                node.GetLastToken(),
                bannerText,
                autoCollapse);
        }

        public static OutliningSpan CreateRegion(SyntaxNode node, SyntaxToken startToken, int endPos, string bannerText, bool autoCollapse)
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
            var hintSpan = TextSpan.FromBounds(node.SpanStart, endPos);

            return CreateRegion(
                span,
                hintSpan,
                bannerText,
                autoCollapse);
        }

        public static OutliningSpan CreateRegion(SyntaxNode node, SyntaxToken startToken, SyntaxToken endToken, string bannerText, bool autoCollapse)
        {
            return CreateRegion(
                node,
                startToken,
                GetCollapsibleEnd(endToken),
                bannerText,
                autoCollapse);
        }

        public static OutliningSpan CreateRegion(SyntaxNode node, bool autoCollapse)
        {
            return CreateRegion(
                node,
                bannerText: Ellipsis,
                autoCollapse: autoCollapse);
        }

        // Adds everything after 'syntaxToken' up to and including the end 
        // of node as a region.  The snippet to display is just "..."
        public static OutliningSpan CreateRegion(SyntaxNode node, SyntaxToken syntaxToken, bool autoCollapse)
        {
            return CreateRegion(
                node, syntaxToken,
                bannerText: Ellipsis,
                autoCollapse: autoCollapse);
        }

        // Adds everything after 'syntaxToken' up to and including the end 
        // of node as a region.  The snippet to display is just "..."
        public static OutliningSpan CreateRegion(SyntaxNode node, SyntaxToken startToken, SyntaxToken endToken, bool autoCollapse)
        {
            return CreateRegion(
                node, startToken, endToken,
                bannerText: Ellipsis,
                autoCollapse: autoCollapse);
        }

        // Adds the span surrounding the syntax list as a region.  The
        // snippet shown is the text from the first line of the first 
        // node in the list.
        public static OutliningSpan CreateRegion(IEnumerable<SyntaxNode> syntaxList, bool autoCollapse)
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

            return CreateRegion(
                textSpan: TextSpan.FromBounds(spanStart, spanEnd),
                hintSpan: TextSpan.FromBounds(hintSpanStart, hintSpanEnd),
                bannerText: Ellipsis,
                autoCollapse: autoCollapse);
        }
    }
}
