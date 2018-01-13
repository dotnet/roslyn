// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Json;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.VirtualChars;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching
{
    internal static class CommonJsonBraceMatcher
    {
        internal static async Task<BraceMatchingResult?> FindBracesAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var option = document.Project.Solution.Workspace.Options.GetOption(JsonOptions.HighlightRelatedJsonComponentsUnderCursor, document.Project.Language);
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
            var detector = JsonPatternDetector.GetOrCreate(
                semanticModel, syntaxFacts, document.GetLanguageService<ISemanticFactsService>(), document.GetLanguageService<IVirtualCharService>());
            if (!detector.IsDefinitelyJson(token, cancellationToken))
            {
                return default;
            }

            var tree = detector.TryParseJson(token, cancellationToken);
            if (tree == null)
            {
                return default;
            }

            return GetMatchingBraces(tree, position);
        }

        private static BraceMatchingResult? GetMatchingBraces(JsonTree tree, int position)
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

        private static BraceMatchingResult? FindBraceHighlights(JsonTree tree, VirtualChar ch)
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

        private static BraceMatchingResult? Create(JsonToken open, JsonToken close)
        {
            if (open.IsMissing || close.IsMissing)
            {
                return default;
            }

            return new BraceMatchingResult(
                JsonHelpers.GetSpan(open),
                JsonHelpers.GetSpan(close));
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
