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
        public async Task TestAtRoot()
        {
            VerifyKeyword(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClass()
        {
            VerifyKeyword(
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
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
        public async Task TestNotAfterStackAlloc()
        {
            VerifyAbsence(
@"class C {
     int* goo = stackalloc $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFixedStatement()
        {
            VerifyKeyword(
@"fixed ($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInDelegateReturnType()
        {
            VerifyKeyword(
@"public delegate $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotInCastType(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"var str = (($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotInCastType2(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"var str = (($$)items) as string;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInTypeOf(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"typeof($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterExtern()
        {
            VerifyKeyword(@"extern alias Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsing()
        {
            VerifyKeyword(@"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNamespace()
        {
            VerifyKeyword(@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateDeclaration()
        {
            VerifyKeyword(@"delegate void Goo();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethod()
        {
            VerifyKeyword(
@"class C {
  void Goo() {}
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterField()
        {
            VerifyKeyword(
@"class C {
  int i;
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterProperty()
        {
            VerifyKeyword(
@"class C {
  int i { get; }
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeUsing()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"$$
using Goo;");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/9880"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeUsing_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$
using Goo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAssemblyAttribute()
        {
            VerifyKeyword(@"[assembly: goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRootAttribute()
        {
            VerifyKeyword(@"[goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMultipleRootAttributes()
        {
            VerifyKeyword(@"[goo][goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedAttribute()
        {
            VerifyKeyword(
@"class C {
  [goo]
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideStruct()
        {
            VerifyKeyword(
@"struct S {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideInterface()
        {
            VerifyKeyword(
@"interface I {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideClass()
        {
            VerifyKeyword(
@"class C {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPartial()
            => VerifyAbsence(@"partial $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPartial()
        {
            VerifyKeyword(
@"class C {
    partial $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAbstract()
            => VerifyAbsence(@"abstract $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedAbstract()
        {
            VerifyKeyword(
@"class C {
    abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterInternal()
            => VerifyAbsence(SourceCodeKind.Regular, @"internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterInternal_Interactive()
            => VerifyKeyword(SourceCodeKind.Script, @"internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedInternal()
        {
            VerifyKeyword(
@"class C {
    internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPublic()
            => VerifyAbsence(SourceCodeKind.Regular, @"public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPublic_Interactive()
            => VerifyKeyword(SourceCodeKind.Script, @"public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPublic()
        {
            VerifyKeyword(
@"class C {
    public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPrivate()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPrivate_Script()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPrivate()
        {
            VerifyKeyword(
@"class C {
    private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterProtected()
        {
            VerifyAbsence(
@"protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedProtected()
        {
            VerifyKeyword(
@"class C {
    protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterSealed()
            => VerifyAbsence(@"sealed $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedSealed()
        {
            VerifyKeyword(
@"class C {
    sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStatic()
            => VerifyKeyword(@"static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStaticInClass()
        {
            VerifyKeyword(
@"class C {
    static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStaticPublic()
            => VerifyAbsence(SourceCodeKind.Regular, @"static public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStaticPublic_Interactive()
            => VerifyKeyword(SourceCodeKind.Script, @"static public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedStaticPublic()
        {
            VerifyKeyword(
@"class C {
    static public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegate()
        {
            VerifyKeyword(
@"delegate $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotAfterAnonymousDelegate(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"var q = delegate $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterEvent()
        {
            VerifyAbsence(
@"class C {
    event $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterVoid()
        {
            VerifyAbsence(
@"class C {
    void $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNew()
        {
            VerifyAbsence(
@"new $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedNew()
        {
            VerifyKeyword(
@"class C {
   new $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInUnsafeBlock(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"unsafe {
    $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeMethod()
        {
            VerifyKeyword(
@"class C {
   unsafe void Goo() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeClass()
        {
            VerifyKeyword(
@"unsafe class C {
   void Goo() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInParameter()
        {
            VerifyAbsence(
@"class C {
   void Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeParameter1()
        {
            VerifyKeyword(
@"class C {
   unsafe void Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeParameter2()
        {
            VerifyKeyword(
@"unsafe class C {
   void Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInCast()
        {
            VerifyAbsence(
@"class C {
   void Goo() {
     hr = GetRealProcAddress(""CompareAssemblyIdentity"", ($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInCast2()
        {
            VerifyAbsence(
@"class C {
   void Goo() {
     hr = GetRealProcAddress(""CompareAssemblyIdentity"", ($$**)pfnCompareAssemblyIdentity);");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeCast()
        {
            VerifyKeyword(
@"unsafe class C {
   void Goo() {
     hr = GetRealProcAddress(""CompareAssemblyIdentity"", ($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeCast2()
        {
            VerifyKeyword(
@"unsafe class C {
   void Goo() {
     hr = GetRealProcAddress(""CompareAssemblyIdentity"", ($$**)pfnCompareAssemblyIdentity);");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeConversionOperator()
        {
            VerifyKeyword(
@"class C {
   unsafe implicit operator int(C c) {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeOperator()
        {
            VerifyKeyword(
@"class C {
   unsafe int operator ++(C c) {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeConstructor()
        {
            VerifyKeyword(
@"class C {
   unsafe C() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeDestructor()
        {
            VerifyKeyword(
@"class C {
   unsafe ~C() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeProperty()
        {
            VerifyKeyword(
@"class C {
   unsafe int Goo {
     get {
       $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeIndexer()
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
        public async Task TestNotInDefault(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"default($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInSizeOf(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"sizeof($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [WorkItem(544347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544347")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeDefaultExpression()
        {
            VerifyKeyword(
@"unsafe class C
{
    static void Method1(void* p1 = default($$");
        }

        [WorkItem(544347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544347")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInDefaultExpression()
        {
            VerifyAbsence(
@"class C
{
    static void Method1(void* p1 = default($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAsync()
            => VerifyKeyword(@"class c { async $$ }");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAsyncAsType()
            => VerifyAbsence(@"class c { async async $$ }");

        [Fact]
        [WorkItem(8617, "https://github.com/dotnet/roslyn/issues/8617")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction()
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
        public async Task TestLocalFunction2()
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
        public async Task TestLocalFunction3()
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
        public async Task TestLocalFunction4()
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
        public async Task TestLocalFunction5()
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
        public async Task TestLocalFunction6()
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
        public async Task TestLocalFunction7()
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
        public async Task TestInFunctionPointer01()
        {
            VerifyKeyword(@"
class C
{
    delegate*<$$");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFunctionPointer02()
        {
            VerifyKeyword(@"
class C<T>
{
    C<delegate*<$$");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFunctionPointer03()
        {
            VerifyKeyword(@"
class C
{
    delegate*<int, $$");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFunctionPointer04()
        {
            VerifyKeyword(@"
class C
{
    delegate*<int, v$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFunctionPointerTypeAfterComma()
        {
            VerifyKeyword(@"
class C
{
    delegate*<int, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFunctionPointerTypeAfterModifier()
        {
            VerifyKeyword(@"
class C
{
    delegate*<ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegateAsterisk()
        {
            VerifyAbsence(@"
class C
{
    delegate*$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(43295, "https://github.com/dotnet/roslyn/issues/43295")]
        public async Task TestAfterReadonlyInStruct()
        {
            VerifyKeyword(@"
struct S
{
    public readonly $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(43295, "https://github.com/dotnet/roslyn/issues/43295")]
        public async Task TestNotAfterReadonlyInClass()
        {
            VerifyAbsence(@"
class C
{
    public readonly $$");
        }
    }
}
