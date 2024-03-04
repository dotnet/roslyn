// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;

using static EmbeddedSyntaxHelpers;

using JsonToken = EmbeddedSyntaxToken<JsonKind>;
using JsonTrivia = EmbeddedSyntaxTrivia<JsonKind>;

/// <summary>
/// Classifier impl for embedded json strings.
/// </summary>
[ExportEmbeddedLanguageClassifier(
    PredefinedEmbeddedLanguageNames.Json,
    [LanguageNames.CSharp, LanguageNames.VisualBasic],
    supportsUnannotatedAPIs: true, "Json"), Shared]
internal sealed class JsonClassifier : IEmbeddedLanguageClassifier
{
    private static readonly ObjectPool<Visitor> s_visitorPool = new(() => new Visitor());

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public JsonClassifier()
    {
    }

    public void RegisterClassifications(EmbeddedLanguageClassificationContext context)
    {
        var info = context.Project.GetRequiredLanguageService<IEmbeddedLanguagesProvider>().EmbeddedLanguageInfo;

        var token = context.SyntaxToken;
        if (!info.IsAnyStringLiteral(token.RawKind))
            return;

        if (!context.Options.ColorizeJsonPatterns)
            return;

        var semanticModel = context.SemanticModel;
        var detector = JsonLanguageDetector.GetOrCreate(semanticModel.Compilation, info);

        // We do support json classification in strings that look very likely to be json, even if we aren't 100%
        // certain if it truly is json.
        var tree = detector.TryParseString(token, semanticModel, includeProbableStrings: true, context.CancellationToken);
        if (tree == null)
            return;

        var visitor = s_visitorPool.Allocate();
        try
        {
            visitor.Context = context;
            AddClassifications(tree.Root, visitor, context);
        }
        finally
        {
            visitor.Context = default;
            s_visitorPool.Free(visitor);
        }
    }

    private static void AddClassifications(JsonNode node, Visitor visitor, EmbeddedLanguageClassificationContext context)
    {
        node.Accept(visitor);

        foreach (var child in node)
        {
            if (child.IsNode)
            {
                AddClassifications(child.Node, visitor, context);
            }
            else
            {
                AddTokenClassifications(child.Token, context);
            }
        }
    }

    private static void AddTokenClassifications(JsonToken token, EmbeddedLanguageClassificationContext context)
    {
        foreach (var trivia in token.LeadingTrivia)
            AddTriviaClassifications(trivia, context);

        if (!token.IsMissing)
        {
            switch (token.Kind)
            {
                case JsonKind.CommaToken:
                    context.AddClassification(ClassificationTypeNames.JsonPunctuation, token.GetSpan());
                    break;
            }
        }

        foreach (var trivia in token.TrailingTrivia)
            AddTriviaClassifications(trivia, context);
    }

    private static void AddTriviaClassifications(JsonTrivia trivia, EmbeddedLanguageClassificationContext context)
    {
        if (trivia.Kind is JsonKind.MultiLineCommentTrivia or JsonKind.SingleLineCommentTrivia &&
            trivia.VirtualChars.Length > 0)
        {
            context.AddClassification(ClassificationTypeNames.JsonComment, GetSpan(trivia.VirtualChars));
        }
    }

    private class Visitor : IJsonNodeVisitor
    {
        public EmbeddedLanguageClassificationContext Context;

        private void AddClassification(JsonToken token, string typeName)
        {
            if (!token.IsMissing)
                Context.AddClassification(typeName, token.GetSpan());
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
            // Already handled when we recurse in AddTokenClassifications.  Specifically, commas show up both as
            // nodes (with tokens in them) in error recovery scenarios, and also just as tokens in a separated list.
            // So, to handle both, we just handle the token case in AddTokenClassifications.
        }
    }
}
