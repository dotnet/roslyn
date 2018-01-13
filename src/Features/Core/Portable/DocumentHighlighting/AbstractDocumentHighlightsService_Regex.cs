// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.RegularExpressions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VirtualChars;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DocumentHighlighting
{
    internal abstract partial class AbstractDocumentHighlightsService : IDocumentHighlightsService
    {
        private async Task<ImmutableArray<DocumentHighlights>> TryGetRegexPatternHighlightsAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var option = document.Project.Solution.Workspace.Options.GetOption(RegularExpressionsOptions.HighlightRelatedRegexComponentsUnderCursor, document.Project.Language);
            if (!option)
            {
                return default;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

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

            return GetHighlights(document, tree, position);
        }

        private ImmutableArray<DocumentHighlights> GetHighlights(
            Document document, RegexTree tree, int position)
        {
            var referencesOnTheRight = GetReferences(document, tree, position, caretOnLeft: true);
            if (!referencesOnTheRight.IsEmpty)
            {
                return referencesOnTheRight;
            }

            // Nothing was on the right of the caret.  Return anything we were able to find on 
            // the left of the caret
            var referencesOnTheLeft = GetReferences(document, tree, position - 1, caretOnLeft: false);
            return referencesOnTheLeft;
        }

        private ImmutableArray<DocumentHighlights> GetReferences(
            Document document, RegexTree tree, int position, bool caretOnLeft)
        {
            var virtualChar = tree.Text.FirstOrNullable(vc => vc.Span.Contains(position));
            if (virtualChar == null)
            {
                return ImmutableArray<DocumentHighlights>.Empty;
            }

            var ch = virtualChar.Value;
            return FindReferenceHighlights(document, tree, ch);
        }

        private ImmutableArray<DocumentHighlights> FindReferenceHighlights(
            Document document, RegexTree tree, VirtualChar ch)
        {
            var node = FindReferenceNode(tree.Root, ch);
            if (node == null)
            {
                return ImmutableArray<DocumentHighlights>.Empty;
            }

            var captureToken = GetCaptureToken(node);
            if (captureToken.Kind == RegexKind.NumberToken)
            {
                var val = (int)captureToken.Value;
                if (tree.CaptureNumbersToSpan.TryGetValue(val, out var captureSpan))
                {
                    return CreateHighlights(document, node, captureSpan);
                }
            }
            else
            {
                var val = (string)captureToken.Value;
                if (tree.CaptureNamesToSpan.TryGetValue(val, out var captureSpan))
                {
                    return CreateHighlights(document, node, captureSpan);
                }
            }

            return ImmutableArray<DocumentHighlights>.Empty;
        }

        private ImmutableArray<DocumentHighlights> CreateHighlights(
            Document document, RegexEscapeNode node, TextSpan captureSpan)
        {
            return ImmutableArray.Create(new DocumentHighlights(document,
                ImmutableArray.Create(
                    new HighlightSpan(RegexHelpers.GetSpan(node), HighlightSpanKind.None),
                    new HighlightSpan(captureSpan, HighlightSpanKind.None))));
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
