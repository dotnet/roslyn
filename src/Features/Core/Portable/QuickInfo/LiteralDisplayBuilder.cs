// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal static class LiteralDisplayBuilder
    {
        public static ImmutableArray<TaggedText> Build(HostLanguageServices languageServices, object value)
        {
            var generator = languageServices.GetRequiredService<SyntaxGenerator>();
            var classificationHelpers = languageServices.GetRequiredService<IClassificationHelpersService>();

            var textBuilder = ImmutableArray.CreateBuilder<TaggedText>();
            AddConcatenations(textBuilder, generator.SyntaxFacts, classificationHelpers, generator.LiteralExpression(value));
            return textBuilder.ToImmutable();
        }

        private static void AddConcatenations(
            ImmutableArray<TaggedText>.Builder textBuilder,
            ISyntaxFactsService syntaxFacts,
            IClassificationHelpersService classificationHelpers,
            SyntaxNode node)
        {
            if (syntaxFacts.IsBinaryExpression(node))
            {
                syntaxFacts.GetPartsOfBinaryExpression(
                    node,
                    out var leftNode,
                    out var concatenateToken,
                    out node);

                AddConcatenations(textBuilder, syntaxFacts, classificationHelpers, leftNode);
                textBuilder.AddOperator(concatenateToken.Text);
            }

            node = WalkDownMemberAccess(syntaxFacts, node);

            foreach (var token in node.DescendantTokens())
            {
                var tag = GetTag(classificationHelpers.GetClassification(token));
                textBuilder.Add(new TaggedText(tag, token.Text));
            }
        }

        private static SyntaxNode WalkDownMemberAccess(ISyntaxFactsService syntaxFacts, SyntaxNode node)
        {
            while (syntaxFacts.IsSimpleMemberAccessExpression(node))
            {
                node = syntaxFacts.GetNameOfMemberAccessExpression(node);
            }

            return node;
        }

        private static string GetTag(string classificationType)
        {
            switch (classificationType)
            {
                case ClassificationTypeNames.NumericLiteral: return TextTags.NumericLiteral;
                case ClassificationTypeNames.StringLiteral: return TextTags.StringLiteral;
                case ClassificationTypeNames.Keyword: return TextTags.Keyword;
                case ClassificationTypeNames.Operator: return TextTags.Operator;
                case ClassificationTypeNames.Punctuation: return TextTags.Punctuation;
                case ClassificationTypeNames.Identifier: return TextTags.Field;
                default: return TextTags.Text;
            }
        }
    }
}
