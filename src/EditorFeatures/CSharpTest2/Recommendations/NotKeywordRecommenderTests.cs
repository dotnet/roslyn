// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class NotKeywordRecommenderTests : KeywordRecommenderTests
{
    private const string InitializeObjectE = """
        object e = new object();
        """;

    [Fact]
    public Task TestAfterIsKeyword()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is $$"));

    [Fact]
    public Task TestAfterNotKeyword()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is not $$"));

    [Fact]
    public Task TestAfterNotKeywordAndOpenParen()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is not ($$"));

    [Fact]
    public Task TestAfterAndKeyword_IntExpression()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is 1 and $$"));

    [Fact]
    public Task TestAfterAndKeyword_StrExpression()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is ""str"" and $$"));

    [Fact]
    public Task TestAfterAndKeyword_RelationalExpression()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is <= 1 and $$"));

    [Fact]
    public Task TestAfterOpenParen()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is ($$"));

    [Fact]
    public Task TestAfterMultipleOpenParen()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is ((($$"));

    [Fact]
    public Task TestInMiddleofCompletePattern()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is $$ 1 or 2)"));

    [Fact]
    public Task TestInMiddleOfCompleteQualifiedPattern()
        => VerifyKeywordAsync(
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

    [Fact]
    public Task TestInMiddleOfCompleteQualifiedPattern_List()
        => VerifyKeywordAsync(
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

    [Fact]
    public Task TestInMiddleofCompletePattern_MultipleParens()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is ((($$ 1 or 2))))"));

    [Fact]
    public Task TestInMiddleofCompletePattern_EmptyListPattern()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is ($$ []) and var x)"));

    [Fact]
    public Task TestAtBeginningOfSwitchExpression()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
            """
            var result = e switch
            {
                $$
            """));

    [Fact]
    public Task TestAtBeginningOfSwitchStatement()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
            """
            switch (e)
            {
                case $$
            """));

    [Fact]
    public Task TestInMiddleOfSwitchExpression()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
            """
            var result = e switch
            {
                1 => 2,
                $$
            """));

    [Fact]
    public Task TestInMiddleOfSwitchStatement()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
            """
            switch (e)
            {
                case 1:
                case $$
            """));

    [Fact]
    public Task TestAtBeginningOfSwitchExpression_AfterOpenParen()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
            """
            var result = e switch
            {
                ($$
            """));

    [Fact]
    public Task TestInMiddleOfSwitchExpression_AfterOpenParen()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
            """
            var result = e switch
            {
                1 => 2,
                ($$
            """));

    [Fact]
    public Task TestInMiddleOfSwitchExpression_ComplexCase()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
            """
            var result = e switch
            {
                1 and ($$
            """));

    [Fact]
    public Task TestAtBeginningOfSwitchStatement_AfterOpenParen()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
            """
            switch (e)
            {
                case ($$
            """));

    [Fact]
    public Task TestAtBeginningOfSwitchStatement_AfterOpenParen_CompleteStatement()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
            """
            switch (e)
            {
                case ($$ 1)
            """));

    [Fact]
    public Task TestInsideSubpattern()
        => VerifyKeywordAsync(
            """
            class C
            {
                public int P { get; }

                void M(C test)
                {
                    if (test is { P: $$
            """);

    [Fact]
    public Task TestInsideSubpattern_ExtendedProperty()
        => VerifyKeywordAsync(
            """
            class C
            {
                public C P { get; }
                public int P2 { get; }

                void M(C test)
                {
                    if (test is { P.P2: $$
            """);

    [Fact]
    public Task TestInsideSubpattern_AfterOpenParen()
        => VerifyKeywordAsync(
            """
            class C
            {
                public int P { get; }

                void M(C test)
                {
                    if (test is { P: ($$
            """);

    [Fact]
    public Task TestInsideSubpattern_AfterOpenParen_Complex()
        => VerifyKeywordAsync(
            """
            class C
            {
                public int P { get; }

                void M(C test)
                {
                    if (test is { P: (1 or $$
            """);

    [Fact]
    public Task TestMissingAfterConstant()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is 1 $$"));

    [Fact]
    public Task TestMissingAfterMultipleConstants()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is 1 or 2 $$"));

    [Fact]
    public Task TestAfterType()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is int $$"));

    [Fact]
    public Task TestAfterRelationalOperator()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is >= 0 $$"));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/61184")]
    [InlineData("and")]
    [InlineData("or")]
    public Task TestAfterIdentifierPatternKeyword(string precedingKeyword)
        => VerifyKeywordAsync(InitializeObjectE +
            $$"""
            if (e is Test.TestValue {{precedingKeyword}} $$)

            enum Test { TestValue }
            """);
}
