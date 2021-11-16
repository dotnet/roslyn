// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal static class CSharpSimplificationHelpers
    {
        public static SyntaxToken TryEscapeIdentifierToken(SyntaxToken syntaxToken, SyntaxNode parentOfToken)
        {
            // do not escape an already escaped identifier
            if (syntaxToken.IsVerbatimIdentifier())
            {
                return syntaxToken;
            }

            if (SyntaxFacts.GetKeywordKind(syntaxToken.ValueText) == SyntaxKind.None && SyntaxFacts.GetContextualKeywordKind(syntaxToken.ValueText) == SyntaxKind.None)
            {
                return syntaxToken;
            }

            if (SyntaxFacts.GetContextualKeywordKind(syntaxToken.ValueText) == SyntaxKind.UnderscoreToken)
            {
                return syntaxToken;
            }

            var parent = parentOfToken.Parent;
            if (parentOfToken is SimpleNameSyntax && parent.Kind() == SyntaxKind.XmlNameAttribute)
            {
                // do not try to escape XML name attributes
                return syntaxToken;
            }

            // do not escape global in a namespace qualified name
            if (parent.Kind() == SyntaxKind.AliasQualifiedName &&
                syntaxToken.ValueText == "global")
            {
                return syntaxToken;
            }

            // safe to escape identifier
            return syntaxToken.CopyAnnotationsTo(
                SyntaxFactory.VerbatimIdentifier(
                    syntaxToken.LeadingTrivia,
                    syntaxToken.ToString(),
                    syntaxToken.ValueText,
                    syntaxToken.TrailingTrivia))
                        .WithAdditionalAnnotations(Simplifier.Annotation);
        }

        public static T AppendElasticTriviaIfNecessary<T>(T rewrittenNode, T originalNode) where T : SyntaxNode
        {
            var firstRewrittenToken = rewrittenNode.GetFirstToken(true, false, true, true);
            var firstOriginalToken = originalNode.GetFirstToken(true, false, true, true);
            if (TryAddLeadingElasticTriviaIfNecessary(firstRewrittenToken, firstOriginalToken, out var rewrittenTokenWithLeadingElasticTrivia))
            {
                return rewrittenNode.ReplaceToken(firstRewrittenToken, rewrittenTokenWithLeadingElasticTrivia);
            }

            return rewrittenNode;
        }

        public static bool TryAddLeadingElasticTriviaIfNecessary(SyntaxToken token, SyntaxToken originalToken, out SyntaxToken tokenWithLeadingWhitespace)
        {
            tokenWithLeadingWhitespace = default;

            if (token.HasLeadingTrivia)
            {
                return false;
            }

            var previousToken = originalToken.GetPreviousToken();

            if (previousToken.HasTrailingTrivia)
            {
                return false;
            }

            tokenWithLeadingWhitespace = token.WithLeadingTrivia(SyntaxFactory.ElasticMarker).WithAdditionalAnnotations(Formatter.Annotation);
            return true;
        }
    }
}
