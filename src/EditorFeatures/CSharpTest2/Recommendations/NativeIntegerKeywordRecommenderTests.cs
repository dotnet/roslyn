// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            RecommendKeywords = (position, context) => _recommender.RecommendKeywords(position, context, CancellationToken.None);
        }

        private void VerifyKeyword(string text)
        {
            _recommender = new NintKeywordRecommender();
            keywordText = "nint";
            base.VerifyKeyword(text);

            _recommender = new NuintKeywordRecommender();
            keywordText = "nuint";
            base.VerifyKeyword(text);
        }

        private void VerifyAbsence(string text)
        {
            _recommender = new NintKeywordRecommender();
            keywordText = "nint";
            base.VerifyAbsence(text);

            _recommender = new NintKeywordRecommender();
            keywordText = "nuint";
            base.VerifyAbsence(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInLocalDeclaration()
        {
            VerifyKeyword(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInParameterList()
        {
            VerifyKeyword(
@"class C
{
    void F($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInLambdaParameterListFirst()
        {
            VerifyKeyword(AddInsideMethod(
@"F(($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInLambdaParameterListLater()
        {
            VerifyKeyword(AddInsideMethod(
@"F((int x, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterConst()
        {
            VerifyKeyword(AddInsideMethod(
@"const $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInFixedStatement()
        {
            VerifyKeyword(
@"fixed ($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInRef()
        {
            VerifyKeyword(AddInsideMethod(
@"ref $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInMemberType()
        {
            VerifyKeyword(
@"class C
{
    private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInOperatorType()
        {
            VerifyKeyword(
@"class C
{
    static implicit operator $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInEnumUnderlyingType()
        {
            VerifyAbsence(
@"enum E : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInTypeParameterConstraint()
        {
            // Ideally, keywords should not be recommended for constraint types.
            VerifyKeyword(
@"class C<T> where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInExpression()
        {
            VerifyKeyword(AddInsideMethod(
@"var v = 1 + $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInDefault()
        {
            VerifyKeyword(AddInsideMethod(
@"_ = default($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInCastType()
        {
            VerifyKeyword(AddInsideMethod(
@"var v = (($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInNew()
        {
            VerifyKeyword(AddInsideMethod(
@"_ = new $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterAs()
        {
            VerifyKeyword(AddInsideMethod(
@"object x = null;
var y = x as $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIs()
        {
            VerifyKeyword(AddInsideMethod(
@"object x = null;
if (x is $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterStackAlloc()
        {
            VerifyKeyword(
@"class C
{
    nint* p = stackalloc $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUsingAliasFirst()
        {
            // Ideally, keywords should not be recommended as first token in target.
            VerifyKeyword(
@"using A = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUsingAliasLater()
        {
            VerifyKeyword(
@"using A = List<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInNameOf()
        {
            VerifyKeyword(AddInsideMethod(
@"_ = nameof($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInSizeOf()
        {
            VerifyKeyword(AddInsideMethod(
@"_ = sizeof($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInCRef()
        {
            VerifyAbsence(AddInsideMethod(
@"/// <see cref=""$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInTupleWithinType()
        {
            VerifyKeyword(@"
class Program
{
    ($$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInTupleWithinMember()
        {
            VerifyKeyword(@"
class Program
{
    void Method()
    {
        ($$
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestPatternInSwitch()
        {
            VerifyKeyword(AddInsideMethod(
@"switch(o)
{
    case $$
}"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInFunctionPointerType()
        {
            VerifyKeyword(@"
class C
{
    delegate*<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInFunctionPointerTypeAfterComma()
        {
            VerifyKeyword(@"
class C
{
    delegate*<int, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInFunctionPointerTypeAfterModifier()
        {
            VerifyKeyword(@"
class C
{
    delegate*<ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterDelegateAsterisk()
        {
            VerifyAbsence(@"
class C
{
    delegate*$$");
        }
    }
}
