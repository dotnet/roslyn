// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ElseKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotAtRoot_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public Task TestNotAfterClass_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalStatement_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalVariableDeclaration_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            int i = 0;
            $$
            """);

    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
@"using Goo = $$");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
@"global using Goo = $$");

    [Fact]
    public Task TestNotInPreprocessor1()
        => VerifyAbsenceAsync(
            """
            class C {
            #if $$
            """);

    [Fact]
    public Task TestInPreprocessorFollowedBySkippedTokens()
        => VerifyKeywordAsync(
            """
            #if GOO
            #$$
            dasd
            """);

    [Fact]
    public Task TestNotInEmptyStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestAfterHash()
        => VerifyKeywordAsync(
@"#$$");

    [Fact]
    public Task TestAfterHashAndSpace()
        => VerifyKeywordAsync(
@"# $$");

    [Fact]
    public Task TestNotAfterIf()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            if (true)
            $$
            """));

    [Theory]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestAfterIfStatement(string statement)
        => VerifyKeywordAsync(AddInsideMethod(
            $"""
            if (true)
                {statement}
            $$
            """));

    [Theory]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestAfterIfStatement_BeforeElse(string statement)
        => VerifyKeywordAsync(AddInsideMethod(
            $"""
            if (true)
                {statement}
            $$
            else
            """));

    [Theory]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestAfterIfNestedIfStatement(string statement)
        => VerifyKeywordAsync(AddInsideMethod(
            $"""
            if (true)
            if (true)
                {statement}
            $$
            """));

    [Theory]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestAfterIfNestedIfStatement_BeforeElse(string statement)
        => VerifyKeywordAsync(AddInsideMethod(
            $"""
            if (true)
            if (true)
                {statement}
            $$
            else
            """));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/25336")]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestAfterIfNestedIfElseStatement(string statement)
        => VerifyKeywordAsync(AddInsideMethod(
            $"""
            if (true)
                if (true)
                    Console.WriteLine();
                else
                    {statement}
            $$
            """));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/25336")]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestAfterIfNestedIfElseStatement_BeforeElse(string statement)
        => VerifyKeywordAsync(AddInsideMethod(
            $"""
            if (true)
                if (true)
                    Console.WriteLine();
                else
                    {statement}
            $$
            else
            """));

    [Theory]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestNotAfterIfNestedIfElseElseStatement(string statement)
        => VerifyAbsenceAsync(AddInsideMethod(
            $"""
            if (true)
                if (true)
                    Console.WriteLine();
                else
                    Console.WriteLine();
            else
                {statement}
            $$
            """));

    [Theory]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestNotAfterIfStatementElse(string statement)
        => VerifyAbsenceAsync(AddInsideMethod(
            $"""
            if (true)
                {statement}
            else
                $$
            """));

    [Theory]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestNotAfterIfElseStatement(string statement)
        => VerifyAbsenceAsync(AddInsideMethod(
            $"""
            if (true)
                Console.WriteLine();
            else
                {statement}
            $$
            """));

    [Theory]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestAfterIfElseNestedIfStatement(string statement)
        => VerifyKeywordAsync(AddInsideMethod(
            $"""
            if (true)
                Console.WriteLine();
            else
                if (true)
                    {statement}
                $$
            """));

    [Theory]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestAfterIfElseNestedIfStatement_BeforeElse(string statement)
        => VerifyKeywordAsync(AddInsideMethod(
            $"""
            if (true)
                Console.WriteLine();
            else
                if (true)
                    {statement}
                $$
                else
            """));

    [Theory]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestNotAfterIfElseNestedIfElseStatement(string statement)
        => VerifyAbsenceAsync(AddInsideMethod(
            $"""
            if (true)
                Console.WriteLine();
            else
                if (true)
                    Console.WriteLine();
                else
                    {statement}
            $$
            """));

    [Theory]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestAfterWhileIfWhileNestedIfElseStatement(string statement)
        => VerifyKeywordAsync(AddInsideMethod(
            $"""
            while (true)
            if (true)
                while (true)
                    if (true)
                        Console.WriteLine();
                    else
                        {statement}
            $$
            """));

    [Theory]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestAfterWhileIfWhileNestedIfElseStatement_BeforeElse(string statement)
        => VerifyKeywordAsync(AddInsideMethod(
            $"""
            while (true)
            if (true)
                while (true)
                    if (true)
                        Console.WriteLine();
                    else
                        {statement}
            $$
            else
            """));

    [Theory]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestNotAfterWhileIfWhileNestedIfElseElseStatement(string statement)
        => VerifyAbsenceAsync(AddInsideMethod(
            $"""
            while (true)
                if (true)
                    while (true)
                        if (true)
                            Console.WriteLine();
                        else
                            Console.WriteLine();
                else
                    {statement}
            $$
            """));

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
    public Task TestNotAfterIfIncompleteStatement(string statement)
        => VerifyAbsenceAsync(AddInsideMethod(
            $"""
            if (true)
                {statement}
            $$
            """));

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
    public Task TestNotAfterIfNestedIfIncompleteStatement(string statement)
        => VerifyAbsenceAsync(AddInsideMethod(
            $"""
            if (true)
            if (true)
                {statement}
            $$
            """));

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
    public Task TestNotAfterIfNestedIfElseIncompleteStatement(string statement)
        => VerifyAbsenceAsync(AddInsideMethod(
            $"""
            if (true)
                if (true)
                    Console.WriteLine();
                else
                    {statement}
            $$
            """));

    [Theory]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestAfterIfNestedIfIncompleteStatementElseStatement(string statement)
        => VerifyKeywordAsync(AddInsideMethod(
            $"""
            if (true)
                if (true)
                    Console // Incomplete, but that's fine. This is not the if statement we care about.
                else
                    {statement}
            $$
            """));

    [Theory]
    [InlineData("Console.WriteLine();")]
    [InlineData("{ }")]
    [InlineData("while (true) { }")]
    public Task TestAfterIfNestedIfIncompleteStatementElseStatement_BeforeElse(string statement)
        => VerifyKeywordAsync(AddInsideMethod(
            $"""
            if (true)
                if (true)
                    Console // Incomplete, but that's fine. This is not the if statement we care about.
                else
                    {statement}
            $$
            else
            """));

    [Fact]
    public Task TestNotInsideStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            if (true)
                Console.WriteLine()$$; // Complete statement, but we're not at the end of it.
            """));

    [Fact]
    public Task TestNotAfterSkippedToken()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            if (true)
                Console.WriteLine();,
            $$
            """));
}
