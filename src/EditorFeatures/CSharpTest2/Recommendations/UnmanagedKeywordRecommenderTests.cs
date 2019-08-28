// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class UnmanagedKeywordRecommenderTests : RecommenderTests
    {
        private readonly UnmanagedKeywordRecommender _recommender = new UnmanagedKeywordRecommender();

        public UnmanagedKeywordRecommenderTests()
        {
            this.keywordText = "unmanaged";
            this.RecommendKeywordsAsync = (position, context) => _recommender.RecommendKeywordsAsync(position, context, CancellationToken.None);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterName_Type()
        {
            await VerifyAbsenceAsync(
@"class Test $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereClause_Type()
        {
            await VerifyAbsenceAsync(
@"class Test<T> where $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereClauseType_Type()
        {
            await VerifyAbsenceAsync(
@"class Test<T> where T $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterWhereClauseColon_Type()
        {
            await VerifyKeywordAsync(
@"class Test<T> where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterTypeConstraint_Type()
        {
            await VerifyAbsenceAsync(
@"class Test<T> where T : I $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeConstraintComma_Type()
        {
            await VerifyKeywordAsync(
@"class Test<T> where T : I, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterName_Method()
        {
            await VerifyAbsenceAsync(
@"class Test {
    void M $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereClause_Method()
        {
            await VerifyAbsenceAsync(
@"class Test {
    void M<T> where $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereClauseType_Method()
        {
            await VerifyAbsenceAsync(
@"class Test {
    void M<T> where T $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterWhereClauseColon_Method()
        {
            await VerifyKeywordAsync(
@"class Test {
    void M<T> where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterTypeConstraint_Method()
        {
            await VerifyAbsenceAsync(
@"class Test {
    void M<T> where T : I $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeConstraintComma_Method()
        {
            await VerifyKeywordAsync(
@"class Test {
    void M<T> where T : I, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterName_Delegate()
        {
            await VerifyAbsenceAsync(
@"delegate void D $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereClause_Delegate()
        {
            await VerifyAbsenceAsync(
@"delegate void D<T>() where $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereClauseType_Delegate()
        {
            await VerifyAbsenceAsync(
@"delegate void D<T>() where T $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterWhereClauseColon_Delegate()
        {
            await VerifyKeywordAsync(
@"delegate void D<T>() where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterTypeConstraint_Delegate()
        {
            await VerifyAbsenceAsync(
@"delegate void D<T>() where T : I $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeConstraintComma_Delegate()
        {
            await VerifyKeywordAsync(
@"delegate void D<T>() where T : I, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterName_LocalFunction()
        {
            await VerifyAbsenceAsync(
@"class Test {
    void N() {
        void M $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereClause_LocalFunction()
        {
            await VerifyAbsenceAsync(
@"class Test {
    void N() {
        void M<T> where $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereClauseType_LocalFunction()
        {
            await VerifyAbsenceAsync(
@"class Test {
    void N() {
        void M<T> where T $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterWhereClauseColon_LocalFunction()
        {
            await VerifyKeywordAsync(
@"class Test {
    void N() {
        void M<T> where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterTypeConstraint_LocalFunction()
        {
            await VerifyAbsenceAsync(
@"class Test {
    void N() {
        void M<T> where T : I $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeConstraintComma_LocalFunction()
        {
            await VerifyKeywordAsync(
@"class Test {
    void N() {
        void M<T> where T : I, $$");
        }
    }
}
