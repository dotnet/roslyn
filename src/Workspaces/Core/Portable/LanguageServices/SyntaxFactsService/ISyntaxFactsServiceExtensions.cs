﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal static class ISyntaxFactsServiceExtensions
    {
        public static bool IsWord(this ISyntaxFactsService syntaxFacts, SyntaxToken token)
        {
            return syntaxFacts.IsIdentifier(token)
                || syntaxFacts.IsKeyword(token)
                || syntaxFacts.IsContextualKeyword(token)
                || syntaxFacts.IsPreprocessorKeyword(token);
        }

        public static bool IsAnyMemberAccessExpression(
            this ISyntaxFactsService syntaxFacts, SyntaxNode node)
        {
            return syntaxFacts.IsSimpleMemberAccessExpression(node) || syntaxFacts.IsPointerMemberAccessExpression(node);
        }

        public static bool IsRegularOrDocumentationComment(this ISyntaxFactsService syntaxFacts, SyntaxTrivia trivia)
            => syntaxFacts.IsRegularComment(trivia) || syntaxFacts.IsDocumentationComment(trivia);

        public static ImmutableArray<SyntaxTrivia> GetTriviaAfterLeadingBlankLines(
            this ISyntaxFactsService syntaxFacts, SyntaxNode node)
        {
            var leadingBlankLines = syntaxFacts.GetLeadingBlankLines(node);
            return node.GetLeadingTrivia().Skip(leadingBlankLines.Length).ToImmutableArray();
        }

        public static void GetPartsOfAssignmentStatement( 
            this ISyntaxFactsService syntaxFacts, SyntaxNode statement, 
            out SyntaxNode left, out SyntaxNode right)
        {
            syntaxFacts.GetPartsOfAssignmentStatement(statement, out left, out _, out right);
        }

        public static SyntaxNode Unparenthesize(
            this ISyntaxFactsService syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetPartsOfParenthesizedExpression(node,
                out var openParenToken, out var expression, out var closeParenToken);

            var leadingTrivia = openParenToken.LeadingTrivia
                .Concat(openParenToken.TrailingTrivia)
                .Where(t => !syntaxFacts.IsElastic(t))
                .Concat(expression.GetLeadingTrivia());

            var trailingTrivia = expression.GetTrailingTrivia()
                .Concat(closeParenToken.LeadingTrivia)
                .Where(t => !syntaxFacts.IsElastic(t))
                .Concat(closeParenToken.TrailingTrivia);

            var resultNode = expression
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(trailingTrivia);

            return resultNode;
        }

        public static bool SpansPreprocessorDirective(this ISyntaxFactsService service, SyntaxNode node)
            => service.SpansPreprocessorDirective(SpecializedCollections.SingletonEnumerable(node));

        public static bool IsWhitespaceOrEndOfLineTrivia(this ISyntaxFactsService syntaxFacts, SyntaxTrivia trivia)
            => syntaxFacts.IsWhitespaceTrivia(trivia) || syntaxFacts.IsEndOfLineTrivia(trivia);

        public static void GetPartsOfBinaryExpression(this ISyntaxFactsService syntaxFacts, SyntaxNode node, out SyntaxNode left, out SyntaxNode right)
            => syntaxFacts.GetPartsOfBinaryExpression(node, out left, out _, out right);

        public static SyntaxNode GetExpressionOfParenthesizedExpression(this ISyntaxFactsService syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetPartsOfParenthesizedExpression(node, out _, out var expression, out _);
            return expression;
        }

        public static SyntaxToken GetOperatorTokenOfBinaryExpression(this ISyntaxFactsService syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetPartsOfBinaryExpression(node, out _, out var token, out _);
            return token;
        }
    }
}
