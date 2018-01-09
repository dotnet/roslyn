// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Json;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.VirtualChars;

namespace Microsoft.CodeAnalysis.Classification
{
    internal static class CommonJsonPatternTokenClassifier
    {
        private static ObjectPool<Visitor> _visitorPool = new ObjectPool<Visitor>(() => new Visitor());

        public static void AddClassifications(
            Workspace workspace, SyntaxToken token, SemanticModel semanticModel, ArrayBuilder<ClassifiedSpan> result,  
            ISyntaxFactsService syntaxFacts, ISemanticFactsService semanticFacts, IVirtualCharService virtualCharService,
            CancellationToken cancellationToken)
        {
            if (!workspace.Options.GetOption(JsonOptions.ColorizeJsonPatterns, LanguageNames.CSharp))
            {
                return;
            }

            // Do some quick syntactic checks before doing any complex work.
            if (JsonPatternDetector.IsDefinitelyNotJson(token, syntaxFacts))
            {
                return;
            }

            var detector = JsonPatternDetector.TryGetOrCreate(semanticModel, syntaxFacts, semanticFacts, virtualCharService);
            if (detector == null)
            {
                return;
            }

            if (!detector.IsDefinitelyJson(token, cancellationToken))
            {
                return;
            }

            var tree = detector?.TryParseJson(token, cancellationToken);
            if (tree == null)
            {
                return;
            }

            var visitor = _visitorPool.Allocate();
            try
            {
                visitor.Result = result;
                AddClassifications(tree.Root, visitor, result);
            }
            finally
            {
                visitor.Result = null;
                _visitorPool.Free(visitor);
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
            {
                AddTriviaClassifications(trivia, result);
            }

            foreach (var trivia in token.TrailingTrivia)
            {
                AddTriviaClassifications(trivia, result);
            }
        }

        private static void AddTriviaClassifications(JsonTrivia trivia, ArrayBuilder<ClassifiedSpan> result)
        {
            if ((trivia.Kind == JsonKind.MultiLineCommentTrivia || trivia.Kind == JsonKind.SingleLineCommentTrivia) &&
                trivia.VirtualChars.Length > 0)
            {
                result.Add(new ClassifiedSpan(
                    ClassificationTypeNames.JsonComment, JsonHelpers.GetSpan(trivia.VirtualChars)));
            }
        }

        private class Visitor : IJsonNodeVisitor
        {
            public ArrayBuilder<ClassifiedSpan> Result;

            private void AddClassification(JsonToken token, string typeName)
            {
                if (!token.IsMissing)
                {
                    Result.Add(new ClassifiedSpan(typeName, JsonHelpers.GetSpan(token)));
                }
            }

            private void ClassifyWholeNode(JsonNode node, string typeName)
            {
                foreach (var child in node)
                {
                    if (child.IsNode)
                    {
                        ClassifyWholeNode(child.Node, typeName);
                    }
                    else
                    {
                        AddClassification(child.Token, typeName);
                    }
                }
            }

            public void Visit(JsonCompilationUnit node)
            {
                // nothing to do.
            }

            public void Visit(JsonSequenceNode node)
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
            {
                VisitLiteral(node.LiteralToken);
            }

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

            public void Visit(JsonEmptyValueNode node)
            {
                AddClassification(node.CommaToken, ClassificationTypeNames.JsonPunctuation);
            }
        }
    }
}
