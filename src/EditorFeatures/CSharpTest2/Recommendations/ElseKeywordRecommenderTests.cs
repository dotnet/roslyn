// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ElseKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPreprocessor1()
        {
            VerifyAbsence(
@"class C {
#if $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPreprocessorFollowedBySkippedTokens()
        {
            VerifyKeyword(
@"#if GOO
#$$
dasd
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterHash()
        {
            VerifyKeyword(
@"#$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterHashAndSpace()
        {
            VerifyKeyword(
@"# $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIf()
        {
            VerifyAbsence(AddInsideMethod(
@"if (true)
$$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfStatement(string statement)
        {
            VerifyKeyword(AddInsideMethod(
$@"if (true)
    {statement}
$$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfStatement_BeforeElse(string statement)
        {
            VerifyKeyword(AddInsideMethod(
$@"if (true)
    {statement}
$$
else"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfNestedIfStatement(string statement)
        {
            VerifyKeyword(AddInsideMethod(
$@"if (true)
    if (true)
        {statement}
    $$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfNestedIfStatement_BeforeElse(string statement)
        {
            VerifyKeyword(AddInsideMethod(
$@"if (true)
    if (true)
        {statement}
    $$
    else"));
        }

        [WorkItem(25336, "https://github.com/dotnet/roslyn/issues/25336")]
        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfNestedIfElseStatement(string statement)
        {
            VerifyKeyword(AddInsideMethod(
$@"if (true)
    if (true)
        Console.WriteLine();
    else
        {statement}
$$"));
        }

        [WorkItem(25336, "https://github.com/dotnet/roslyn/issues/25336")]
        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfNestedIfElseStatement_BeforeElse(string statement)
        {
            VerifyKeyword(AddInsideMethod(
$@"if (true)
    if (true)
        Console.WriteLine();
    else
        {statement}
$$
else"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestNotAfterIfNestedIfElseElseStatement(string statement)
        {
            VerifyAbsence(AddInsideMethod(
$@"if (true)
    if (true)
        Console.WriteLine();
    else
        Console.WriteLine();
else
    {statement}
$$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestNotAfterIfStatementElse(string statement)
        {
            VerifyAbsence(AddInsideMethod(
$@"if (true)
    {statement}
else
    $$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestNotAfterIfElseStatement(string statement)
        {
            VerifyAbsence(AddInsideMethod(
$@"if (true)
    Console.WriteLine();
else
    {statement}
$$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfElseNestedIfStatement(string statement)
        {
            VerifyKeyword(AddInsideMethod(
$@"if (true)
    Console.WriteLine();
else
    if (true)
        {statement}
    $$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfElseNestedIfStatement_BeforeElse(string statement)
        {
            VerifyKeyword(AddInsideMethod(
$@"if (true)
    Console.WriteLine();
else
    if (true)
        {statement}
    $$
    else"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestNotAfterIfElseNestedIfElseStatement(string statement)
        {
            VerifyAbsence(AddInsideMethod(
$@"if (true)
    Console.WriteLine();
else
    if (true)
        Console.WriteLine();
    else
        {statement}
$$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterWhileIfWhileNestedIfElseStatement(string statement)
        {
            VerifyKeyword(AddInsideMethod(
$@"while (true)
    if (true)
        while (true)
            if (true)
                Console.WriteLine();
            else
                {statement}
    $$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterWhileIfWhileNestedIfElseStatement_BeforeElse(string statement)
        {
            VerifyKeyword(AddInsideMethod(
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

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestNotAfterWhileIfWhileNestedIfElseElseStatement(string statement)
        {
            VerifyAbsence(AddInsideMethod(
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

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
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
            VerifyAbsence(AddInsideMethod(
$@"if (true)
    {statement}
$$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
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
            VerifyAbsence(AddInsideMethod(
$@"if (true)
    if (true)
        {statement}
    $$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
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
            VerifyAbsence(AddInsideMethod(
$@"if (true)
    if (true)
        Console.WriteLine();
    else
        {statement}
$$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfNestedIfIncompleteStatementElseStatement(string statement)
        {
            VerifyKeyword(AddInsideMethod(
$@"if (true)
    if (true)
        Console // Incomplete, but that's fine. This is not the if statement we care about.
    else
        {statement}
$$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public async Task TestAfterIfNestedIfIncompleteStatementElseStatement_BeforeElse(string statement)
        {
            VerifyKeyword(AddInsideMethod(
$@"if (true)
    if (true)
        Console // Incomplete, but that's fine. This is not the if statement we care about.
    else
        {statement}
$$
else"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInsideStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"if (true)
    Console.WriteLine()$$; // Complete statement, but we're not at the end of it.
"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterSkippedToken()
        {
            VerifyAbsence(AddInsideMethod(
@"if (true)
    Console.WriteLine();,
$$"));
        }
    }
}
