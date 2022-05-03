// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices;

namespace Microsoft.CodeAnalysis.Editor.EmbeddedLanguages.RegularExpressions
{
    using RegexToken = EmbeddedSyntaxToken<RegexKind>;
    using RegexTrivia = EmbeddedSyntaxTrivia<RegexKind>;

    /// <summary>
    /// Brace matching impl for embedded regex strings.
    /// </summary>
    internal sealed class RegexBraceMatcher : IBraceMatcher
    {
        private readonly RegexEmbeddedLanguage _language;

        public RegexBraceMatcher(RegexEmbeddedLanguage language)
            => _language = language;

        public async Task<BraceMatchingResult?> FindBracesAsync(
            Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken)
        {
            if (!options.HighlightingOptions.HighlightRelatedRegexComponentsUnderCursor)
                return null;

            var tree = await _language.TryGetTreeAtPositionAsync(document, position, cancellationToken).ConfigureAwait(false);
            return tree == null ? null : GetMatchingBraces(tree, position);
        }

        private static BraceMatchingResult? GetMatchingBraces(RegexTree tree, int position)
        {
            var virtualChar = tree.Text.Find(position);
            if (virtualChar == null)
                return null;

            var ch = virtualChar.Value;
            return ch.Value switch
            {
                '(' or ')' => FindGroupingBraces(tree, ch) ?? FindCommentBraces(tree, ch),
                '[' or ']' => FindCharacterClassBraces(tree, ch),
                _ => null,
            };
        }

        private static BraceMatchingResult? CreateResult(RegexToken open, RegexToken close)
            => open.IsMissing || close.IsMissing
                ? null
                : new BraceMatchingResult(open.VirtualChars[0].Span, close.VirtualChars[0].Span);

        private static BraceMatchingResult? FindCommentBraces(RegexTree tree, VirtualChar ch)
        {
            var trivia = FindTrivia(tree.Root, ch);
            if (trivia?.Kind != RegexKind.CommentTrivia)
                return null;

            var firstChar = trivia.Value.VirtualChars[0];
            var lastChar = trivia.Value.VirtualChars[trivia.Value.VirtualChars.Length - 1];
            return firstChar != '(' || lastChar != ')'
                ? null
                : new BraceMatchingResult(firstChar.Span, lastChar.Span);
        }

        private static BraceMatchingResult? FindGroupingBraces(RegexTree tree, VirtualChar ch)
        {
            var node = FindGroupingNode(tree.Root, ch);
            return node == null ? null : CreateResult(node.OpenParenToken, node.CloseParenToken);
        }

        private static BraceMatchingResult? FindCharacterClassBraces(RegexTree tree, VirtualChar ch)
        {
            var node = FindCharacterClassNode(tree.Root, ch);
            return node == null ? null : CreateResult(node.OpenBracketToken, node.CloseBracketToken);
        }

        private static RegexGroupingNode FindGroupingNode(RegexNode node, VirtualChar ch)
            => FindNode<RegexGroupingNode>(node, ch, (grouping, c) =>
                    grouping.OpenParenToken.VirtualChars.Contains(c) || grouping.CloseParenToken.VirtualChars.Contains(c));

        private static RegexBaseCharacterClassNode FindCharacterClassNode(RegexNode node, VirtualChar ch)
            => FindNode<RegexBaseCharacterClassNode>(node, ch, (grouping, c) =>
                    grouping.OpenBracketToken.VirtualChars.Contains(c) || grouping.CloseBracketToken.VirtualChars.Contains(c));

        private static TNode FindNode<TNode>(RegexNode node, VirtualChar ch, Func<TNode, VirtualChar, bool> predicate)
            where TNode : RegexNode
        {
            if (node is TNode nodeMatch && predicate(nodeMatch, ch))
                return nodeMatch;

            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    var result = FindNode(child.Node, ch, predicate);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        private static RegexTrivia? FindTrivia(RegexNode node, VirtualChar ch)
        {
            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    var result = FindTrivia(child.Node, ch);
                    if (result != null)
                        return result;
                }
                else
                {
                    var token = child.Token;
                    var trivia = TryGetTrivia(token.LeadingTrivia, ch) ??
                                 TryGetTrivia(token.TrailingTrivia, ch);

                    if (trivia != null)
                        return trivia;
                }
            }

            return null;
        }

        private static RegexTrivia? TryGetTrivia(ImmutableArray<RegexTrivia> triviaList, VirtualChar ch)
        {
            foreach (var trivia in triviaList)
            {
                if (trivia.VirtualChars.Contains(ch))
                    return trivia;
            }

            return null;
        }
    }
}
