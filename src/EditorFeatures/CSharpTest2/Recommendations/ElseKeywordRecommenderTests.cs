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
        public void TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInPreprocessor1()
        {
            VerifyAbsence(
@"class C {
#if $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInPreprocessorFollowedBySkippedTokens()
        {
            VerifyKeyword(
@"#if GOO
#$$
dasd
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterHash()
        {
            VerifyKeyword(
@"#$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterHashAndSpace()
        {
            VerifyKeyword(
@"# $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterIf()
        {
            VerifyAbsence(AddInsideMethod(
@"if (true)
$$"));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("Console.WriteLine();")]
        [InlineData("{ }")]
        [InlineData("while (true) { }")]
        public void TestAfterIfStatement(string statement)
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
        public void TestAfterIfStatement_BeforeElse(string statement)
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
        public void TestAfterIfNestedIfStatement(string statement)
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
        public void TestAfterIfNestedIfStatement_BeforeElse(string statement)
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
        public void TestAfterIfNestedIfElseStatement(string statement)
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
        public void TestAfterIfNestedIfElseStatement_BeforeElse(string statement)
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
        public void TestNotAfterIfNestedIfElseElseStatement(string statement)
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
        public void TestNotAfterIfStatementElse(string statement)
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
        public void TestNotAfterIfElseStatement(string statement)
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
        public void TestAfterIfElseNestedIfStatement(string statement)
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
        public void TestAfterIfElseNestedIfStatement_BeforeElse(string statement)
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
        public void TestNotAfterIfElseNestedIfElseStatement(string statement)
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
        public void TestAfterWhileIfWhileNestedIfElseStatement(string statement)
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
        public void TestAfterWhileIfWhileNestedIfElseStatement_BeforeElse(string statement)
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
        public void TestNotAfterWhileIfWhileNestedIfElseElseStatement(string statement)
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
        public void TestNotAfterIfIncompleteStatement(string statement)
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
        public void TestNotAfterIfNestedIfIncompleteStatement(string statement)
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
        public void TestNotAfterIfNestedIfElseIncompleteStatement(string statement)
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
        public void TestAfterIfNestedIfIncompleteStatementElseStatement(string statement)
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
        public void TestAfterIfNestedIfIncompleteStatementElseStatement_BeforeElse(string statement)
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
        public void TestNotInsideStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"if (true)
    Console.WriteLine()$$; // Complete statement, but we're not at the end of it.
"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterSkippedToken()
        {
            VerifyAbsence(AddInsideMethod(
@"if (true)
    Console.WriteLine();,
$$"));
        }
    }
}
