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
    public class ElseKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                int i = 0;
                $$
                """);
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
        public async Task TestNotInPreprocessor1()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                #if $$
                """);
        }

        [Fact]
        public async Task TestInPreprocessorFollowedBySkippedTokens()
        {
            await VerifyKeywordAsync(
                """
                #if GOO
                #$$
                dasd
                """);
        }

        [Fact]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestAfterHash()
        {
            await VerifyKeywordAsync(
@"#$$");
        }

        [Fact]
        public async Task TestAfterHashAndSpace()
        {
            await VerifyKeywordAsync(
@"# $$");
        }

        [Fact]
        public async Task TestNotAfterIf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                if (true)
                $$
                """));
        }

        [Theory]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfStatement(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    {statement}
$$"));
        }

        [Theory]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfStatement_BeforeElse(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    {statement}
$$
else"));
        }

        [Theory]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfNestedIfStatement(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    if (true)
        {statement}
    $$"));
        }

        [Theory]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfNestedIfStatement_BeforeElse(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    if (true)
        {statement}
    $$
    else"));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/25336")]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfNestedIfElseStatement(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    if (true)
        Console.WriteLine();
    else
        {statement}
$$"));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/25336")]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfNestedIfElseStatement_BeforeElse(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    if (true)
        Console.WriteLine();
    else
        {statement}
$$
else"));
        }

        [Theory]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestNotAfterIfNestedIfElseElseStatement(string statement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
$@"if (true)
    if (true)
        Console.WriteLine();
    else
        Console.WriteLine();
else
    {statement}
$$"));
        }

        [Theory]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestNotAfterIfStatementElse(string statement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
$@"if (true)
    {statement}
else
    $$"));
        }

        [Theory]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestNotAfterIfElseStatement(string statement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
$@"if (true)
    Console.WriteLine();
else
    {statement}
$$"));
        }

        [Theory]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfElseNestedIfStatement(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    Console.WriteLine();
else
    if (true)
        {statement}
    $$"));
        }

        [Theory]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfElseNestedIfStatement_BeforeElse(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    Console.WriteLine();
else
    if (true)
        {statement}
    $$
    else"));
        }

        [Theory]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestNotAfterIfElseNestedIfElseStatement(string statement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
$@"if (true)
    Console.WriteLine();
else
    if (true)
        Console.WriteLine();
    else
        {statement}
$$"));
        }

        [Theory]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterWhileIfWhileNestedIfElseStatement(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"while (true)
    if (true)
        while (true)
            if (true)
                Console.WriteLine();
            else
                {statement}
    $$"));
        }

        [Theory]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterWhileIfWhileNestedIfElseStatement_BeforeElse(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"while (true)
    if (true)
        while (true)
            if (true)
                Console.WriteLine();
            else
                {statement}
    $$
    else"));
        }

        [Theory]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestNotAfterWhileIfWhileNestedIfElseElseStatement(string statement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
$@"while (true)
    if (true)
        while (true)
            if (true)
                Console.WriteLine();
            else
                Console.WriteLine();
    else
        {statement}
$$"));
        }

        [Theory]
        [InlineData("Console")]
        [InlineData("Console.")]
        [InlineData("Console.WriteLine(")]
        [InlineData("Console.WriteLine()")]
        [InlineData("{")]
        [InlineData("{ Console.WriteLine();")]
        [InlineData("while")]
        [InlineData("while (true)")]
        [InlineData("while (true) {")]
        [InlineData("while (true) { { }")]
        [InlineData("for (int i = 0;")]
        public async Task TestNotAfterIfIncompleteStatement(string statement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
$@"if (true)
    {statement}
$$"));
        }

        [Theory]
        [InlineData("Console")]
        [InlineData("Console.")]
        [InlineData("Console.WriteLine(")]
        [InlineData("Console.WriteLine()")]
        [InlineData("{")]
        [InlineData("{ Console.WriteLine();")]
        [InlineData("while")]
        [InlineData("while (true)")]
        [InlineData("while (true) {")]
        [InlineData("while (true) { { }")]
        [InlineData("for (int i = 0;")]
        public async Task TestNotAfterIfNestedIfIncompleteStatement(string statement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
$@"if (true)
    if (true)
        {statement}
    $$"));
        }

        [Theory]
        [InlineData("Console")]
        [InlineData("Console.")]
        [InlineData("Console.WriteLine(")]
        [InlineData("Console.WriteLine()")]
        [InlineData("{")]
        [InlineData("{ Console.WriteLine();")]
        [InlineData("while")]
        [InlineData("while (true)")]
        [InlineData("while (true) {")]
        [InlineData("while (true) { { }")]
        [InlineData("for (int i = 0;")]
        public async Task TestNotAfterIfNestedIfElseIncompleteStatement(string statement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
$@"if (true)
    if (true)
        Console.WriteLine();
    else
        {statement}
$$"));
        }

        [Theory]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfNestedIfIncompleteStatementElseStatement(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    if (true)
        Console // Incomplete, but that's fine. This is not the if statement we care about.
    else
        {statement}
$$"));
        }

        [Theory]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfNestedIfIncompleteStatementElseStatement_BeforeElse(string statement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
$@"if (true)
    if (true)
        Console // Incomplete, but that's fine. This is not the if statement we care about.
    else
        {statement}
$$
else"));
        }

        [Fact]
        public async Task TestNotInsideStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                if (true)
                    Console.WriteLine()$$; // Complete statement, but we're not at the end of it.
                """));
        }

        [Fact]
        public async Task TestNotAfterSkippedToken()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                if (true)
                    Console.WriteLine();,
                $$
                """));
        }
    }
}
