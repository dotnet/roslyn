// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
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
                        var prop = recommenderType.GetProperty("KeywordKind", BindingFlags.NonPublic | BindingFlags.Instance);
                        var kind = (SyntaxKind)prop.GetValue(recommender, null);

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

            AbstractSyntacticSingleKeywordRecommender recommender;
            s_recommenderMap.TryGetValue(kind, out recommender);
            Assert.NotNull(recommender);

            this.RecommendKeywordsAsync = recommender.RecommendKeywordsAsync_Test;
        }
    }
}
