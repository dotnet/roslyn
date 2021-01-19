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
    public class VoidKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtRoot()
        {
            VerifyKeyword(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterClass()
        {
            VerifyKeyword(
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
        public void TestNotAfterStackAlloc()
        {
            VerifyAbsence(
@"class C {
     int* goo = stackalloc $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInFixedStatement()
        {
            VerifyKeyword(
@"fixed ($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInDelegateReturnType()
        {
            VerifyKeyword(
@"public delegate $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestNotInCastType(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"var str = (($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestNotInCastType2(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"var str = (($$)items) as string;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestInTypeOf(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"typeof($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterExtern()
        {
            VerifyKeyword(@"extern alias Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterUsing()
        {
            VerifyKeyword(@"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNamespace()
        {
            VerifyKeyword(@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterDelegateDeclaration()
        {
            VerifyKeyword(@"delegate void Goo();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterMethod()
        {
            VerifyKeyword(
@"class C {
  void Goo() {}
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterField()
        {
            VerifyKeyword(
@"class C {
  int i;
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterProperty()
        {
            VerifyKeyword(
@"class C {
  int i { get; }
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotBeforeUsing()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"$$
using Goo;");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/9880"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotBeforeUsing_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$
using Goo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterAssemblyAttribute()
        {
            VerifyKeyword(@"[assembly: goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterRootAttribute()
        {
            VerifyKeyword(@"[goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterMultipleRootAttributes()
        {
            VerifyKeyword(@"[goo][goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedAttribute()
        {
            VerifyKeyword(
@"class C {
  [goo]
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideStruct()
        {
            VerifyKeyword(
@"struct S {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideInterface()
        {
            VerifyKeyword(
@"interface I {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideClass()
        {
            VerifyKeyword(
@"class C {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterPartial()
            => VerifyAbsence(@"partial $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedPartial()
        {
            VerifyKeyword(
@"class C {
    partial $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterAbstract()
            => VerifyAbsence(@"abstract $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedAbstract()
        {
            VerifyKeyword(
@"class C {
    abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterInternal()
            => VerifyAbsence(SourceCodeKind.Regular, @"internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterInternal_Interactive()
            => VerifyKeyword(SourceCodeKind.Script, @"internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedInternal()
        {
            VerifyKeyword(
@"class C {
    internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterPublic()
            => VerifyAbsence(SourceCodeKind.Regular, @"public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPublic_Interactive()
            => VerifyKeyword(SourceCodeKind.Script, @"public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedPublic()
        {
            VerifyKeyword(
@"class C {
    public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterPrivate()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPrivate_Script()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedPrivate()
        {
            VerifyKeyword(
@"class C {
    private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterProtected()
        {
            VerifyAbsence(
@"protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedProtected()
        {
            VerifyKeyword(
@"class C {
    protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterSealed()
            => VerifyAbsence(@"sealed $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedSealed()
        {
            VerifyKeyword(
@"class C {
    sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterStatic()
            => VerifyKeyword(@"static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterStaticInClass()
        {
            VerifyKeyword(
@"class C {
    static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterStaticPublic()
            => VerifyAbsence(SourceCodeKind.Regular, @"static public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterStaticPublic_Interactive()
            => VerifyKeyword(SourceCodeKind.Script, @"static public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedStaticPublic()
        {
            VerifyKeyword(
@"class C {
    static public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterDelegate()
        {
            VerifyKeyword(
@"delegate $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestNotAfterAnonymousDelegate(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"var q = delegate $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterEvent()
        {
            VerifyAbsence(
@"class C {
    event $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterVoid()
        {
            VerifyAbsence(
@"class C {
    void $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterNew()
        {
            VerifyAbsence(
@"new $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedNew()
        {
            VerifyKeyword(
@"class C {
   new $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestInUnsafeBlock(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"unsafe {
    $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUnsafeMethod()
        {
            VerifyKeyword(
@"class C {
   unsafe void Goo() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUnsafeClass()
        {
            VerifyKeyword(
@"unsafe class C {
   void Goo() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInParameter()
        {
            VerifyAbsence(
@"class C {
   void Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUnsafeParameter1()
        {
            VerifyKeyword(
@"class C {
   unsafe void Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUnsafeParameter2()
        {
            VerifyKeyword(
@"unsafe class C {
   void Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInCast()
        {
            VerifyAbsence(
@"class C {
   void Goo() {
     hr = GetRealProcAddress(""CompareAssemblyIdentity"", ($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInCast2()
        {
            VerifyAbsence(
@"class C {
   void Goo() {
     hr = GetRealProcAddress(""CompareAssemblyIdentity"", ($$**)pfnCompareAssemblyIdentity);");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUnsafeCast()
        {
            VerifyKeyword(
@"unsafe class C {
   void Goo() {
     hr = GetRealProcAddress(""CompareAssemblyIdentity"", ($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUnsafeCast2()
        {
            VerifyKeyword(
@"unsafe class C {
   void Goo() {
     hr = GetRealProcAddress(""CompareAssemblyIdentity"", ($$**)pfnCompareAssemblyIdentity);");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUnsafeConversionOperator()
        {
            VerifyKeyword(
@"class C {
   unsafe implicit operator int(C c) {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUnsafeOperator()
        {
            VerifyKeyword(
@"class C {
   unsafe int operator ++(C c) {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUnsafeConstructor()
        {
            VerifyKeyword(
@"class C {
   unsafe C() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUnsafeDestructor()
        {
            VerifyKeyword(
@"class C {
   unsafe ~C() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUnsafeProperty()
        {
            VerifyKeyword(
@"class C {
   unsafe int Goo {
     get {
       $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUnsafeIndexer()
        {
            VerifyKeyword(
@"class C {
   unsafe int this[int i] {
     get {
       $$");
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestNotInDefault(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"default($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestInSizeOf(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"sizeof($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [WorkItem(544347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544347")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUnsafeDefaultExpression()
        {
            VerifyKeyword(
@"unsafe class C
{
    static void Method1(void* p1 = default($$");
        }

        [WorkItem(544347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544347")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInDefaultExpression()
        {
            VerifyAbsence(
@"class C
{
    static void Method1(void* p1 = default($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfter()
            => VerifyKeyword(@"class c { async $$ }");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterAsyncAsType()
            => VerifyAbsence(@"class c { async async $$ }");

        [Fact]
        [WorkItem(8617, "https://github.com/dotnet/roslyn/issues/8617")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestLocalFunction()
        {
            VerifyKeyword(@"
class C
{
    void M()
    {
        $$
    }
}");
        }

        [Fact]
        [WorkItem(8617, "https://github.com/dotnet/roslyn/issues/8617")]
        [WorkItem(14525, "https://github.com/dotnet/roslyn/issues/14525")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestLocalFunction2()
        {
            VerifyKeyword(@"
class C
{
    void M()
    {
        async $$
    }
}");
        }

        [Fact]
        [WorkItem(8617, "https://github.com/dotnet/roslyn/issues/8617")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestLocalFunction3()
        {
            VerifyAbsence(@"
class C
{
    void M()
    {
        async async $$
    }
}");
        }

        [Fact]
        [WorkItem(8617, "https://github.com/dotnet/roslyn/issues/8617")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestLocalFunction4()
        {
            VerifyAbsence(@"
class C
{
    void M()
    {
        var async $$
    }
}");
        }

        [Fact]
        [WorkItem(8617, "https://github.com/dotnet/roslyn/issues/8617")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestLocalFunction5()
        {
            VerifyAbsence(@"
using System;
class C
{
    void M(Action<int> a)
    {
        M(async $$ () => 
    }
}");
        }

        [Fact]
        [WorkItem(8617, "https://github.com/dotnet/roslyn/issues/8617")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestLocalFunction6()
        {
            VerifyKeyword(@"
class C
{
    void M()
    {
        unsafe async $$
    }
}");
        }

        [Fact]
        [WorkItem(8617, "https://github.com/dotnet/roslyn/issues/8617")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestLocalFunction7()
        {
            VerifyKeyword(@"
using System;
class C
{
    void M(Action<int> a)
    {
        M(async () =>
        {
            async $$
        })
    }
}");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInFunctionPointer01()
        {
            VerifyKeyword(@"
class C
{
    delegate*<$$");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInFunctionPointer02()
        {
            VerifyKeyword(@"
class C<T>
{
    C<delegate*<$$");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInFunctionPointer03()
        {
            VerifyKeyword(@"
class C
{
    delegate*<int, $$");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInFunctionPointer04()
        {
            VerifyKeyword(@"
class C
{
    delegate*<int, v$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInFunctionPointerTypeAfterComma()
        {
            VerifyKeyword(@"
class C
{
    delegate*<int, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInFunctionPointerTypeAfterModifier()
        {
            VerifyKeyword(@"
class C
{
    delegate*<ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterDelegateAsterisk()
        {
            VerifyAbsence(@"
class C
{
    delegate*$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(43295, "https://github.com/dotnet/roslyn/issues/43295")]
        public void TestAfterReadonlyInStruct()
        {
            VerifyKeyword(@"
struct S
{
    public readonly $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(43295, "https://github.com/dotnet/roslyn/issues/43295")]
        public void TestNotAfterReadonlyInClass()
        {
            VerifyAbsence(@"
class C
{
    public readonly $$");
        }
    }
}
