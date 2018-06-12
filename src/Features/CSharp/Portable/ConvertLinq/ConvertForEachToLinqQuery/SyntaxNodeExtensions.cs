// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery
{
    internal static class SyntaxNodeAndSyntaxTokenExtensions
    {
        public static T WithComments<T>(this T node, IEnumerable<SyntaxTrivia> leadingTrivia, IEnumerable<SyntaxTrivia> trailingTrivia) where T : SyntaxNode
            => node.WithCommentsBeforeLeadingTrivia(leadingTrivia).WithCommentsAfterTrailingTrivia(trailingTrivia);

        public static T WithComments<T>(this T node, IEnumerable<SyntaxToken> leadingTokens, IEnumerable<SyntaxToken> trailingTokens) where T : SyntaxNode
            => node.WithCommentsBeforeLeadingTrivia(Helpers.GetTrivia(leadingTokens)).WithCommentsAfterTrailingTrivia(Helpers.GetTrivia(trailingTokens));

        public static SyntaxToken WithComments(this SyntaxToken token, IEnumerable<SyntaxTrivia> leadingTrivia, IEnumerable<SyntaxTrivia> trailingTrivia)
         => token.WithCommentsBeforeLeadingTrivia(leadingTrivia).WithCommentsAfterTrailingTrivia(trailingTrivia);

        public static SyntaxToken WithCommentsBeforeLeadingTrivia(this SyntaxToken token, IEnumerable<SyntaxTrivia> trivia, params SyntaxNodeOrToken[] nodesOrTokens)
            => token.WithLeadingTrivia( FilterComments(trivia.Concat(Helpers.GetTrivia(nodesOrTokens)).Concat(token.LeadingTrivia)));

        public static T WithCommentsBeforeLeadingTrivia<T>(this T node, IEnumerable<SyntaxTrivia> trivia, params SyntaxNodeOrToken[] nodesOrTokens) where T : SyntaxNode
            => node.WithLeadingTrivia(FilterComments(trivia.Concat(Helpers.GetTrivia(nodesOrTokens)).Concat(node.GetLeadingTrivia())));

        public static SyntaxToken WithCommentsAfterTrailingTrivia(this SyntaxToken token, IEnumerable<SyntaxTrivia> trivia, params SyntaxNodeOrToken[] nodesOrTokens)
            => token.WithTrailingTrivia(FilterComments(token.TrailingTrivia.Concat(Helpers.GetTrivia(nodesOrTokens).Concat(trivia))));

        public static T WithCommentsAfterTrailingTrivia<T>(this T node, IEnumerable<SyntaxTrivia> trivia, params SyntaxNodeOrToken[] nodesOrTokens) where T : SyntaxNode
            => node.WithTrailingTrivia(FilterComments(node.GetTrailingTrivia().Concat(Helpers.GetTrivia(nodesOrTokens).Concat(trivia))));

        public static T KeepCommentsAndAddElasticMarkers<T>(this T node) where T : SyntaxNode
            => node
                    .WithTrailingTrivia(AddElasticMarker(FilterComments(node.GetTrailingTrivia())))
                    .WithLeadingTrivia(AddElasticMarker(FilterComments(node.GetLeadingTrivia())));

        public static SyntaxToken KeepCommentsAndAddElasticMarkers(this SyntaxToken token)
            => token
                .WithTrailingTrivia(AddElasticMarker(FilterComments(token.TrailingTrivia)))
                .WithLeadingTrivia(AddElasticMarker(FilterComments(token.LeadingTrivia)));

        private static IEnumerable<SyntaxTrivia> AddElasticMarker(IEnumerable<SyntaxTrivia> trivia)
            => trivia.Concat(new[] { SyntaxFactory.ElasticMarker });

        private static IEnumerable<SyntaxTrivia> FilterComments(IEnumerable<SyntaxTrivia> trivia)
        {
            var previousIsSingleLineComment = false;
            foreach (var t in trivia)
            {
                if (previousIsSingleLineComment && t.IsEndOfLine())
                {
                    yield return t;
                }

                if (t.IsSingleOrMultiLineComment())
                {
                    yield return t;
                }

                previousIsSingleLineComment = t.IsSingleLineComment();
            }
        }
    }
}
