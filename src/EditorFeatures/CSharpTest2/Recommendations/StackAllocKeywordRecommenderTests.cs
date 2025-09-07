// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class StackAllocKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAtRoot_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public Task TestAfterClass_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestAfterGlobalStatement()
        => VerifyKeywordAsync(
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestAfterGlobalVariableDeclaration_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
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
    public Task TestInEmptyStatement()
        => VerifyKeywordAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestInEmptySpaceAfterAssignment()
        => VerifyKeywordAsync(AddInsideMethod(
@"var v = $$"));

    [Fact]
    public Task TestInUnsafeEmptySpace()
        => VerifyKeywordAsync(
            """
            unsafe class C {
                void Goo() {
                  var v = $$
            """);

    [Fact]
    public Task TestInUnsafeEmptySpace_AfterNonPointer()
        => VerifyKeywordAsync(
            """
            unsafe class C {
                void Goo() {
                  int v = $$
            """);

    [Fact]
    public Task TestInUnsafeEmptySpace_AfterPointer()
        => VerifyKeywordAsync(
            """
            unsafe class C {
                void Goo() {
                  int* v = $$
            """);

    [Fact]
    public Task TestInField()
        => VerifyKeywordAsync(
            """
            class C {
                int v = $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544504")]
    public Task TestInsideForStatementVarDecl1()
        => VerifyKeywordAsync(
            """
            class C
            {
                unsafe static void Main(string[] args)
                {
                    for (var i = $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544504")]
    public Task TestInsideForStatementVarDecl2()
        => VerifyKeywordAsync(
            """
            class C
            {
                unsafe static void Main(string[] args)
                {
                    for (int* i = $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544504")]
    public Task TestInsideForStatementVarDecl3()
        => VerifyKeywordAsync(
            """
            class C
            {
                unsafe static void Main(string[] args)
                {
                    for (string i = $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23584")]
    public Task TestOnRHSOfAssignment_Span()
        => VerifyKeywordAsync(AddInsideMethod("""
            Span<int> s = $$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23584")]
    public Task TestOnRHSOfAssignment_Pointer()
        => VerifyKeywordAsync(AddInsideMethod(
@"int* v = $$"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23584")]
    public Task TestOnRHSOfAssignment_ReAssignment()
        => VerifyKeywordAsync(AddInsideMethod(
@"v = $$"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23584")]
    public async Task TestOnRHSWithCast()
    {
        await VerifyKeywordAsync(AddInsideMethod("""
            var s = (Span<char>)$$
            """));

        await VerifyKeywordAsync(AddInsideMethod("""
            s = (Span<char>)$$
            """));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23584")]
    public async Task TestOnRHSWithConditionalExpression_True()
    {
        await VerifyKeywordAsync(AddInsideMethod("""
            var s = value ? $$
            """));

        await VerifyKeywordAsync(AddInsideMethod("""
            s = value ? $$
            """));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23584")]
    public async Task TestOnRHSWithConditionalExpression_True_WithCast()
    {
        await VerifyKeywordAsync(AddInsideMethod("""
            var s = value ? (Span<int>)$$
            """));

        await VerifyKeywordAsync(AddInsideMethod("""
            s = value ? (Span<int>)$$
            """));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23584")]
    public async Task TestOnRHSWithConditionalExpression_False()
    {
        await VerifyKeywordAsync(AddInsideMethod("""
            var s = value ? stackalloc int[10] : $$
            """));

        await VerifyKeywordAsync(AddInsideMethod("""
            s = value ? stackalloc int[10] : $$
            """));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23584")]
    public async Task TestOnRHSWithConditionalExpression_False_WithCast()
    {
        await VerifyKeywordAsync(AddInsideMethod("""
            var s = value ? stackalloc int[10] : (Span<int>)$$
            """));

        await VerifyKeywordAsync(AddInsideMethod("""
            s = value ? stackalloc int[10] : (Span<int>)$$
            """));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23584")]
    public async Task TestOnRHSWithConditionalExpression_NestedConditional_True()
    {
        await VerifyKeywordAsync(AddInsideMethod("""
            var s = value1 ? value2 ? $$
            """));

        await VerifyKeywordAsync(AddInsideMethod("""
            s = value1 ? value2 ? $$
            """));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23584")]
    public async Task TestOnRHSWithConditionalExpression_NestedConditional_WithCast_True()
    {
        await VerifyKeywordAsync(AddInsideMethod("""
            var s = value1 ? value2 ? (Span<int>)$$
            """));

        await VerifyKeywordAsync(AddInsideMethod("""
            s = value1 ? value2 ? (Span<int>)$$
            """));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23584")]
    public async Task TestOnRHSWithConditionalExpression_NestedConditional_False()
    {
        await VerifyKeywordAsync(AddInsideMethod("""
            var s = value1 ? value2 ? stackalloc int [10] : $$
            """));

        await VerifyKeywordAsync(AddInsideMethod("""
            s = value1 ? value2 ? stackalloc int [10] : $$
            """));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23584")]
    public async Task TestOnRHSWithConditionalExpression_NestedConditional_WithCast_False()
    {
        await VerifyKeywordAsync(AddInsideMethod("""
            var s = value1 ? value2 ? stackalloc int [10] : (Span<int>)$$
            """));

        await VerifyKeywordAsync(AddInsideMethod("""
            s = value1 ? value2 ? stackalloc int [10] : (Span<int>)$$
            """));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23584")]
    public async Task TestNotInLHSOfAssignment()
    {
        await VerifyAbsenceAsync(AddInsideMethod("""
            var x $$ =
            """));

        await VerifyAbsenceAsync(AddInsideMethod("""
            x $$ =
            """));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41736")]
    public async Task TestInArgument()
    {
        await VerifyKeywordAsync("""
            class Program
            {
                static void Method(System.Span<byte> span)
                {
                    Method($$);
                }
            }
            """);

        await VerifyKeywordAsync("""
            class Program
            {
                static void Method(int x, System.Span<byte> span)
                {
                    Method(1, $$);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41736")]
    public Task TestNotInConstFieldInitializer()
        => VerifyAbsenceAsync("""
            class Program
            {
                private const int _f = $$
            }
            """);

    #region Collection expressions

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_BeforeFirstElementToVar()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var x = [$$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_BeforeFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [$$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_AfterFirstElementToVar()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var x = [new object(), $$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_AfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_SpreadBeforeFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [.. $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_SpreadAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, .. $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenAtFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, ($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenSpreadAtFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [.. ($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenSpreadAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, .. ($$
            }
            """);

    #endregion
}
