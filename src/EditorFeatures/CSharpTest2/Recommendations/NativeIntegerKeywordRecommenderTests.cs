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
    public class NativeIntegerKeywordRecommenderTests : RecommenderTests
    {
        private AbstractNativeIntegerKeywordRecommender _recommender;

        public NativeIntegerKeywordRecommenderTests()
        {
            RecommendKeywordsAsync = (position, context) => _recommender.RecommendKeywordsAsync(position, context, CancellationToken.None);
        }

        private async Task VerifyKeywordAsync(string text)
        {
            _recommender = new NintKeywordRecommender();
            keywordText = "nint";
            await base.VerifyKeywordAsync(text);

            _recommender = new NuintKeywordRecommender();
            keywordText = "nuint";
            await base.VerifyKeywordAsync(text);
        }

        private async Task VerifyAbsenceAsync(string text)
        {
            _recommender = new NintKeywordRecommender();
            keywordText = "nint";
            await base.VerifyAbsenceAsync(text);

            _recommender = new NintKeywordRecommender();
            keywordText = "nuint";
            await base.VerifyAbsenceAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInLocalDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInParameterList()
        {
            await VerifyKeywordAsync(
@"class C
{
    void F($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConst()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"const $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInCastType()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUsingAliasFirst()
        {
            // Ideally, we should not recommend keywords as first token in target.
            await VerifyKeywordAsync(
@"using A = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUsingAliasLater()
        {
            await VerifyKeywordAsync(
@"using A = List<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInNameOf()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"_ = nameof($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInSizeOf()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"_ = sizeof($$"));
        }
    }
}
