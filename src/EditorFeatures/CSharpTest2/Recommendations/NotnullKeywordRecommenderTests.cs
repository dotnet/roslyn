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
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class NotNullKeywordRecommenderTests : RecommenderTests
    {
        protected override string KeywordText => "notnull";

        private readonly NotNullKeywordRecommender _recommender = new();

        public NotNullKeywordRecommenderTests()
        {
            this.RecommendKeywordsAsync = (position, context) => Task.FromResult(_recommender.RecommendKeywords(position, context, CancellationToken.None));
        }

        [Fact]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
        }

        [Fact]
        public async Task TestNotInGlobalUsingAlias()
        {
            await VerifyAbsenceAsync(
@"global using Goo = $$");
        }

        [Fact]
        public async Task TestNotAfterName_Type()
        {
            await VerifyAbsenceAsync(
@"class Test $$");
        }

        [Fact]
        public async Task TestNotAfterWhereClause_Type()
        {
            await VerifyAbsenceAsync(
@"class Test<T> where $$");
        }

        [Fact]
        public async Task TestNotAfterWhereClauseType_Type()
        {
            await VerifyAbsenceAsync(
@"class Test<T> where T $$");
        }

        [Fact]
        public async Task TestAfterWhereClauseColon_Type()
        {
            await VerifyKeywordAsync(
@"class Test<T> where T : $$");
        }

        [Fact]
        public async Task TestNotAfterTypeConstraint_Type()
        {
            await VerifyAbsenceAsync(
@"class Test<T> where T : I $$");
        }

        [Fact]
        public async Task TestAfterTypeConstraintComma_Type()
        {
            await VerifyKeywordAsync(
@"class Test<T> where T : I, $$");
        }

        [Fact]
        public async Task TestNotAfterName_Method()
        {
            await VerifyAbsenceAsync(
                """
                class Test {
                    void M $$
                """);
        }

        [Fact]
        public async Task TestNotAfterWhereClause_Method()
        {
            await VerifyAbsenceAsync(
                """
                class Test {
                    void M<T> where $$
                """);
        }

        [Fact]
        public async Task TestNotAfterWhereClauseType_Method()
        {
            await VerifyAbsenceAsync(
                """
                class Test {
                    void M<T> where T $$
                """);
        }

        [Fact]
        public async Task TestAfterWhereClauseColon_Method()
        {
            await VerifyKeywordAsync(
                """
                class Test {
                    void M<T> where T : $$
                """);
        }

        [Fact]
        public async Task TestNotAfterTypeConstraint_Method()
        {
            await VerifyAbsenceAsync(
                """
                class Test {
                    void M<T> where T : I $$
                """);
        }

        [Fact]
        public async Task TestAfterTypeConstraintComma_Method()
        {
            await VerifyKeywordAsync(
                """
                class Test {
                    void M<T> where T : I, $$
                """);
        }

        [Fact]
        public async Task TestNotAfterName_Delegate()
        {
            await VerifyAbsenceAsync(
@"delegate void D $$");
        }

        [Fact]
        public async Task TestNotAfterWhereClause_Delegate()
        {
            await VerifyAbsenceAsync(
@"delegate void D<T>() where $$");
        }

        [Fact]
        public async Task TestNotAfterWhereClauseType_Delegate()
        {
            await VerifyAbsenceAsync(
@"delegate void D<T>() where T $$");
        }

        [Fact]
        public async Task TestAfterWhereClauseColon_Delegate()
        {
            await VerifyKeywordAsync(
@"delegate void D<T>() where T : $$");
        }

        [Fact]
        public async Task TestNotAfterTypeConstraint_Delegate()
        {
            await VerifyAbsenceAsync(
@"delegate void D<T>() where T : I $$");
        }

        [Fact]
        public async Task TestAfterTypeConstraintComma_Delegate()
        {
            await VerifyKeywordAsync(
@"delegate void D<T>() where T : I, $$");
        }

        [Fact]
        public async Task TestNotAfterName_LocalFunction()
        {
            await VerifyAbsenceAsync(
                """
                class Test {
                    void N() {
                        void M $$
                """);
        }

        [Fact]
        public async Task TestNotAfterWhereClause_LocalFunction()
        {
            await VerifyAbsenceAsync(
                """
                class Test {
                    void N() {
                        void M<T> where $$
                """);
        }

        [Fact]
        public async Task TestNotAfterWhereClauseType_LocalFunction()
        {
            await VerifyAbsenceAsync(
                """
                class Test {
                    void N() {
                        void M<T> where T $$
                """);
        }

        [Fact]
        public async Task TestAfterWhereClauseColon_LocalFunction()
        {
            await VerifyKeywordAsync(
                """
                class Test {
                    void N() {
                        void M<T> where T : $$
                """);
        }

        [Fact]
        public async Task TestNotAfterTypeConstraint_LocalFunction()
        {
            await VerifyAbsenceAsync(
                """
                class Test {
                    void N() {
                        void M<T> where T : I $$
                """);
        }

        [Fact]
        public async Task TestAfterTypeConstraintComma_LocalFunction()
        {
            await VerifyKeywordAsync(
                """
                class Test {
                    void N() {
                        void M<T> where T : I, $$
                """);
        }
    }
}
