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
    public class NotNullKeywordRecommenderTests : RecommenderTests
    {
        private readonly NotNullKeywordRecommender _recommender = new NotNullKeywordRecommender();

        public NotNullKeywordRecommenderTests()
        {
            this.keywordText = "notnull";
            this.RecommendKeywords = (position, context) => _recommender.RecommendKeywords(position, context, CancellationToken.None);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterName_Type()
        {
            VerifyAbsence(
@"class Test $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterWhereClause_Type()
        {
            VerifyAbsence(
@"class Test<T> where $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterWhereClauseType_Type()
        {
            VerifyAbsence(
@"class Test<T> where T $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterWhereClauseColon_Type()
        {
            VerifyKeyword(
@"class Test<T> where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterTypeConstraint_Type()
        {
            VerifyAbsence(
@"class Test<T> where T : I $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterTypeConstraintComma_Type()
        {
            VerifyKeyword(
@"class Test<T> where T : I, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterName_Method()
        {
            VerifyAbsence(
@"class Test {
    void M $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterWhereClause_Method()
        {
            VerifyAbsence(
@"class Test {
    void M<T> where $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterWhereClauseType_Method()
        {
            VerifyAbsence(
@"class Test {
    void M<T> where T $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterWhereClauseColon_Method()
        {
            VerifyKeyword(
@"class Test {
    void M<T> where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterTypeConstraint_Method()
        {
            VerifyAbsence(
@"class Test {
    void M<T> where T : I $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterTypeConstraintComma_Method()
        {
            VerifyKeyword(
@"class Test {
    void M<T> where T : I, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterName_Delegate()
        {
            VerifyAbsence(
@"delegate void D $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterWhereClause_Delegate()
        {
            VerifyAbsence(
@"delegate void D<T>() where $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterWhereClauseType_Delegate()
        {
            VerifyAbsence(
@"delegate void D<T>() where T $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterWhereClauseColon_Delegate()
        {
            VerifyKeyword(
@"delegate void D<T>() where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterTypeConstraint_Delegate()
        {
            VerifyAbsence(
@"delegate void D<T>() where T : I $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterTypeConstraintComma_Delegate()
        {
            VerifyKeyword(
@"delegate void D<T>() where T : I, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterName_LocalFunction()
        {
            VerifyAbsence(
@"class Test {
    void N() {
        void M $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterWhereClause_LocalFunction()
        {
            VerifyAbsence(
@"class Test {
    void N() {
        void M<T> where $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterWhereClauseType_LocalFunction()
        {
            VerifyAbsence(
@"class Test {
    void N() {
        void M<T> where T $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterWhereClauseColon_LocalFunction()
        {
            VerifyKeyword(
@"class Test {
    void N() {
        void M<T> where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterTypeConstraint_LocalFunction()
        {
            VerifyAbsence(
@"class Test {
    void N() {
        void M<T> where T : I $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterTypeConstraintComma_LocalFunction()
        {
            VerifyKeyword(
@"class Test {
    void N() {
        void M<T> where T : I, $$");
        }
    }
}
