// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis.Classification.Classifiers;
    using static EmbeddedSyntaxHelpers;

    using JsonToken = EmbeddedSyntaxToken<JsonKind>;
    using JsonTrivia = EmbeddedSyntaxTrivia<JsonKind>;

    /// <summary>
    /// Classifier impl for embedded json strings.
    /// </summary>
    internal class JsonEmbeddedClassifier : AbstractSyntaxClassifier
    {
        private static readonly ObjectPool<Visitor> s_visitorPool = new(() => new Visitor());
        private readonly EmbeddedLanguageInfo _info;

        public override ImmutableArray<int> SyntaxTokenKinds { get; }

        public JsonEmbeddedClassifier(EmbeddedLanguageInfo info)
        {
            _info = info;
            SyntaxTokenKinds = info.AllStringLiteralKinds;
        }

        public override void AddClassifications(
            SyntaxToken token,
            SemanticModel semanticModel,
            ClassificationOptions options,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            if (!_info.IsAnyStringLiteral(token.RawKind))
                return;

            if (!options.ColorizeJsonPatterns)
                return;

            var detector = JsonLanguageDetector.GetOrCreate(semanticModel.Compilation, _info);

            // We do support json classification in strings that look very likely to be json, even if we aren't 100%
            // certain if it truly is json.
            var tree = detector.TryParseString(token, semanticModel, includeProbableStrings: true, cancellationToken);
            if (tree == null)
                return;

            var visitor = s_visitorPool.Allocate();
            try
            {
                visitor.Result = result;
                AddClassifications(tree.Root, visitor, result);
            }
            finally
            {
                visitor.Result = null;
                s_visitorPool.Free(visitor);
            }
        }

        private static void AddClassifications(JsonNode node, Visitor visitor, ArrayBuilder<ClassifiedSpan> result)
        {
            node.Accept(visitor);

            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    AddClassifications(child.Node, visitor, result);
                }
                else
                {
                    AddTriviaClassifications(child.Token, result);
                }
            }
        }

        private static void AddTriviaClassifications(JsonToken token, ArrayBuilder<ClassifiedSpan> result)
        {
            foreach (var trivia in token.LeadingTrivia)
                AddTriviaClassifications(trivia, result);

            foreach (var trivia in token.TrailingTrivia)
                AddTriviaClassifications(trivia, result);
        }

        private static void AddTriviaClassifications(JsonTrivia trivia, ArrayBuilder<ClassifiedSpan> result)
        {
            if (trivia.Kind is JsonKind.MultiLineCommentTrivia or JsonKind.SingleLineCommentTrivia &&
                trivia.VirtualChars.Length > 0)
            {
                result.Add(new ClassifiedSpan(
                    ClassificationTypeNames.JsonComment, GetSpan(trivia.VirtualChars)));
            }
        }

        private class Visitor : IJsonNodeVisitor
        {
            public ArrayBuilder<ClassifiedSpan>? Result;

            private void AddClassification(JsonToken token, string typeName)
            {
                if (!token.IsMissing)
                    Result!.Add(new ClassifiedSpan(typeName, token.GetSpan()));
            }

            public void Visit(JsonCompilationUnit node)
            {
                // nothing to do.
            }

            public void Visit(JsonArrayNode node)
            {
                AddClassification(node.OpenBracketToken, ClassificationTypeNames.JsonArray);
                AddClassification(node.CloseBracketToken, ClassificationTypeNames.JsonArray);
            }

            public void Visit(JsonObjectNode node)
            {
                AddClassification(node.OpenBraceToken, ClassificationTypeNames.JsonObject);
                AddClassification(node.CloseBraceToken, ClassificationTypeNames.JsonObject);
            }

            public void Visit(JsonPropertyNode node)
            {
                AddClassification(node.NameToken, ClassificationTypeNames.JsonPropertyName);
                AddClassification(node.ColonToken, ClassificationTypeNames.JsonPunctuation);
            }

            public void Visit(JsonConstructorNode node)
            {
                AddClassification(node.NewKeyword, ClassificationTypeNames.JsonKeyword);
                AddClassification(node.NameToken, ClassificationTypeNames.JsonConstructorName);
                AddClassification(node.OpenParenToken, ClassificationTypeNames.JsonPunctuation);
                AddClassification(node.CloseParenToken, ClassificationTypeNames.JsonPunctuation);
            }

            public void Visit(JsonLiteralNode node)
                => VisitLiteral(node.LiteralToken);

            private void VisitLiteral(JsonToken literalToken)
            {
                switch (literalToken.Kind)
                {
                    case JsonKind.NumberToken:
                        AddClassification(literalToken, ClassificationTypeNames.JsonNumber);
                        return;

                    case JsonKind.StringToken:
                        AddClassification(literalToken, ClassificationTypeNames.JsonString);
                        return;

                    case JsonKind.TrueLiteralToken:
                    case JsonKind.FalseLiteralToken:
                    case JsonKind.NullLiteralToken:
                    case JsonKind.UndefinedLiteralToken:
                    case JsonKind.NaNLiteralToken:
                    case JsonKind.InfinityLiteralToken:
                        AddClassification(literalToken, ClassificationTypeNames.JsonKeyword);
                        return;

                    default:
                        AddClassification(literalToken, ClassificationTypeNames.JsonText);
                        return;
                }
            }

            public void Visit(JsonNegativeLiteralNode node)
            {
                AddClassification(node.MinusToken, ClassificationTypeNames.JsonOperator);
                VisitLiteral(node.LiteralToken);
            }

            public void Visit(JsonTextNode node)
            {
                VisitLiteral(node.TextToken);
            }

            public void Visit(JsonCommaValueNode node)
            {
                AddClassification(node.CommaToken, ClassificationTypeNames.JsonPunctuation);
            }
        }
    }
}
