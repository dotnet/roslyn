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
    public class StackAllocKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestAfterClass_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
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
        public async Task TestInEmptyStatement()
        {
            // e.g. this is a valid statement
            // stackalloc[] { 1, 2, 3 }.IndexOf(1);
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestInEmptySpaceAfterAssignment()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var v = $$"));
        }

        [Fact]
        public async Task TestInUnsafeEmptySpace()
        {
            await VerifyKeywordAsync(
                """
                unsafe class C {
                    void Goo() {
                      var v = $$
                """);
        }

        [Fact]
        public async Task TestInUnsafeEmptySpace_AfterNonPointer()
        {
            // There can be an implicit conversion to int
            await VerifyKeywordAsync(
                """
                unsafe class C {
                    void Goo() {
                      int v = $$
                """);
        }

        [Fact]
        public async Task TestInUnsafeEmptySpace_AfterPointer()
        {
            await VerifyKeywordAsync(
                """
                unsafe class C {
                    void Goo() {
                      int* v = $$
                """);
        }

        [Fact]
        public async Task TestInField()
        {
            // While assigning stackalloc'd value to a field is invalid,
            // using one in the initializer is OK. e.g.
            // int _f = stackalloc[] { 1, 2, 3 }.IndexOf(1);
            await VerifyKeywordAsync(
                """
                class C {
                    int v = $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544504")]
        public async Task TestInsideForStatementVarDecl1()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    unsafe static void Main(string[] args)
                    {
                        for (var i = $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544504")]
        public async Task TestInsideForStatementVarDecl2()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    unsafe static void Main(string[] args)
                    {
                        for (int* i = $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544504")]
        public async Task TestInsideForStatementVarDecl3()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    unsafe static void Main(string[] args)
                    {
                        for (string i = $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23584")]
        public async Task TestOnRHSOfAssignment_Span()
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                Span<int> s = $$
                """));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23584")]
        public async Task TestOnRHSOfAssignment_Pointer()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"int* v = $$"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23584")]
        public async Task TestOnRHSOfAssignment_ReAssignment()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"v = $$"));
        }

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
        public async Task TestNotInConstFieldInitializer()
        {
            await VerifyAbsenceAsync("""
                class Program
                {
                    private const int _f = $$
                }
                """);
        }

        #region Collection expressions

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_BeforeFirstElementToVar()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var x = [$$
                """));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_BeforeFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [$$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_AfterFirstElementToVar()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var x = [new object(), $$
                """));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_AfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_SpreadBeforeFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [.. $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_SpreadAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, .. $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenAtFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, ($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenSpreadAtFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [.. ($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenSpreadAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, .. ($$
                }
                """);
        }

        #endregion
    }
}
