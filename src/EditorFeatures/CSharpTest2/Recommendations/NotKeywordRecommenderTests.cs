// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class NotKeywordRecommenderTests : KeywordRecommenderTests
    {
        private const string InitializeObjectE = """
            object e = new object();
            """;

        [Fact]
        public async Task TestAfterIsKeyword()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is $$"));
        }

        [Fact]
        public async Task TestAfterNotKeyword()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is not $$"));
        }

        [Fact]
        public async Task TestAfterNotKeywordAndOpenParen()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is not ($$"));
        }

        [Fact]
        public async Task TestAfterAndKeyword_IntExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is 1 and $$"));
        }

        [Fact]
        public async Task TestAfterAndKeyword_StrExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is ""str"" and $$"));
        }

        [Fact]
        public async Task TestAfterAndKeyword_RelationalExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is <= 1 and $$"));
        }

        [Fact]
        public async Task TestAfterOpenParen()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is ($$"));
        }

        [Fact]
        public async Task TestAfterMultipleOpenParen()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is ((($$"));
        }

        [Fact]
        public async Task TestInMiddleofCompletePattern()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is $$ 1 or 2)"));
        }

        [Fact]
        public async Task TestInMiddleOfCompleteQualifiedPattern()
        {
            await VerifyKeywordAsync(
                """
                namespace N
                {
                    class C
                    {
                        const int P = 1;

                        void M()
                        {
                            if (e is $$ N.C.P or 2) { }
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInMiddleOfCompleteQualifiedPattern_List()
        {
            await VerifyKeywordAsync(
                """
                namespace N
                {
                    class C
                    {
                        const int P = 1;

                        void M()
                        {
                            if (e is $$ System.Collections.Generic.List<int> or 2) { }
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInMiddleofCompletePattern_MultipleParens()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is ((($$ 1 or 2))))"));
        }

        [Fact]
        public async Task TestInMiddleofCompletePattern_EmptyListPattern()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is ($$ []) and var x)"));
        }

        [Fact]
        public async Task TestAtBeginningOfSwitchExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
                """
                var result = e switch
                {
                    $$
                """));
        }

        [Fact]
        public async Task TestAtBeginningOfSwitchStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
                """
                switch (e)
                {
                    case $$
                """));
        }

        [Fact]
        public async Task TestInMiddleOfSwitchExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
                """
                var result = e switch
                {
                    1 => 2,
                    $$
                """));
        }

        [Fact]
        public async Task TestInMiddleOfSwitchStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
                """
                switch (e)
                {
                    case 1:
                    case $$
                """));
        }

        [Fact]
        public async Task TestAtBeginningOfSwitchExpression_AfterOpenParen()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
                """
                var result = e switch
                {
                    ($$
                """));
        }

        [Fact]
        public async Task TestInMiddleOfSwitchExpression_AfterOpenParen()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
                """
                var result = e switch
                {
                    1 => 2,
                    ($$
                """));
        }

        [Fact]
        public async Task TestInMiddleOfSwitchExpression_ComplexCase()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
                """
                var result = e switch
                {
                    1 and ($$
                """));
        }

        [Fact]
        public async Task TestAtBeginningOfSwitchStatement_AfterOpenParen()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
                """
                switch (e)
                {
                    case ($$
                """));
        }

        [Fact]
        public async Task TestAtBeginningOfSwitchStatement_AfterOpenParen_CompleteStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
                """
                switch (e)
                {
                    case ($$ 1)
                """));
        }

        [Fact]
        public async Task TestInsideSubpattern()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    public int P { get; }

                    void M(C test)
                    {
                        if (test is { P: $$
                """);
        }

        [Fact]
        public async Task TestInsideSubpattern_ExtendedProperty()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    public C P { get; }
                    public int P2 { get; }

                    void M(C test)
                    {
                        if (test is { P.P2: $$
                """);
        }

        [Fact]
        public async Task TestInsideSubpattern_AfterOpenParen()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    public int P { get; }

                    void M(C test)
                    {
                        if (test is { P: ($$
                """);
        }

        [Fact]
        public async Task TestInsideSubpattern_AfterOpenParen_Complex()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    public int P { get; }

                    void M(C test)
                    {
                        if (test is { P: (1 or $$
                """);
        }

        [Fact]
        public async Task TestMissingAfterConstant()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is 1 $$"));
        }

        [Fact]
        public async Task TestMissingAfterMultipleConstants()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is 1 or 2 $$"));
        }

        [Fact]
        public async Task TestAfterType()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is int $$"));
        }

        [Fact]
        public async Task TestAfterRelationalOperator()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is >= 0 $$"));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/61184")]
        [InlineData("and")]
        [InlineData("or")]
        public async Task TestAfterIdentifierPatternKeyword(string precedingKeyword)
        {
            await VerifyKeywordAsync(InitializeObjectE +
$@"if (e is Test.TestValue {precedingKeyword} $$)

enum Test {{ TestValue }}");
        }
    }
}
