// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices
{
    using JsonToken = EmbeddedSyntaxToken<JsonKind>;

    /// <summary>
    /// Brace matcher impl for embedded json strings.
    /// </summary>
    [ExportEmbeddedLanguageBraceMatcher(
        PredefinedEmbeddedLanguageNames.Json,
        new[] { LanguageNames.CSharp, LanguageNames.VisualBasic },
        supportsUnannotatedAPIs: true,
        "Json"), Shared]
    internal sealed class JsonBraceMatcher : IEmbeddedLanguageBraceMatcher
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public JsonBraceMatcher()
        {
        }

        public BraceMatchingResult? FindBraces(
            Project project,
            SemanticModel semanticModel,
            SyntaxToken token,
            int position,
            BraceMatchingOptions options,
            CancellationToken cancellationToken)
        {
            if (!options.HighlightingOptions.HighlightRelatedJsonComponentsUnderCursor)
                return null;

            var info = project.GetRequiredLanguageService<IEmbeddedLanguagesProvider>().EmbeddedLanguageInfo;
            var detector = JsonLanguageDetector.GetOrCreate(semanticModel.Compilation, info);

            // We do support brace matching in strings that look very likely to be json, even if we aren't 100% certain
            // if it truly is json.
            var tree = detector.TryParseString(token, semanticModel, includeProbableStrings: true, cancellationToken);
            if (tree == null)
                return null;

            return GetMatchingBraces(tree, position);
        }

        private static BraceMatchingResult? GetMatchingBraces(JsonTree tree, int position)
        {
            var virtualChar = tree.Text.Find(position);
            if (virtualChar == null)
                return null;

            var ch = virtualChar.Value;
            return ch.Value is '{' or '[' or '(' or '}' or ']' or ')'
                ? FindBraceHighlights(tree, ch)
                : null;
        }

        private static BraceMatchingResult? FindBraceHighlights(JsonTree tree, VirtualChar ch)
            => FindBraceMatchingResult(tree.Root, ch);

        private static BraceMatchingResult? FindBraceMatchingResult(JsonNode node, VirtualChar ch)
        {
            var fullSpan = node.GetFullSpan();
            if (fullSpan == null)
                return null;

            if (!fullSpan.Value.Contains(ch.Span.Start))
                return null;

            switch (node)
            {
                case JsonArrayNode array when Matches(array.OpenBracketToken, array.CloseBracketToken, ch):
                    return Create(array.OpenBracketToken, array.CloseBracketToken);

                case JsonObjectNode obj when Matches(obj.OpenBraceToken, obj.CloseBraceToken, ch):
                    return Create(obj.OpenBraceToken, obj.CloseBraceToken);

                case JsonConstructorNode cons when Matches(cons.OpenParenToken, cons.CloseParenToken, ch):
                    return Create(cons.OpenParenToken, cons.CloseParenToken);
            }

            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    var result = FindBraceMatchingResult(child.Node, ch);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        private static BraceMatchingResult? Create(JsonToken open, JsonToken close)
           => open.IsMissing || close.IsMissing
               ? null
               : new BraceMatchingResult(open.GetSpan(), close.GetSpan());

        private static bool Matches(JsonToken openToken, JsonToken closeToken, VirtualChar ch)
            => openToken.VirtualChars.Contains(ch) || closeToken.VirtualChars.Contains(ch);
    }
}
