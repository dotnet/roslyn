// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServices;

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
    }
}
