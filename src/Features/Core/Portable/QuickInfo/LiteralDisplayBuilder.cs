// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal static class LiteralDisplayBuilder
    {
        public static ImmutableArray<TaggedText> Build(ISyntaxFactsService syntaxFacts, SyntaxGenerator generator, object value)
        {
            var textBuilder = ImmutableArray.CreateBuilder<TaggedText>();
            AddConcatenations(textBuilder, syntaxFacts, generator.LiteralExpression(value));
            return textBuilder.ToImmutable();
        }

        private static void AddConcatenations(
            ImmutableArray<TaggedText>.Builder textBuilder,
            ISyntaxFactsService syntaxFacts,
            SyntaxNode node)
        {
            if (syntaxFacts.IsBinaryExpression(node))
            {
                syntaxFacts.GetPartsOfBinaryExpression(
                    node,
                    out var leftNode,
                    out var concatenateToken,
                    out node);

                AddConcatenations(textBuilder, syntaxFacts, leftNode);
                textBuilder.AddOperator(concatenateToken.Text);
            }

            node = WalkDownMemberAccess(syntaxFacts, node);

            foreach (var token in node.DescendantTokens())
            {
                textBuilder.Add(TagToken(syntaxFacts, token));
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

        private static TaggedText TagToken(ISyntaxFactsService syntaxFacts, SyntaxToken token)
        {
            var tag =
                syntaxFacts.IsCharacterLiteral(token) || syntaxFacts.IsStringLiteral(token) ? TextTags.StringLiteral :
                syntaxFacts.IsNumericLiteral(token) ? TextTags.NumericLiteral :
                syntaxFacts.IsKeyword(token) ? TextTags.Keyword :
                TextTags.Text;

            return new TaggedText(tag, token.Text);
        }
    }
}
