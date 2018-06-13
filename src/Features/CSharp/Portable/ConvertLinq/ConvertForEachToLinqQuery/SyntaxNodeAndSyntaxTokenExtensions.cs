// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery
{
    internal static class SyntaxNodeAndSyntaxTokenExtensions
    {
        public static T WithComments<T>(this T node, IEnumerable<SyntaxToken> leadingTokens, IEnumerable<SyntaxToken> trailingTokens) where T : SyntaxNode
            => node.WithComments(Helpers.GetTrivia(leadingTokens), Helpers.GetTrivia(trailingTokens));

        public static T WithComments<T>(this T node, IEnumerable<SyntaxTrivia> leadingTrivia, IEnumerable<SyntaxTrivia> trailingTrivia, params SyntaxNodeOrToken[] trailingNodesOrTokens) where T : SyntaxNode
            => node
            .WithLeadingTrivia(FilterComments(leadingTrivia.Concat(node.GetLeadingTrivia()), addElasticMarker: false))
            .WithTrailingTrivia(FilterComments(node.GetTrailingTrivia().Concat(Helpers.GetTrivia(trailingNodesOrTokens).Concat(trailingTrivia)), addElasticMarker: false));

        public static SyntaxToken WithComments(this SyntaxToken token, IEnumerable<SyntaxTrivia> leadingTrivia, IEnumerable<SyntaxTrivia> trailingTrivia, params SyntaxNodeOrToken[] trailingNodesOrTokens)
            => token
                .WithCommentsBeforeLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(FilterComments(token.TrailingTrivia.Concat(Helpers.GetTrivia(trailingNodesOrTokens).Concat(trailingTrivia)), addElasticMarker: false));

        public static SyntaxToken WithCommentsBeforeLeadingTrivia(this SyntaxToken token, IEnumerable<SyntaxTrivia> trivia, params SyntaxNodeOrToken[] nodesOrTokens)
            => token
                .WithLeadingTrivia(FilterComments(trivia.Concat(Helpers.GetTrivia(nodesOrTokens)).Concat(token.LeadingTrivia), addElasticMarker: false));

        public static T KeepCommentsAndAddElasticMarkers<T>(this T node) where T : SyntaxNode
            => node
                .WithTrailingTrivia(FilterComments(node.GetTrailingTrivia(), addElasticMarker: true))
                .WithLeadingTrivia(FilterComments(node.GetLeadingTrivia(), addElasticMarker: true));

        public static SyntaxToken KeepCommentsAndAddElasticMarkers(this SyntaxToken token)
            => token
                .WithTrailingTrivia(FilterComments(token.TrailingTrivia, addElasticMarker: true))
                .WithLeadingTrivia(FilterComments(token.LeadingTrivia, addElasticMarker: true));

        private static IEnumerable<SyntaxTrivia> FilterComments(IEnumerable<SyntaxTrivia> trivia, bool addElasticMarker)
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

            if (addElasticMarker)
            {
                yield return SyntaxFactory.ElasticMarker;
            }
        }
    }
}
