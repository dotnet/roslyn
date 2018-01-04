// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.RegularExpressions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BraceMatching
{
    internal class AbstractRegexBraceMatcher : IBraceMatcher
    {
        private readonly int _stringLiteralKind;
        private readonly ISyntaxFactsService _syntaxFacts;
        private readonly ISemanticFactsService _semanticFacts;
        private readonly IVirtualCharService _virtualCharService;

        protected AbstractRegexBraceMatcher(
            int stringLiteralKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
        {
            _stringLiteralKind = stringLiteralKind;
            _syntaxFacts = syntaxFacts;
            _semanticFacts = semanticFacts;
            _virtualCharService = virtualCharService;
        }

        public async Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, CancellationToken cancellationToken = default)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            if (token.RawKind == _stringLiteralKind &&
                !RegexPatternDetector.IsDefinitelyNotPattern(token, _syntaxFacts))
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var detector = RegexPatternDetector.TryGetOrCreate(semanticModel, _syntaxFacts, _semanticFacts);
                var tree = detector?.TryParseRegexPattern(token, _virtualCharService, cancellationToken);
                if (tree != null)
                {
                    return FindBraces(tree, position);
                }
            }

            return null;
        }

        private BraceMatchingResult? FindBraces(RegexTree tree, int position)
        {
            var virtualChar = tree.Text.FirstOrNullable(vc => vc.Span.Contains(position));
            if (virtualChar == null)
            {
                return null;
            }

            var ch = virtualChar.Value;
            return FindReferenceMatch(tree, ch) ?? FindBraceMatch(tree, ch);
        }

        private BraceMatchingResult? FindBraceMatch(RegexTree tree, VirtualChar ch)
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

        private RegexGroupingNode FindGroupingNode(RegexNode node, VirtualChar ch)
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

        private BraceMatchingResult? FindReferenceMatch(RegexTree tree, VirtualChar ch)
        {
            var node = FindReferenceNode(tree.Root, ch);
            if (node == null)
            {
                return null;
            }

            var captureToken = GetCaptureToken(node);
            if (captureToken.Kind == RegexKind.NumberToken)
            {
                var val = (int)captureToken.Value;
                if (tree.CaptureNumbersToSpan.TryGetValue(val, out var captureSpan))
                {
                    return new BraceMatchingResult(RegexHelpers.GetSpan(node), captureSpan);
                }
            }
            else
            {
                var val = (string)captureToken.Value;
                if (tree.CaptureNamesToSpan.TryGetValue(val, out var captureSpan))
                {
                    return new BraceMatchingResult(RegexHelpers.GetSpan(node), captureSpan);
                }
            }

            return null;
        }

        private RegexToken GetCaptureToken(RegexEscapeNode node)
        {
            switch (node)
            {
                case RegexBackreferenceEscapeNode backReference:
                    return backReference.NumberToken;
                case RegexCaptureEscapeNode captureEscape:
                    return captureEscape.CaptureToken;
                case RegexKCaptureEscapeNode kCaptureEscape:
                    return kCaptureEscape.CaptureToken;
            }

            throw new InvalidOperationException();
        }

        private RegexEscapeNode FindReferenceNode(RegexNode node, VirtualChar virtualChar)
        {
            if (node.Kind == RegexKind.BackreferenceEscape ||
                node.Kind == RegexKind.CaptureEscape ||
                node.Kind == RegexKind.KCaptureEscape)
            {
                if (RegexHelpers.Contains(node, virtualChar))
                {
                    return (RegexEscapeNode)node;
                }
            }

            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    var result = FindReferenceNode(child.Node, virtualChar);
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
