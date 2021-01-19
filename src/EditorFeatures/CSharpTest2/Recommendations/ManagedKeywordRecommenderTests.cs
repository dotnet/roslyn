// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ManagedKeywordRecommenderTests : RecommenderTests
    {
        private readonly ManagedKeywordRecommender _recommender = new ManagedKeywordRecommender();

        public ManagedKeywordRecommenderTests()
        {
            this.keywordText = "managed";
            this.RecommendKeywords = (position, context) => _recommender.RecommendKeywords(position, context, CancellationToken.None);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFunctionPointerDeclaration()
        {
            VerifyKeyword(
@"class Test {
    unsafe void N() {
        delegate* $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFunctionPointerDeclarationTouchingAsterisk()
        {
            VerifyKeyword(
@"class Test {
    unsafe void N() {
        delegate*$$");
        }
    }
}
