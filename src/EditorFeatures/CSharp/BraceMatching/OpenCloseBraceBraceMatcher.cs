// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RegularExpressions;
using Microsoft.CodeAnalysis.RegularExpressions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.BraceMatching
{
    [ExportBraceMatcher(LanguageNames.CSharp)]
    internal class OpenCloseBraceBraceMatcher : AbstractCSharpBraceMatcher
    {
        public OpenCloseBraceBraceMatcher()
            : base(SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken)
        {
        }
    }

    [ExportBraceMatcher(LanguageNames.CSharp)]
    internal class RegexBraceMatcher : IBraceMatcher
    {
        public async Task<BraceMatchingResult?> FindBracesAsync(Document document, int position, CancellationToken cancellationToken = default)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            if (token.Kind() == SyntaxKind.StringLiteralToken &&
                !RegexPatternDetector.IsDefinitelyNotPattern(token, CSharpSyntaxFactsService.Instance))
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var detector = RegexPatternDetector.TryGetOrCreate(semanticModel, CSharpSyntaxFactsService.Instance, CSharpSemanticFactsService.Instance);
                var tree = detector?.TryParseRegexPattern(token, CSharpVirtualCharService.Instance, cancellationToken);
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
