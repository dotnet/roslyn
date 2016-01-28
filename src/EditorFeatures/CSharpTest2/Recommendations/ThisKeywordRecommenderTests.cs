// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ThisKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Foo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAngle()
        {
            await VerifyAbsenceAsync(
@"interface IFoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterIn()
        {
            await VerifyAbsenceAsync(
@"interface IFoo<in $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterComma()
        {
            await VerifyAbsenceAsync(
@"interface IFoo<Foo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterAttribute()
        {
            await VerifyAbsenceAsync(
@"interface IFoo<[Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceNotAfterAngle()
        {
            await VerifyAbsenceAsync(
@"delegate void D<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceNotAfterComma()
        {
            await VerifyAbsenceAsync(
@"delegate void D<Foo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceNotAfterAttribute()
        {
            await VerifyAbsenceAsync(
@"delegate void D<[Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotThisBaseListAfterAngle()
        {
            await VerifyAbsenceAsync(
@"interface IFoo : Bar<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInGenericMethod()
        {
            await VerifyAbsenceAsync(
@"interface IFoo {
    void Foo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterRef()
        {
            await VerifyAbsenceAsync(
@"class C {
    void Foo(ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterThis_InBogusMethod()
        {
            await VerifyAbsenceAsync(
@"class C {
    void Foo(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOut()
        {
            await VerifyAbsenceAsync(
@"class C {
    void Foo(out $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterMethodOpenParen()
        {
            await VerifyAbsenceAsync(
@"class C {
    void Foo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterMethodComma()
        {
            await VerifyAbsenceAsync(
@"class C {
    void Foo(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterMethodAttribute()
        {
            await VerifyAbsenceAsync(
@"class C {
    void Foo(int i, [Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterConstructorOpenParen()
        {
            await VerifyAbsenceAsync(
@"class C {
    public C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterConstructorComma()
        {
            await VerifyAbsenceAsync(
@"class C {
    public C(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterConstructorAttribute()
        {
            await VerifyAbsenceAsync(
@"class C {
    public C(int i, [Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegateOpenParen()
        {
            await VerifyAbsenceAsync(
@"delegate void D($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegateComma()
        {
            await VerifyAbsenceAsync(
@"delegate void D(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegateAttribute()
        {
            await VerifyAbsenceAsync(
@"delegate void D(int i, [Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOperator()
        {
            await VerifyAbsenceAsync(
@"class C {
    static int operator +($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDestructor()
        {
            await VerifyAbsenceAsync(
@"class C {
    ~C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIndexer()
        {
            await VerifyAbsenceAsync(
@"class C {
    int this[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInInstanceMethodInInstanceClass()
        {
            await VerifyAbsenceAsync(
@"class C {
    int Foo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInStaticMethodInInstanceClass()
        {
            await VerifyAbsenceAsync(
@"class C {
    static int Foo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInInstanceMethodInStaticClass()
        {
            await VerifyAbsenceAsync(
@"static class C {
    int Foo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInStaticMethodInStaticClass()
        {
            await VerifyKeywordAsync(
@"static class C {
    static int Foo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAttribute()
        {
            await VerifyKeywordAsync(
@"static class C {
    static int Foo([Bar]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterSecondAttribute()
        {
            await VerifyAbsenceAsync(
@"static class C {
    static int Foo(this int i, [Bar]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterThis()
        {
            await VerifyAbsenceAsync(
@"static class C {
    static int Foo(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterFirstParameter()
        {
            await VerifyAbsenceAsync(
@"static class C {
    static int Foo(this int a, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInClassConstructorInitializer()
        {
            await VerifyKeywordAsync(
@"class C {
    public C() : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInStaticClassConstructorInitializer()
        {
            await VerifyAbsenceAsync(
@"class C {
    static C() : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInStructConstructorInitializer()
        {
            await VerifyKeywordAsync(
@"struct C {
    public C() : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterCast()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"stack.Push(((IEnumerable<Segment>)((TreeSegment)$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterReturn()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexer()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return this.items[$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSimpleCast()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return ((IEnumerable<T>)$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInClass()
        {
            await VerifyAbsenceAsync(
@"class C {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterVoid()
        {
            await VerifyAbsenceAsync(
@"class C {
    void $$");
        }

        [WorkItem(542636)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterType()
        {
            await VerifyAbsenceAsync(
@"class C {
    int $$");
        }

        [WorkItem(542636)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeArray()
        {
            await VerifyAbsenceAsync(
@"class C {
    internal byte[] $$");
        }

        [WorkItem(542636)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeArrayBeforeArguments()
        {
            await VerifyAbsenceAsync(
@"class C {
    internal byte[] $$[int i] { get; }");
        }

        [WorkItem(542636)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeBeforeArguments()
        {
            await VerifyAbsenceAsync(
@"class C {
    internal byte $$[int i] { get; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMultiply()
        {
            await VerifyKeywordAsync(
@"class C {
    internal CustomAttributeRow this[uint rowId] //  This is 1 based...
    {
      get
        // ^ requires rowId <= this.NumberOfRows;
      {
        int rowOffset = (int)(rowId - 1) * $$");
        }

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInStaticMethod()
        {
            await VerifyAbsenceAsync(
@"class C {
    static void Foo() { int i = $$ }
}");
        }

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInStaticProperty()
        {
            await VerifyAbsenceAsync(
@"class C {
    static int Foo { get { int i = $$ }
}");
        }

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInInstanceProperty()
        {
            await VerifyKeywordAsync(
@"class C {
    int Foo { get { int i = $$ }
}");
        }

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInStaticConstructor()
        {
            await VerifyAbsenceAsync(
@"class C {
    static C() { int i = $$ }
}");
        }

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInInstanceConstructor()
        {
            await VerifyKeywordAsync(
@"class C {
    public C() { int i = $$ }
}");
        }

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEnumMemberInitializer1()
        {
            await VerifyAbsenceAsync(
@"enum E {
    a = $$
}");
        }

        [WorkItem(539334)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPartialInType()
        {
            await VerifyAbsenceAsync(
@"class C
{
    partial $$
}");
        }

        [WorkItem(540476)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIncompleteTypeName()
        {
            await VerifyAbsenceAsync(
@"class C
{
    Foo.$$
}");
        }

        [WorkItem(541712)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInStaticMethodContext()
        {
            await VerifyAbsenceAsync(
@"class Program
{
    static void Main(string[] args)
    {
        $$
    }
}");
        }

        [WorkItem(544219)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInObjectInitializerMemberContext()
        {
            await VerifyAbsenceAsync(@"
class C
{
    public int x, y;
    void M()
    {
        var c = new C { x = 2, y = 3, $$");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInExpressionBodiedMembersProperty()
        {
            await VerifyKeywordAsync(@"
class C
{
    int x;
    int M => $$
    int p;
}");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInExpressionBodiedMembersMethod()
        {
            await VerifyKeywordAsync(@"
class C
{
    int x;
    int give() => $$");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInExpressionBodiedMembersIndexer()
        {
            await VerifyKeywordAsync(@"
class C
{
    int x;
    public object this[int i] => $$");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInExpressionBodiedMembers_Static()
        {
            await VerifyAbsenceAsync(@"
class C
{
    int x;
    static int M => $$");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInExpressionBodiedMembersOperator()
        {
            await VerifyAbsenceAsync(@"
class C
{
    int x;
    public static C operator - (C c1) => $$");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInExpressionBodiedMembersConversionOperator()
        {
            await VerifyAbsenceAsync(@"
class F
{
}

class C
{
    int x;
    public static explicit operator F(C c1) => $$");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOutsideExpressionBodiedMember()
        {
            await VerifyAbsenceAsync(@"
class C
{
    int x;
    int M => this.x;$$
    int p;
}");
        }
    }
}
