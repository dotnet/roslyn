// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class KeywordRecommenderTests : RecommenderTests
    {
        private static readonly Dictionary<SyntaxKind, AbstractSyntacticSingleKeywordRecommender> s_recommenderMap =
            new Dictionary<SyntaxKind, AbstractSyntacticSingleKeywordRecommender>(SyntaxFacts.EqualityComparer);

        static KeywordRecommenderTests()
        {
            foreach (var recommenderType in typeof(AbstractSyntacticSingleKeywordRecommender).Assembly.GetTypes())
            {
                if (recommenderType.IsSubclassOf(typeof(AbstractSyntacticSingleKeywordRecommender)))
                {
                    try
                    {
                        var recommender = Activator.CreateInstance(recommenderType);
                        var field = recommenderType.GetField(nameof(AbstractSyntacticSingleKeywordRecommender.KeywordKind), BindingFlags.Public | BindingFlags.Instance);
                        var kind = (SyntaxKind)field.GetValue(recommender);

                        s_recommenderMap.Add(kind, (AbstractSyntacticSingleKeywordRecommender)recommender);
                    }
                    catch
                    {
                    }
                }
            }
        }

        protected KeywordRecommenderTests()
        {
            var name = this.GetType().Name;
            var kindName = name.Substring(0, name.Length - "RecommenderTests".Length);

            var field = typeof(SyntaxKind).GetField(kindName);
            var kind = (SyntaxKind)field.GetValue(null);
            this.keywordText = SyntaxFacts.GetText(kind);

            s_recommenderMap.TryGetValue(kind, out var recommender);
            Assert.NotNull(recommender);

            this.RecommendKeywordsAsync = (position, context) => Task.FromResult(recommender.GetTestAccessor().RecommendKeywords(position, context));
        }
    }
}
