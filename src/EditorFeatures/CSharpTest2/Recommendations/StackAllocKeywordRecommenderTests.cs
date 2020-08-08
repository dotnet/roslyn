// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class StackAllocKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClass_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInEmptyStatement()
        {
            // e.g. this is a valid statement
            // stackalloc[] { 1, 2, 3 }.IndexOf(1);
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInEmptySpaceAfterAssignment()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var v = $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeEmptySpace()
        {
            await VerifyKeywordAsync(
@"unsafe class C {
    void Goo() {
      var v = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeEmptySpace_AfterNonPointer()
        {
            // There can be an implicit conversion to int
            await VerifyKeywordAsync(
@"unsafe class C {
    void Goo() {
      int v = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeEmptySpace_AfterPointer()
        {
            await VerifyKeywordAsync(
@"unsafe class C {
    void Goo() {
      int* v = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInField()
        {
            // While assigning stackalloc'd value to a field is invalid,
            // using one in the initializer is OK. e.g.
            // int _f = stackalloc[] { 1, 2, 3 }.IndexOf(1);
            await VerifyKeywordAsync(
@"class C {
    int v = $$");
        }

        [WorkItem(544504, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544504")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideForStatementVarDecl1()
        {
            await VerifyKeywordAsync(
@"class C
{
    unsafe static void Main(string[] args)
    {
        for (var i = $$");
        }

        [WorkItem(544504, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544504")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideForStatementVarDecl2()
        {
            await VerifyKeywordAsync(
@"class C
{
    unsafe static void Main(string[] args)
    {
        for (int* i = $$");
        }

        [WorkItem(544504, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544504")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideForStatementVarDecl3()
        {
            await VerifyKeywordAsync(
@"class C
{
    unsafe static void Main(string[] args)
    {
        for (string i = $$");
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOnRHSOfAssignment_Span()
        {
            await VerifyKeywordAsync(AddInsideMethod(@"
Span<int> s = $$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOnRHSOfAssignment_Pointer()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"int* v = $$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOnRHSOfAssignment_ReAssignment()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"v = $$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOnRHSWithCast()
        {
            await VerifyKeywordAsync(AddInsideMethod(@"
var s = (Span<char>)$$"));

            await VerifyKeywordAsync(AddInsideMethod(@"
s = (Span<char>)$$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOnRHSWithConditionalExpression_True()
        {
            await VerifyKeywordAsync(AddInsideMethod(@"
var s = value ? $$"));

            await VerifyKeywordAsync(AddInsideMethod(@"
s = value ? $$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOnRHSWithConditionalExpression_True_WithCast()
        {
            await VerifyKeywordAsync(AddInsideMethod(@"
var s = value ? (Span<int>)$$"));

            await VerifyKeywordAsync(AddInsideMethod(@"
s = value ? (Span<int>)$$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOnRHSWithConditionalExpression_False()
        {
            await VerifyKeywordAsync(AddInsideMethod(@"
var s = value ? stackalloc int[10] : $$"));

            await VerifyKeywordAsync(AddInsideMethod(@"
s = value ? stackalloc int[10] : $$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOnRHSWithConditionalExpression_False_WithCast()
        {
            await VerifyKeywordAsync(AddInsideMethod(@"
var s = value ? stackalloc int[10] : (Span<int>)$$"));

            await VerifyKeywordAsync(AddInsideMethod(@"
s = value ? stackalloc int[10] : (Span<int>)$$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOnRHSWithConditionalExpression_NestedConditional_True()
        {
            await VerifyKeywordAsync(AddInsideMethod(@"
var s = value1 ? value2 ? $$"));

            await VerifyKeywordAsync(AddInsideMethod(@"
s = value1 ? value2 ? $$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOnRHSWithConditionalExpression_NestedConditional_WithCast_True()
        {
            await VerifyKeywordAsync(AddInsideMethod(@"
var s = value1 ? value2 ? (Span<int>)$$"));

            await VerifyKeywordAsync(AddInsideMethod(@"
s = value1 ? value2 ? (Span<int>)$$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOnRHSWithConditionalExpression_NestedConditional_False()
        {
            await VerifyKeywordAsync(AddInsideMethod(@"
var s = value1 ? value2 ? stackalloc int [10] : $$"));

            await VerifyKeywordAsync(AddInsideMethod(@"
s = value1 ? value2 ? stackalloc int [10] : $$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOnRHSWithConditionalExpression_NestedConditional_WithCast_False()
        {
            await VerifyKeywordAsync(AddInsideMethod(@"
var s = value1 ? value2 ? stackalloc int [10] : (Span<int>)$$"));

            await VerifyKeywordAsync(AddInsideMethod(@"
s = value1 ? value2 ? stackalloc int [10] : (Span<int>)$$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInLHSOfAssignment()
        {
            await VerifyAbsenceAsync(AddInsideMethod(@"
var x $$ ="));

            await VerifyAbsenceAsync(AddInsideMethod(@"
x $$ ="));
        }

        [WorkItem(41736, "https://github.com/dotnet/roslyn/issues/41736")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInArgument()
        {
            await VerifyKeywordAsync(@"
class Program
{
    static void Method(System.Span<byte> span)
    {
        Method($$);
    }
}");

            await VerifyKeywordAsync(@"
class Program
{
    static void Method(int x, System.Span<byte> span)
    {
        Method(1, $$);
    }
}");
        }

        [WorkItem(41736, "https://github.com/dotnet/roslyn/issues/41736")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInConstFieldInitializer()
        {
            await VerifyAbsenceAsync(@"
class Program
{
    private const int _f = $$
}");
        }
    }
}
