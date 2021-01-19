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
    public class StackAllocKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtRoot_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterClass_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGlobalStatement_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
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
        public void TestInEmptyStatement()
        {
            // e.g. this is a valid statement
            // stackalloc[] { 1, 2, 3 }.IndexOf(1);
            VerifyKeyword(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInEmptySpaceAfterAssignment()
        {
            VerifyKeyword(AddInsideMethod(
@"var v = $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUnsafeEmptySpace()
        {
            VerifyKeyword(
@"unsafe class C {
    void Goo() {
      var v = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUnsafeEmptySpace_AfterNonPointer()
        {
            // There can be an implicit conversion to int
            VerifyKeyword(
@"unsafe class C {
    void Goo() {
      int v = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUnsafeEmptySpace_AfterPointer()
        {
            VerifyKeyword(
@"unsafe class C {
    void Goo() {
      int* v = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInField()
        {
            // While assigning stackalloc'd value to a field is invalid,
            // using one in the initializer is OK. e.g.
            // int _f = stackalloc[] { 1, 2, 3 }.IndexOf(1);
            VerifyKeyword(
@"class C {
    int v = $$");
        }

        [WorkItem(544504, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544504")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideForStatementVarDecl1()
        {
            VerifyKeyword(
@"class C
{
    unsafe static void Main(string[] args)
    {
        for (var i = $$");
        }

        [WorkItem(544504, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544504")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideForStatementVarDecl2()
        {
            VerifyKeyword(
@"class C
{
    unsafe static void Main(string[] args)
    {
        for (int* i = $$");
        }

        [WorkItem(544504, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544504")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideForStatementVarDecl3()
        {
            VerifyKeyword(
@"class C
{
    unsafe static void Main(string[] args)
    {
        for (string i = $$");
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestOnRHSOfAssignment_Span()
        {
            VerifyKeyword(AddInsideMethod(@"
Span<int> s = $$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestOnRHSOfAssignment_Pointer()
        {
            VerifyKeyword(AddInsideMethod(
@"int* v = $$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestOnRHSOfAssignment_ReAssignment()
        {
            VerifyKeyword(AddInsideMethod(
@"v = $$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestOnRHSWithCast()
        {
            VerifyKeyword(AddInsideMethod(@"
var s = (Span<char>)$$"));

            VerifyKeyword(AddInsideMethod(@"
s = (Span<char>)$$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestOnRHSWithConditionalExpression_True()
        {
            VerifyKeyword(AddInsideMethod(@"
var s = value ? $$"));

            VerifyKeyword(AddInsideMethod(@"
s = value ? $$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestOnRHSWithConditionalExpression_True_WithCast()
        {
            VerifyKeyword(AddInsideMethod(@"
var s = value ? (Span<int>)$$"));

            VerifyKeyword(AddInsideMethod(@"
s = value ? (Span<int>)$$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestOnRHSWithConditionalExpression_False()
        {
            VerifyKeyword(AddInsideMethod(@"
var s = value ? stackalloc int[10] : $$"));

            VerifyKeyword(AddInsideMethod(@"
s = value ? stackalloc int[10] : $$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestOnRHSWithConditionalExpression_False_WithCast()
        {
            VerifyKeyword(AddInsideMethod(@"
var s = value ? stackalloc int[10] : (Span<int>)$$"));

            VerifyKeyword(AddInsideMethod(@"
s = value ? stackalloc int[10] : (Span<int>)$$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestOnRHSWithConditionalExpression_NestedConditional_True()
        {
            VerifyKeyword(AddInsideMethod(@"
var s = value1 ? value2 ? $$"));

            VerifyKeyword(AddInsideMethod(@"
s = value1 ? value2 ? $$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestOnRHSWithConditionalExpression_NestedConditional_WithCast_True()
        {
            VerifyKeyword(AddInsideMethod(@"
var s = value1 ? value2 ? (Span<int>)$$"));

            VerifyKeyword(AddInsideMethod(@"
s = value1 ? value2 ? (Span<int>)$$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestOnRHSWithConditionalExpression_NestedConditional_False()
        {
            VerifyKeyword(AddInsideMethod(@"
var s = value1 ? value2 ? stackalloc int [10] : $$"));

            VerifyKeyword(AddInsideMethod(@"
s = value1 ? value2 ? stackalloc int [10] : $$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestOnRHSWithConditionalExpression_NestedConditional_WithCast_False()
        {
            VerifyKeyword(AddInsideMethod(@"
var s = value1 ? value2 ? stackalloc int [10] : (Span<int>)$$"));

            VerifyKeyword(AddInsideMethod(@"
s = value1 ? value2 ? stackalloc int [10] : (Span<int>)$$"));
        }

        [WorkItem(23584, "https://github.com/dotnet/roslyn/issues/23584")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInLHSOfAssignment()
        {
            VerifyAbsence(AddInsideMethod(@"
var x $$ ="));

            VerifyAbsence(AddInsideMethod(@"
x $$ ="));
        }

        [WorkItem(41736, "https://github.com/dotnet/roslyn/issues/41736")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInArgument()
        {
            VerifyKeyword(@"
class Program
{
    static void Method(System.Span<byte> span)
    {
        Method($$);
    }
}");

            VerifyKeyword(@"
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
        public void TestNotInConstFieldInitializer()
        {
            VerifyAbsence(@"
class Program
{
    private const int _f = $$
}");
        }
    }
}
