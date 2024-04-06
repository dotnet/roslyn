// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices;

using RegexToken = EmbeddedSyntaxToken<RegexKind>;

[ExportEmbeddedLanguageDocumentHighlighter(
    PredefinedEmbeddedLanguageNames.Regex,
    [LanguageNames.CSharp, LanguageNames.VisualBasic],
    supportsUnannotatedAPIs: true, "Regex", "Regexp"), Shared]
internal sealed class RegexDocumentHighlighter : IEmbeddedLanguageDocumentHighlighter
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RegexDocumentHighlighter()
    {
    }

    public ImmutableArray<DocumentHighlights> GetDocumentHighlights(
        Document document, SemanticModel semanticModel, SyntaxToken token, int position, HighlightingOptions options, CancellationToken cancellationToken)
    {
        if (!options.HighlightRelatedRegexComponentsUnderCursor)
            return default;

        var info = document.GetRequiredLanguageService<IEmbeddedLanguagesProvider>().EmbeddedLanguageInfo;

        var detector = RegexLanguageDetector.GetOrCreate(semanticModel.Compilation, info);
        var tree = detector.TryParseString(token, semanticModel, cancellationToken);

        return tree == null
            ? default
            : [new DocumentHighlights(document, GetHighlights(tree, position))];
    }

    private static ImmutableArray<HighlightSpan> GetHighlights(RegexTree tree, int positionInDocument)
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

    private static ImmutableArray<HighlightSpan> GetReferences(RegexTree tree, int position)
    {
        var virtualChar = tree.Text.Find(position);
        if (virtualChar == null)
            return [];

        var ch = virtualChar.Value;
        return FindReferenceHighlights(tree, ch);
    }

    private static ImmutableArray<HighlightSpan> FindReferenceHighlights(RegexTree tree, VirtualChar ch)
    {
        var node = FindReferenceNode(tree.Root, ch);
        if (node == null)
        {
            return [];
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

        return [];
    }

    private static ImmutableArray<HighlightSpan> CreateHighlights(
        RegexEscapeNode node, TextSpan captureSpan)
    {
        return [CreateHighlightSpan(node.GetSpan()), CreateHighlightSpan(captureSpan)];
    }

    private static HighlightSpan CreateHighlightSpan(TextSpan textSpan)
        => new(textSpan, HighlightSpanKind.None);

    private static RegexToken GetCaptureToken(RegexEscapeNode node)
        => node switch
        {
            RegexBackreferenceEscapeNode backReference => backReference.NumberToken,
            RegexCaptureEscapeNode captureEscape => captureEscape.CaptureToken,
            RegexKCaptureEscapeNode kCaptureEscape => kCaptureEscape.CaptureToken,
            _ => throw new InvalidOperationException(),
        };

    private static RegexEscapeNode? FindReferenceNode(RegexNode node, VirtualChar virtualChar)
    {
        if (node.Kind is RegexKind.BackreferenceEscape or
            RegexKind.CaptureEscape or
            RegexKind.KCaptureEscape)
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
