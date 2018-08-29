// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Json.LanguageServices
{
    using JsonToken = EmbeddedSyntaxToken<JsonKind>;

    /// <summary>
    /// Brace matching impl for embedded json strings.
    /// </summary>
    internal class JsonEmbeddedBraceMatcher : IEmbeddedBraceMatcher
    {
        private readonly JsonEmbeddedLanguage _language;

        public JsonEmbeddedBraceMatcher(JsonEmbeddedLanguage language)
        {
            _language = language;
        }

        public async Task<EmbeddedBraceMatchingResult?> FindBracesAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var option = document.Project.Solution.Workspace.Options.GetOption(JsonFeatureOptions.HighlightRelatedJsonComponentsUnderCursor, document.Project.Language);
            if (!option)
            {
                return default;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (JsonPatternDetector.IsDefinitelyNotJson(token, syntaxFacts))
            {
                return default;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var detector = JsonPatternDetector.GetOrCreate(semanticModel, _language);
            if (!detector.IsDefinitelyJson(token, cancellationToken))
            {
                return default;
            }

            var tree = detector.TryParseJson(token);
            if (tree == null)
            {
                return default;
            }

            return GetMatchingBraces(tree, position);
        }

        private static EmbeddedBraceMatchingResult? GetMatchingBraces(JsonTree tree, int position)
        {
            var virtualChar = tree.Text.FirstOrNullable(vc => vc.Span.Contains(position));
            if (virtualChar == null)
            {
                return null;
            }

            var ch = virtualChar.Value;
            if (ch == '{' || ch == '[' || ch == '(' ||
                ch == '}' || ch == ']' || ch == ')')
            {
                return FindBraceHighlights(tree, ch);
            }

            return null;
        }

        private static EmbeddedBraceMatchingResult? FindBraceHighlights(JsonTree tree, VirtualChar ch)
        {
            var node = FindObjectOrArrayNode(tree.Root, ch);
            switch (node)
            {
                case JsonObjectNode obj: return Create(obj.OpenBraceToken, obj.CloseBraceToken);
                case JsonArrayNode array: return Create(array.OpenBracketToken, array.CloseBracketToken);
                case JsonConstructorNode cons: return Create(cons.OpenParenToken, cons.CloseParenToken);
            }

            return default;
        }

        private static EmbeddedBraceMatchingResult? Create(JsonToken open, JsonToken close)
        {
            if (open.IsMissing || close.IsMissing)
            {
                return default;
            }

            return new EmbeddedBraceMatchingResult(open.GetSpan(), close.GetSpan());
        }

        private static JsonValueNode FindObjectOrArrayNode(JsonNode node, VirtualChar ch)
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
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        private static bool Matches(JsonToken openToken, JsonToken closeToken, VirtualChar ch)
            => openToken.VirtualChars.Contains(ch) || closeToken.VirtualChars.Contains(ch);
    }
}
