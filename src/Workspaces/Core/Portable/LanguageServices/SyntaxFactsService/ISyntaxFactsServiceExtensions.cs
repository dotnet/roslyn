// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal static class ISyntaxFactsServiceExtensions
    {
        public static bool IsLegalIdentifier(this ISyntaxFactsService syntaxFacts, string name)
        {
            if (name.Length == 0)
            {
                return false;
            }

            if (!syntaxFacts.IsIdentifierStartCharacter(name[0]))
            {
                return false;
            }

            for (var i = 1; i < name.Length; i++)
            {
                if (!syntaxFacts.IsIdentifierPartCharacter(name[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsReservedOrContextualKeyword(this ISyntaxFactsService syntaxFacts, SyntaxToken token)
            => syntaxFacts.IsReservedKeyword(token) || syntaxFacts.IsContextualKeyword(token);

        public static bool IsWord(this ISyntaxFactsService syntaxFacts, SyntaxToken token)
        {
            return syntaxFacts.IsIdentifier(token)
                || syntaxFacts.IsReservedOrContextualKeyword(token)
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

        public static SyntaxNode GetExpressionOfInvocationExpression(
            this ISyntaxFactsService syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetPartsOfInvocationExpression(node, out var expression, out _);
            return expression;
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

            // If there's no trivia between the original node and the tokens around it, then add
            // elastic markers so the formatting engine will spaces if necessary to keep things
            // parseable.
            if (resultNode.GetLeadingTrivia().Count == 0)
            {
                var previousToken = node.GetFirstToken().GetPreviousToken();
                if (previousToken.TrailingTrivia.Count == 0 &&
                    syntaxFacts.IsWordOrNumber(previousToken) &&
                    syntaxFacts.IsWordOrNumber(resultNode.GetFirstToken()))
                {
                    resultNode = resultNode.WithPrependedLeadingTrivia(syntaxFacts.ElasticMarker);
                }
            }

            if (resultNode.GetTrailingTrivia().Count == 0)
            {
                var nextToken = node.GetLastToken().GetNextToken();
                if (nextToken.LeadingTrivia.Count == 0 &&
                    syntaxFacts.IsWordOrNumber(nextToken) &&
                    syntaxFacts.IsWordOrNumber(resultNode.GetLastToken()))
                {
                    resultNode = resultNode.WithAppendedTrailingTrivia(syntaxFacts.ElasticMarker);
                }
            }

            return resultNode;
        }

        private static bool IsWordOrNumber(this ISyntaxFactsService syntaxFacts, SyntaxToken token)
            => syntaxFacts.IsWord(token) || syntaxFacts.IsNumericLiteral(token);

        public static bool SpansPreprocessorDirective(this ISyntaxFactsService service, SyntaxNode node)
            => service.SpansPreprocessorDirective(SpecializedCollections.SingletonEnumerable(node));

        public static bool SpansPreprocessorDirective(this ISyntaxFactsService service, params SyntaxNode[] nodes)
            => service.SpansPreprocessorDirective(nodes);

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

        public static bool IsAnonymousOrLocalFunction(this ISyntaxFactsService syntaxFacts, SyntaxNode node)
            => syntaxFacts.IsAnonymousFunction(node) ||
               syntaxFacts.IsLocalFunctionStatement(node);

        public static SyntaxNode GetExpressionOfElementAccessExpression(this ISyntaxFactsService syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetPartsOfElementAccessExpression(node, out var expression, out _);
            return expression;
        }

        public static SyntaxNode GetArgumentListOfElementAccessExpression(this ISyntaxFactsService syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetPartsOfElementAccessExpression(node, out _, out var argumentList);
            return argumentList;
        }

        public static SyntaxNode GetExpressionOfConditionalAccessExpression(this ISyntaxFactsService syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetPartsOfConditionalAccessExpression(node, out var expression, out _);
            return expression;
        }

        public static SyntaxToken GetOperatorTokenOfMemberAccessExpression(this ISyntaxFactsService syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetPartsOfMemberAccessExpression(node, out _, out var operatorToken, out _);
            return operatorToken;
        }

        public static void GetPartsOfMemberAccessExpression(this ISyntaxFactsService syntaxFacts, SyntaxNode node, out SyntaxNode expression, out SyntaxNode name)
            => syntaxFacts.GetPartsOfMemberAccessExpression(node, out expression, out _, out name);

        public static void GetPartsOfConditionalAccessExpression(this ISyntaxFactsService syntaxFacts, SyntaxNode node, out SyntaxNode expression, out SyntaxNode whenNotNull)
            => syntaxFacts.GetPartsOfConditionalAccessExpression(node, out expression, out _, out whenNotNull);
    }
}
