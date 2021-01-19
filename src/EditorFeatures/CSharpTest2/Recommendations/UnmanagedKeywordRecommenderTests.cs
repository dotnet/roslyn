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
    public class UnmanagedKeywordRecommenderTests : RecommenderTests
    {
        private readonly UnmanagedKeywordRecommender _recommender = new UnmanagedKeywordRecommender();

        public UnmanagedKeywordRecommenderTests()
        {
            this.keywordText = "unmanaged";
            this.RecommendKeywords = (position, context) => _recommender.RecommendKeywords(position, context, CancellationToken.None);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterName_Type()
        {
            VerifyAbsence(
@"class Test $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereClause_Type()
        {
            VerifyAbsence(
@"class Test<T> where $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereClauseType_Type()
        {
            VerifyAbsence(
@"class Test<T> where T $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterWhereClauseColon_Type()
        {
            VerifyKeyword(
@"class Test<T> where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterTypeConstraint_Type()
        {
            VerifyAbsence(
@"class Test<T> where T : I $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeConstraintComma_Type()
        {
            VerifyKeyword(
@"class Test<T> where T : I, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterName_Method()
        {
            VerifyAbsence(
@"class Test {
    void M $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereClause_Method()
        {
            VerifyAbsence(
@"class Test {
    void M<T> where $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereClauseType_Method()
        {
            VerifyAbsence(
@"class Test {
    void M<T> where T $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterWhereClauseColon_Method()
        {
            VerifyKeyword(
@"class Test {
    void M<T> where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterTypeConstraint_Method()
        {
            VerifyAbsence(
@"class Test {
    void M<T> where T : I $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeConstraintComma_Method()
        {
            VerifyKeyword(
@"class Test {
    void M<T> where T : I, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterName_Delegate()
        {
            VerifyAbsence(
@"delegate void D $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereClause_Delegate()
        {
            VerifyAbsence(
@"delegate void D<T>() where $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereClauseType_Delegate()
        {
            VerifyAbsence(
@"delegate void D<T>() where T $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterWhereClauseColon_Delegate()
        {
            VerifyKeyword(
@"delegate void D<T>() where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterTypeConstraint_Delegate()
        {
            VerifyAbsence(
@"delegate void D<T>() where T : I $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeConstraintComma_Delegate()
        {
            VerifyKeyword(
@"delegate void D<T>() where T : I, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterName_LocalFunction()
        {
            VerifyAbsence(
@"class Test {
    void N() {
        void M $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereClause_LocalFunction()
        {
            VerifyAbsence(
@"class Test {
    void N() {
        void M<T> where $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereClauseType_LocalFunction()
        {
            VerifyAbsence(
@"class Test {
    void N() {
        void M<T> where T $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterWhereClauseColon_LocalFunction()
        {
            VerifyKeyword(
@"class Test {
    void N() {
        void M<T> where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterTypeConstraint_LocalFunction()
        {
            VerifyAbsence(
@"class Test {
    void N() {
        void M<T> where T : I $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeConstraintComma_LocalFunction()
        {
            VerifyKeyword(
@"class Test {
    void N() {
        void M<T> where T : I, $$");
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
