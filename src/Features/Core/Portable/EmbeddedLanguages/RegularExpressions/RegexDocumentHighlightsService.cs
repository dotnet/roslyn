﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions
{
    using RegexToken = EmbeddedSyntaxToken<RegexKind>;

    internal sealed class RegexDocumentHighlightsService : IDocumentHighlightsService
    {
        private readonly RegexEmbeddedLanguage _language;

        public RegexDocumentHighlightsService(RegexEmbeddedLanguage language)
            => _language = language;

        public async Task<ImmutableArray<DocumentHighlights>> GetDocumentHighlightsAsync(
            Document document, int position, IImmutableSet<Document> documentsToSearch, CancellationToken cancellationToken)
        {
            var option = document.Project.Solution.Workspace.Options.GetOption(RegularExpressionsOptions.HighlightRelatedRegexComponentsUnderCursor, document.Project.Language);
            if (!option)
            {
                return default;
            }

            var tree = await _language.TryGetTreeAtPositionAsync(document, position, cancellationToken).ConfigureAwait(false);
            return tree == null
                ? default
                : ImmutableArray.Create(new DocumentHighlights(document, GetHighlights(tree, position)));
        }

        private ImmutableArray<HighlightSpan> GetHighlights(RegexTree tree, int positionInDocument)
        {
            var referencesOnTheRight = GetReferences(tree, positionInDocument);
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
            var referencesOnTheLeft = GetReferences(tree, positionInDocument - 1);
            return referencesOnTheLeft;
        }

        private ImmutableArray<HighlightSpan> GetReferences(RegexTree tree, int position)
        {
            var virtualChar = tree.Text.FirstOrNull(vc => vc.Span.Contains(position));
            if (virtualChar == null)
            {
                return ImmutableArray<HighlightSpan>.Empty;
            }

            var ch = virtualChar.Value;
            return FindReferenceHighlights(tree, ch);
        }

        private ImmutableArray<HighlightSpan> FindReferenceHighlights(RegexTree tree, VirtualChar ch)
        {
            var node = FindReferenceNode(tree.Root, ch);
            if (node == null)
            {
                return ImmutableArray<HighlightSpan>.Empty;
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

            return ImmutableArray<HighlightSpan>.Empty;
        }

        private static ImmutableArray<HighlightSpan> CreateHighlights(
            RegexEscapeNode node, TextSpan captureSpan)
        {
            return ImmutableArray.Create(CreateHighlightSpan(node.GetSpan()), CreateHighlightSpan(captureSpan));
        }

        private static HighlightSpan CreateHighlightSpan(TextSpan textSpan)
            => new HighlightSpan(textSpan, HighlightSpanKind.None);

        private static RegexToken GetCaptureToken(RegexEscapeNode node)
            => node switch
            {
                RegexBackreferenceEscapeNode backReference => backReference.NumberToken,
                RegexCaptureEscapeNode captureEscape => captureEscape.CaptureToken,
                RegexKCaptureEscapeNode kCaptureEscape => kCaptureEscape.CaptureToken,
                _ => throw new InvalidOperationException(),
            };

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
