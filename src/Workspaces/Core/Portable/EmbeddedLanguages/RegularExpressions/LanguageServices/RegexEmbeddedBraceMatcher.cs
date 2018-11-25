﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices
{
    using System.Collections.Immutable;
    using RegexToken = EmbeddedSyntaxToken<RegexKind>;
    using RegexTrivia = EmbeddedSyntaxTrivia<RegexKind>;

    /// <summary>
    /// Brace matching impl for embedded regex strings.
    /// </summary>
    internal sealed class RegexEmbeddedBraceMatcher : IEmbeddedBraceMatcher
    {
        private readonly RegexEmbeddedLanguage _language;

        public RegexEmbeddedBraceMatcher(RegexEmbeddedLanguage language)
        {
            _language = language;
        }

        public async Task<EmbeddedBraceMatchingResult?> FindBracesAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var option = document.Project.Solution.Workspace.Options.GetOption(
                RegularExpressionsOptions.HighlightRelatedRegexComponentsUnderCursor, document.Project.Language);
            if (!option)
            {
                return null;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (RegexPatternDetector.IsDefinitelyNotPattern(token, syntaxFacts))
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var detector = RegexPatternDetector.TryGetOrCreate(semanticModel, _language);
            var tree = detector?.TryParseRegexPattern(token, cancellationToken);

            if (tree == null)
            {
                return null;
            }

            return GetMatchingBraces(tree, position);
        }

        private static EmbeddedBraceMatchingResult? GetMatchingBraces(RegexTree tree, int position)
        {
            var virtualChar = tree.Text.FirstOrNullable(vc => vc.Span.Contains(position));
            if (virtualChar == null)
            {
                return null;
            }

            var ch = virtualChar.Value;
            switch (ch)
            {
                case '(':
                case ')':
                    return FindGroupingBraces(tree, ch) ?? FindCommentBraces(tree, ch);
                case '[':
                case ']':
                    return FindCharacterClassBraces(tree, ch);
                default:
                    return null;
            }
        }

        private static EmbeddedBraceMatchingResult? CreateResult(RegexToken open, RegexToken close)
            => open.IsMissing || close.IsMissing
                ? default(EmbeddedBraceMatchingResult?)
                : new EmbeddedBraceMatchingResult(open.VirtualChars[0].Span, close.VirtualChars[0].Span);

        private static EmbeddedBraceMatchingResult? FindCommentBraces(RegexTree tree, VirtualChar ch)
        {
            var trivia = FindTrivia(tree.Root, ch);
            if (trivia?.Kind != RegexKind.CommentTrivia)
            {
                return null;
            }

            var firstChar = trivia.Value.VirtualChars[0];
            var lastChar = trivia.Value.VirtualChars[trivia.Value.VirtualChars.Length - 1];
            return firstChar != '(' || lastChar != ')'
                ? default(EmbeddedBraceMatchingResult?)
                : new EmbeddedBraceMatchingResult(firstChar.Span, lastChar.Span);
        }

        private static EmbeddedBraceMatchingResult? FindGroupingBraces(RegexTree tree, VirtualChar ch)
        {
            var node = FindGroupingNode(tree.Root, ch);
            return node == null ? null : CreateResult(node.OpenParenToken, node.CloseParenToken);
        }

        private static EmbeddedBraceMatchingResult? FindCharacterClassBraces(RegexTree tree, VirtualChar ch)
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
            {
                return nodeMatch;
            }

            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    var result = FindNode(child.Node, ch, predicate);
                    if (result != null)
                    {
                        return result;
                    }
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
                    {
                        return result;
                    }
                }
                else
                {
                    var token = child.Token;
                    var trivia = TryGetTrivia(token.LeadingTrivia, ch) ??
                                 TryGetTrivia(token.TrailingTrivia, ch);

                    if (trivia != null)
                    {
                        return trivia;
                    }
                }
            }

            return null;
        }

        private static RegexTrivia? TryGetTrivia(ImmutableArray<RegexTrivia> triviaList, VirtualChar ch)
        {
            foreach (var trivia in triviaList)
            {
                if (trivia.VirtualChars.Contains(ch))
                {
                    return trivia;
                }
            }

            return null;
        }
    }
}
