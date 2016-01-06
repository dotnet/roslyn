// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ThisKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInUsingAlias()
        {
            VerifyAbsence(
@"using Foo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterAngle()
        {
            VerifyAbsence(
@"interface IFoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InterfaceTypeVarianceNotAfterIn()
        {
            VerifyAbsence(
@"interface IFoo<in $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InterfaceTypeVarianceNotAfterComma()
        {
            VerifyAbsence(
@"interface IFoo<Foo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InterfaceTypeVarianceNotAfterAttribute()
        {
            VerifyAbsence(
@"interface IFoo<[Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void DelegateTypeVarianceNotAfterAngle()
        {
            VerifyAbsence(
@"delegate void D<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void DelegateTypeVarianceNotAfterComma()
        {
            VerifyAbsence(
@"delegate void D<Foo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void DelegateTypeVarianceNotAfterAttribute()
        {
            VerifyAbsence(
@"delegate void D<[Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotThisBaseListAfterAngle()
        {
            VerifyAbsence(
@"interface IFoo : Bar<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInGenericMethod()
        {
            VerifyAbsence(
@"interface IFoo {
    void Foo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterRef()
        {
            VerifyAbsence(
@"class C {
    void Foo(ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterThis_InBogusMethod()
        {
            VerifyAbsence(
@"class C {
    void Foo(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterOut()
        {
            VerifyAbsence(
@"class C {
    void Foo(out $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterMethodOpenParen()
        {
            VerifyAbsence(
@"class C {
    void Foo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterMethodComma()
        {
            VerifyAbsence(
@"class C {
    void Foo(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterMethodAttribute()
        {
            VerifyAbsence(
@"class C {
    void Foo(int i, [Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterConstructorOpenParen()
        {
            VerifyAbsence(
@"class C {
    public C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterConstructorComma()
        {
            VerifyAbsence(
@"class C {
    public C(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterConstructorAttribute()
        {
            VerifyAbsence(
@"class C {
    public C(int i, [Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterDelegateOpenParen()
        {
            VerifyAbsence(
@"delegate void D($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterDelegateComma()
        {
            VerifyAbsence(
@"delegate void D(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterDelegateAttribute()
        {
            VerifyAbsence(
@"delegate void D(int i, [Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterOperator()
        {
            VerifyAbsence(
@"class C {
    static int operator +($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterDestructor()
        {
            VerifyAbsence(
@"class C {
    ~C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterIndexer()
        {
            VerifyAbsence(
@"class C {
    int this[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInInstanceMethodInInstanceClass()
        {
            VerifyAbsence(
@"class C {
    int Foo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInStaticMethodInInstanceClass()
        {
            VerifyAbsence(
@"class C {
    static int Foo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInInstanceMethodInStaticClass()
        {
            VerifyAbsence(
@"static class C {
    int Foo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InStaticMethodInStaticClass()
        {
            VerifyKeyword(
@"static class C {
    static int Foo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterAttribute()
        {
            VerifyKeyword(
@"static class C {
    static int Foo([Bar]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterSecondAttribute()
        {
            VerifyAbsence(
@"static class C {
    static int Foo(this int i, [Bar]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterThis()
        {
            VerifyAbsence(
@"static class C {
    static int Foo(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterFirstParameter()
        {
            VerifyAbsence(
@"static class C {
    static int Foo(this int a, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InClassConstructorInitializer()
        {
            VerifyKeyword(
@"class C {
    public C() : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInStaticClassConstructorInitializer()
        {
            VerifyAbsence(
@"class C {
    static C() : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InStructConstructorInitializer()
        {
            VerifyKeyword(
@"struct C {
    public C() : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InEmptyStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterCast()
        {
            VerifyKeyword(AddInsideMethod(
@"stack.Push(((IEnumerable<Segment>)((TreeSegment)$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterReturn()
        {
            VerifyKeyword(AddInsideMethod(
@"return $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterIndexer()
        {
            VerifyKeyword(AddInsideMethod(
@"return this.items[$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterSimpleCast()
        {
            VerifyKeyword(AddInsideMethod(
@"return ((IEnumerable<T>)$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInClass()
        {
            VerifyAbsence(
@"class C {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterVoid()
        {
            VerifyAbsence(
@"class C {
    void $$");
        }

        [WorkItem(542636)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterType()
        {
            VerifyAbsence(
@"class C {
    int $$");
        }

        [WorkItem(542636)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterTypeArray()
        {
            VerifyAbsence(
@"class C {
    internal byte[] $$");
        }

        [WorkItem(542636)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterTypeArrayBeforeArguments()
        {
            VerifyAbsence(
@"class C {
    internal byte[] $$[int i] { get; }");
        }

        [WorkItem(542636)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterTypeBeforeArguments()
        {
            VerifyAbsence(
@"class C {
    internal byte $$[int i] { get; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterMultiply()
        {
            VerifyKeyword(
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
        public void NotInStaticMethod()
        {
            VerifyAbsence(
@"class C {
    static void Foo() { int i = $$ }
}");
        }

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInStaticProperty()
        {
            VerifyAbsence(
@"class C {
    static int Foo { get { int i = $$ }
}");
        }

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InInstanceProperty()
        {
            VerifyKeyword(
@"class C {
    int Foo { get { int i = $$ }
}");
        }

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInStaticConstructor()
        {
            VerifyAbsence(
@"class C {
    static C() { int i = $$ }
}");
        }

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InInstanceConstructor()
        {
            VerifyKeyword(
@"class C {
    public C() { int i = $$ }
}");
        }

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInEnumMemberInitializer1()
        {
            VerifyAbsence(
@"enum E {
    a = $$
}");
        }

        [WorkItem(539334)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterPartialInType()
        {
            VerifyAbsence(
@"class C
{
    partial $$
}");
        }

        [WorkItem(540476)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterIncompleteTypeName()
        {
            VerifyAbsence(
@"class C
{
    Foo.$$
}");
        }

        [WorkItem(541712)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInStaticMethodContext()
        {
            VerifyAbsence(
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
        public void NotInObjectInitializerMemberContext()
        {
            VerifyAbsence(@"
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
        public void InExpressionBodiedMembersProperty()
        {
            VerifyKeyword(@"
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
        public void InExpressionBodiedMembersMethod()
        {
            VerifyKeyword(@"
class C
{
    int x;
    int give() => $$");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InExpressionBodiedMembersIndexer()
        {
            VerifyKeyword(@"
class C
{
    int x;
    public object this[int i] => $$");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInExpressionBodiedMembers_Static()
        {
            VerifyAbsence(@"
class C
{
    int x;
    static int M => $$");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInExpressionBodiedMembersOperator()
        {
            VerifyAbsence(@"
class C
{
    int x;
    public static C operator - (C c1) => $$");
        }

        [WorkItem(725, "https://github.com/dotnet/roslyn/issues/725")]
        [WorkItem(1107414)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInExpressionBodiedMembersConversionOperator()
        {
            VerifyAbsence(@"
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
        public void OutsideExpressionBodiedMember()
        {
            VerifyAbsence(@"
class C
{
    int x;
    int M => this.x;$$
    int p;
}");
        }
    }
}
