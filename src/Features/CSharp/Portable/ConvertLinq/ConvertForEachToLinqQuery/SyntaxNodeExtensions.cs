// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery
{
    internal static class SyntaxNodeExtensions
    {
        public static T AddBeforeLeadingTrivia<T>(this T node, IEnumerable<SyntaxTrivia> trivia, params SyntaxNodeOrToken[] nodesOrTokens) where T : SyntaxNode
            => node.WithLeadingTrivia(trivia.Concat(GetCommentTrivia(nodesOrTokens)).Concat(node.GetLeadingTrivia()));

        public static T AddBeforeLeadingTrivia<T>(this T node, params SyntaxNodeOrToken[] nodesOrTokens) where T : SyntaxNode
            => node.AddBeforeLeadingTrivia(Enumerable.Empty<SyntaxTrivia>(), nodesOrTokens);

        public static T AddAfterTrailingTrivia<T>(this T node, IEnumerable<SyntaxTrivia> trivia, params SyntaxNodeOrToken[] nodesOrTokens) where T : SyntaxNode
            => node.WithLeadingTrivia(node.GetTrailingTrivia().Concat(GetCommentTrivia(nodesOrTokens)).Concat(trivia));

        public static T AddAfterTrailingTrivia<T>(this T node, SyntaxNodeOrToken[] nodesOrTokens) where T : SyntaxNode
            => node.AddAfterTrailingTrivia(Enumerable.Empty<SyntaxTrivia>(), nodesOrTokens);


        public static IEnumerable<SyntaxTrivia> GetLeadingComments(this SyntaxNodeOrToken nodeOrToken)
            => nodeOrToken.GetLeadingTrivia().Where(trivia => trivia.IsSingleOrMultiLineComment() || trivia.IsDocComment());

        public static IEnumerable<SyntaxTrivia> GetTrailingComments(this SyntaxNodeOrToken nodeOrToken)
            => nodeOrToken.GetTrailingTrivia().Where(trivia => trivia.IsSingleOrMultiLineComment() || trivia.IsDocComment());

        private static ImmutableArray<SyntaxTrivia> GetCommentTrivia(params SyntaxNodeOrToken[] nodesOrTokens)
        {
            var list = new List<SyntaxTrivia>();
            foreach (var nodeOrToken in nodesOrTokens)
            {
                list.AddRange(nodeOrToken.GetLeadingComments());
                list.AddRange(nodeOrToken.GetTrailingComments());
            }

            return list.ToImmutableArray();
        }
    }
}
