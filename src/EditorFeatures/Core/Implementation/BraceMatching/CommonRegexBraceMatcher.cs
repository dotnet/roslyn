// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.RegularExpressions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.VirtualChars;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching
{
    internal static class CommonRegexBraceMatcher
    {
        internal static async Task<BraceMatchingResult?> FindBracesAsync(
            Document document, SyntaxToken token, int position, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (RegexPatternDetector.IsDefinitelyNotPattern(token, syntaxFacts))
            {
                return default;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var detector = RegexPatternDetector.TryGetOrCreate(semanticModel, syntaxFacts, document.GetLanguageService<ISemanticFactsService>());
            var tree = detector?.TryParseRegexPattern(token, document.GetLanguageService<IVirtualCharService>(), cancellationToken);

            if (tree == null)
            {
                return default;
            }

            return GetMatchingBraces(tree, position);
        }

        private static BraceMatchingResult? GetMatchingBraces(RegexTree tree, int position)
        {
            var virtualChar = tree.Text.FirstOrNullable(vc => vc.Span.Contains(position));
            if (virtualChar == null)
            {
                return null;
            }

            var ch = virtualChar.Value;
            if (ch != '(' && ch != ')')
            {
                return null;
            }

            return FindBraceHighlights(tree, ch);
        }

        private static BraceMatchingResult? FindBraceHighlights(RegexTree tree, VirtualChar ch)
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

            return new BraceMatchingResult(
                node.OpenParenToken.VirtualChars[0].Span,
                node.CloseParenToken.VirtualChars[0].Span);
        }

        private static RegexGroupingNode FindGroupingNode(RegexNode node, VirtualChar ch)
        {
            if (node is RegexGroupingNode grouping &&
                (grouping.OpenParenToken.VirtualChars.Contains(ch) || grouping.CloseParenToken.VirtualChars.Contains(ch)))
            {
                return grouping;
            }

            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    var result = FindGroupingNode(child.Node, ch);
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
