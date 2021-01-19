// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class NotKeywordRecommenderTests : KeywordRecommenderTests
    {
        private const string InitializeObjectE = @"object e = new object();
";

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIsKeyword()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"if (e is $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNotKeyword()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"if (e is not $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNotKeywordAndOpenParen()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"if (e is not ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterAndKeyword_IntExpression()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"if (e is 1 and $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterAndKeyword_StrExpression()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"if (e is ""str"" and $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterAndKeyword_RelationalExpression()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"if (e is <= 1 and $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterOpenParen()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"if (e is ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterMultipleOpenParen()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"if (e is ((($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInMiddleofCompletePattern()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"if (e is $$ 1 or 2)"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInMiddleOfCompleteQualifiedPattern()
        {
            VerifyKeyword(
@"namespace N
{
    class C
    {
        const int P = 1;

        void M()
        {
            if (e is $$ N.C.P or 2) { }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInMiddleOfCompleteQualifiedPattern_List()
        {
            VerifyKeyword(
@"namespace N
{
    class C
    {
        const int P = 1;

        void M()
        {
            if (e is $$ System.Collections.Generic.List<int> or 2) { }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInMiddleofCompletePattern_MultipleParens()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"if (e is ((($$ 1 or 2))))"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtBeginningOfSwitchExpression()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"var result = e switch
{
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtBeginningOfSwitchStatement()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"switch (e)
{
    case $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInMiddleOfSwitchExpression()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"var result = e switch
{
    1 => 2,
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInMiddleOfSwitchStatement()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"switch (e)
{
    case 1:
    case $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtBeginningOfSwitchExpression_AfterOpenParen()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"var result = e switch
{
    ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInMiddleOfSwitchExpression_AfterOpenParen()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"var result = e switch
{
    1 => 2,
    ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInMiddleOfSwitchExpression_ComplexCase()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"var result = e switch
{
    1 and ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtBeginningOfSwitchStatement_AfterOpenParen()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"switch (e)
{
    case ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtBeginningOfSwitchStatement_AfterOpenParen_CompleteStatement()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"switch (e)
{
    case ($$ 1)"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideSubpattern()
        {
            VerifyKeyword(
@"class C
{
    public int P { get; }

    void M(C test)
    {
        if (test is { P: $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideSubpattern_AfterOpenParen()
        {
            VerifyKeyword(
@"class C
{
    public int P { get; }

    void M(C test)
    {
        if (test is { P: ($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideSubpattern_AfterOpenParen_Complex()
        {
            VerifyKeyword(
@"class C
{
    public int P { get; }

    void M(C test)
    {
        if (test is { P: (1 or $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAfterConstant()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"if (e is 1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAfterMultipleConstants()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"if (e is 1 or 2 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterType()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"if (e is int $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterRelationalOperator()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"if (e is >= 0 $$"));
        }
    }
}
