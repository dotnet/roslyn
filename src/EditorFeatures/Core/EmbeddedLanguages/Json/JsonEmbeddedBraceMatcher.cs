// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.EmbeddedLanguages.Json
{
    using JsonToken = EmbeddedSyntaxToken<JsonKind>;

    /// <summary>
    /// Brace matching impl for embedded json strings.
    /// </summary>
    internal class JsonEmbeddedBraceMatcher : IBraceMatcher
    {
        private readonly EmbeddedLanguageInfo _info;

        public JsonEmbeddedBraceMatcher(EmbeddedLanguageInfo info)
        {
            _info = info;
        }

        public async Task<BraceMatchingResult?> FindBracesAsync(
            Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken)
        {
            if (!options.HighlightRelatedJsonComponentsUnderCursor)
                return null;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var detector = JsonLanguageDetector.TryGetOrCreate(semanticModel.Compilation, _info);
            var tree = detector?.TryParseString(token, semanticModel, cancellationToken);
            if (tree == null)
                return null;

            return GetMatchingBraces(tree, position);
        }

        private static BraceMatchingResult? GetMatchingBraces(JsonTree tree, int position)
        {
            var virtualChar = tree.Text.FirstOrNull(vc => vc.Span.Contains(position));
            if (virtualChar == null)
                return null;

            var ch = virtualChar.Value;
            return ch.Value is '{' or '[' or '(' or '}' or ']' or ')'
                ? FindBraceHighlights(tree, ch)
                : null;
        }

        private static BraceMatchingResult? FindBraceHighlights(JsonTree tree, VirtualChar ch)
        {
            var node = FindObjectOrArrayNode(tree.Root, ch);
            return node switch
            {
                JsonObjectNode obj => Create(obj.OpenBraceToken, obj.CloseBraceToken),
                JsonArrayNode array => Create(array.OpenBracketToken, array.CloseBracketToken),
                JsonConstructorNode cons => Create(cons.OpenParenToken, cons.CloseParenToken),
                _ => null,
            };
        }

        private static BraceMatchingResult? Create(JsonToken open, JsonToken close)
        {
            return open.IsMissing || close.IsMissing
                ? null
                : new BraceMatchingResult(open.GetSpan(), close.GetSpan());
        }

        private static JsonValueNode? FindObjectOrArrayNode(JsonNode node, VirtualChar ch)
        {
            switch (node)
            {
                case JsonArrayNode array when Matches(array.OpenBracketToken, array.CloseBracketToken, ch):
                    return array;

                case JsonObjectNode obj when Matches(obj.OpenBraceToken, obj.CloseBraceToken, ch):
                    return obj;

                case JsonConstructorNode cons when Matches(cons.OpenParenToken, cons.CloseParenToken, ch):
                    return cons;
            }

            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    var result = FindObjectOrArrayNode(child.Node, ch);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        private static bool Matches(JsonToken openToken, JsonToken closeToken, VirtualChar ch)
            => openToken.VirtualChars.Contains(ch) || closeToken.VirtualChars.Contains(ch);
    }
}
