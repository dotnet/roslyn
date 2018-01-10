// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Json;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.VirtualChars;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DocumentHighlighting
{
    internal abstract partial class AbstractDocumentHighlightsService : IDocumentHighlightsService
    {
        private async Task<ImmutableArray<DocumentHighlights>> TryGetJsonHighlightsAsync(
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
            var detector = JsonPatternDetector.TryGetOrCreate(
                semanticModel, syntaxFacts, document.GetLanguageService<ISemanticFactsService>(), document.GetLanguageService<IVirtualCharService>());
            if (detector == null || !detector.IsDefinitelyJson(token, cancellationToken))
            {
                return default;
            }

            var tree = detector.TryParseJson(token, cancellationToken);
            if (tree == null)
            {
                return default;
            }

            return GetHighlights(document, tree, position);
        }

        private ImmutableArray<DocumentHighlights> GetHighlights(
            Document document, JsonTree tree, int position)
        {
            var bracesOnTheRight = GetHighlights(document, tree, position, caretOnLeft: true);
            var bracesOnTheLeft = GetHighlights(document, tree, position - 1, caretOnLeft: false);

            if (!bracesOnTheRight.IsEmpty)
            {
                // We were on the left of an open open.  Return these highlights, and any 
                // highlights if we were on the right of a close paren.
                return bracesOnTheRight.Concat(bracesOnTheLeft);
            }

            // Nothing was on the right of the caret.  Return anything we were able to find on 
            // the left of the caret
            return bracesOnTheLeft;
        }

        private ImmutableArray<DocumentHighlights> GetHighlights(
            Document document, JsonTree tree, int position, bool caretOnLeft)
        {
            var virtualChar = tree.Text.FirstOrNullable(vc => vc.Span.Contains(position));
            if (virtualChar == null)
            {
                return ImmutableArray<DocumentHighlights>.Empty;
            }

            var ch = virtualChar.Value;
            if (caretOnLeft)
            {
                return ch == '{' || ch == '[' || ch == '('
                    ? FindBraceHighlights(document, tree, ch)
                    : ImmutableArray<DocumentHighlights>.Empty;
            }
            else
            {
                return ch == '}' || ch == ']' || ch == ')'
                    ? FindBraceHighlights(document, tree, ch)
                    : ImmutableArray<DocumentHighlights>.Empty;
            }
        }

        private ImmutableArray<DocumentHighlights> FindBraceHighlights(
            Document document, JsonTree tree, VirtualChar ch)
        {
            var node = FindObjectOrArrayNode(tree.Root, ch);
            switch (node)
            {
                case JsonObjectNode obj: return Create(document, obj.OpenBraceToken, obj.CloseBraceToken);
                case JsonArrayNode array: return Create(document, array.OpenBracketToken, array.CloseBracketToken);
                case JsonConstructorNode cons: return Create(document, cons.OpenParenToken, cons.CloseParenToken);
            }

            return default;
        }

        private ImmutableArray<DocumentHighlights> Create(Document document, JsonToken open, JsonToken close)
        {
            if (open.IsMissing || close.IsMissing)
            {
                return default;
            }

            return ImmutableArray.Create(new DocumentHighlights(
                document, ImmutableArray.Create(
                    new HighlightSpan(JsonHelpers.GetSpan(open), HighlightSpanKind.None),
                    new HighlightSpan(JsonHelpers.GetSpan(close), HighlightSpanKind.None))));
        }

        private JsonValueNode FindObjectOrArrayNode(JsonNode node, VirtualChar ch)
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

        private bool Matches(JsonToken openToken, JsonToken closeToken, VirtualChar ch)
            => openToken.VirtualChars.Contains(ch) || closeToken.VirtualChars.Contains(ch);
    }
}
