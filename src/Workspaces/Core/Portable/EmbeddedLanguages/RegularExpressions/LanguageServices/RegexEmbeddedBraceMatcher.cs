// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices
{
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
                case '(': case ')':
                case '[': case ']':
                    break;
                default:
                    return null;
            }

            return FindBraceHighlights(tree, ch);
        }

        private static EmbeddedBraceMatchingResult? FindBraceHighlights(RegexTree tree, VirtualChar ch)
        {
            return FindGroupingBraces(tree, ch) ??
                   // FindCommentBraces(tree, ch) ??
                   FindCharacterClassBraces(tree, ch);
        }

        private static EmbeddedBraceMatchingResult? FindGroupingBraces(RegexTree tree, VirtualChar ch)
        {
            var node = FindGroupingNode(tree.Root, ch);
            if (node == null)
            {
                return null;
            }

            if (node.OpenParenToken.IsMissing || node.CloseParenToken.IsMissing)
            {
                return null;
            }

            return new EmbeddedBraceMatchingResult(
                node.OpenParenToken.VirtualChars[0].Span,
                node.CloseParenToken.VirtualChars[0].Span);
        }

        private static EmbeddedBraceMatchingResult? FindCharacterClassBraces(RegexTree tree, VirtualChar ch)
        {
            var node = FindCharacterClassNode(tree.Root, ch);
            if (node == null)
            {
                return null;
            }

            if (node.OpenBracketToken.IsMissing || node.CloseBracketToken.IsMissing)
            {
                return null;
            }

            return new EmbeddedBraceMatchingResult(
                node.OpenBracketToken.VirtualChars[0].Span,
                node.CloseBracketToken.VirtualChars[0].Span);
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
    }
}
