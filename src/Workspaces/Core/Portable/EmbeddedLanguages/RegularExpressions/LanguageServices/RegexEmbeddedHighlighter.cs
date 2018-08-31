// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices
{
    using RegexToken = EmbeddedSyntaxToken<RegexKind>;

    internal sealed class RegexEmbeddedHighlighter : IEmbeddedHighlighter
    {
        private readonly RegexEmbeddedLanguage _language;

        public RegexEmbeddedHighlighter(RegexEmbeddedLanguage language)
        {
            _language = language;
        }

        public async Task<ImmutableArray<TextSpan>> GetHighlightsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var option = document.Project.Solution.Workspace.Options.GetOption(RegularExpressionsOptions.HighlightRelatedRegexComponentsUnderCursor, document.Project.Language);
            if (!option)
            {
                return default;
            }

            var tree = await _language.TryGetTreeAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (tree == null)
            {
                return default;
            }

            return GetHighlights(document, tree, position);
        }

        private ImmutableArray<TextSpan> GetHighlights(
            Document document, RegexTree tree, int positionInDocument)
        {
            var referencesOnTheRight = GetReferences(document, tree, positionInDocument, caretOnLeft: true);
            if (!referencesOnTheRight.IsEmpty)
            {
                return referencesOnTheRight;
            }

            if (positionInDocument == 0)
            {
                return default;
            }

            // Nothing was on the right of the caret.  Return anything we were able to find on 
            // the left of the caret.
            var referencesOnTheLeft = GetReferences(document, tree, positionInDocument - 1, caretOnLeft: false);
            return referencesOnTheLeft;
        }

        private ImmutableArray<TextSpan> GetReferences(
            Document document, RegexTree tree, int position, bool caretOnLeft)
        {
            var virtualChar = tree.Text.FirstOrNullable(vc => vc.Span.Contains(position));
            if (virtualChar == null)
            {
                return ImmutableArray<TextSpan>.Empty;
            }

            var ch = virtualChar.Value;
            return FindReferenceHighlights(tree, ch);
        }

        private ImmutableArray<TextSpan> FindReferenceHighlights(RegexTree tree, VirtualChar ch)
        {
            var node = FindReferenceNode(tree.Root, ch);
            if (node == null)
            {
                return ImmutableArray<TextSpan>.Empty;
            }

            var captureToken = GetCaptureToken(node);
            if (captureToken.Kind == RegexKind.NumberToken)
            {
                var val = (int)captureToken.Value;
                if (tree.CaptureNumbersToSpan.TryGetValue(val, out var captureSpan))
                {
                    return CreateHighlights(node, captureSpan);
                }
            }
            else
            {
                var val = (string)captureToken.Value;
                if (tree.CaptureNamesToSpan.TryGetValue(val, out var captureSpan))
                {
                    return CreateHighlights(node, captureSpan);
                }
            }

            return ImmutableArray<TextSpan>.Empty;
        }

        private ImmutableArray<TextSpan> CreateHighlights(
            RegexEscapeNode node, TextSpan captureSpan)
        {
            return ImmutableArray.Create(node.GetSpan(), captureSpan);
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
                if (node.Contains(virtualChar))
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
