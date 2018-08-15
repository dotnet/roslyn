// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            var tree = await _language.TryGetTreeAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (tree == null)
            {
                return default;
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
            if (ch != '(' && ch != ')')
            {
                return null;
            }

            return FindBraceHighlights(tree, ch);
        }

        private static EmbeddedBraceMatchingResult? FindBraceHighlights(RegexTree tree, VirtualChar ch)
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
