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
    public class VoidKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestAtRoot()
        {
            await VerifyKeywordAsync(
@"$$");
        }

        [Fact]
        public async Task TestAfterClass()
        {
            await VerifyKeywordAsync(
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
        public async Task TestNotAfterStackAlloc()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                     int* goo = stackalloc $$
                """);
        }

        [Fact]
        public async Task TestInFixedStatement()
        {
            await VerifyKeywordAsync(
@"fixed ($$");
        }

        [Fact]
        public async Task TestInDelegateReturnType()
        {
            await VerifyKeywordAsync(
@"public delegate $$");
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotInCastType(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var str = (($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotInCastType2(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var str = (($$)items) as string;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInTypeOf(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"typeof($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestAfterExtern()
        {
            await VerifyKeywordAsync("""
                extern alias Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterUsing()
        {
            await VerifyKeywordAsync("""
                using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalUsing()
        {
            await VerifyKeywordAsync("""
                global using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterNamespace()
        {
            await VerifyKeywordAsync("""
                namespace N {}
                $$
                """);
        }

        [Fact]
        public async Task TestAfterDelegateDeclaration()
        {
            await VerifyKeywordAsync("""
                delegate void Goo();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterMethod()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  void Goo() {}
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterField()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  int i;
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterProperty()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  int i { get; }
                  $$
                """);
        }

        [Fact]
        public async Task TestNotBeforeUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                $$
                using Goo;
                """);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                $$
                using Goo;
                """);
        }

        [Fact]
        public async Task TestNotBeforeGlobalUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                $$
                global using Goo;
                """);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeGlobalUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                $$
                global using Goo;
                """);
        }

        [Fact]
        public async Task TestAfterAssemblyAttribute()
        {
            await VerifyKeywordAsync("""
                [assembly: goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterRootAttribute()
        {
            await VerifyKeywordAsync("""
                [goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterMultipleRootAttributes()
        {
            await VerifyKeywordAsync("""
                [goo][goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  [goo]
                  $$
                """);
        }

        [Fact]
        public async Task TestInsideStruct()
        {
            await VerifyKeywordAsync(
                """
                struct S {
                   $$
                """);
        }

        [Fact]
        public async Task TestInsideInterface()
        {
            await VerifyKeywordAsync(
                """
                interface I {
                   $$
                """);
        }

        [Fact]
        public async Task TestInsideClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPartial()
            => await VerifyAbsenceAsync(@"partial $$");

        [Fact]
        public async Task TestAfterNestedPartial()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    partial $$
                """);
        }

        [Fact]
        public async Task TestNotAfterAbstract()
            => await VerifyAbsenceAsync(@"abstract $$");

        [Fact]
        public async Task TestAfterNestedAbstract()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    abstract $$
                """);
        }

        [Fact]
        public async Task TestNotAfterInternal()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"internal $$");

        [Fact]
        public async Task TestAfterInternal_Interactive()
            => await VerifyKeywordAsync(SourceCodeKind.Script, @"internal $$");

        [Fact]
        public async Task TestAfterNestedInternal()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    internal $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPublic()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"public $$");

        [Fact]
        public async Task TestAfterPublic_Interactive()
            => await VerifyKeywordAsync(SourceCodeKind.Script, @"public $$");

        [Fact]
        public async Task TestAfterNestedPublic()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPrivate()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
@"private $$");
        }

        [Fact]
        public async Task TestAfterPrivate_Script()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"private $$");
        }

        [Fact]
        public async Task TestAfterNestedPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    private $$
                """);
        }

        [Fact]
        public async Task TestNotAfterProtected()
        {
            await VerifyAbsenceAsync(
@"protected $$");
        }

        [Fact]
        public async Task TestAfterNestedProtected()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    protected $$
                """);
        }

        [Fact]
        public async Task TestNotAfterSealed()
            => await VerifyAbsenceAsync(@"sealed $$");

        [Fact]
        public async Task TestAfterNestedSealed()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    sealed $$
                """);
        }

        [Fact]
        public async Task TestAfterStatic()
            => await VerifyKeywordAsync(@"static $$");

        [Fact]
        public async Task TestAfterStaticInClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    static $$
                """);
        }

        [Fact]
        public async Task TestNotAfterStaticPublic()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"static public $$");

        [Fact]
        public async Task TestAfterStaticPublic_Interactive()
            => await VerifyKeywordAsync(SourceCodeKind.Script, @"static public $$");

        [Fact]
        public async Task TestAfterNestedStaticPublic()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    static public $$
                """);
        }

        [Fact]
        public async Task TestAfterDelegate()
        {
            await VerifyKeywordAsync(
@"delegate $$");
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotAfterAnonymousDelegate(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = delegate $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestNotAfterEvent()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    event $$
                """);
        }

        [Fact]
        public async Task TestNotAfterVoid()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNew()
        {
            await VerifyAbsenceAsync(
@"new $$");
        }

        [Fact]
        public async Task TestAfterNestedNew()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   new $$
                """);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInUnsafeBlock(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                unsafe {
                    $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestInUnsafeMethod()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   unsafe void Goo() {
                     $$
                """);
        }

        [Fact]
        public async Task TestInUnsafeClass()
        {
            await VerifyKeywordAsync(
                """
                unsafe class C {
                   void Goo() {
                     $$
                """);
        }

        [Fact]
        public async Task TestNotInParameter()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   void Goo($$
                """);
        }

        [Fact]
        public async Task TestInUnsafeParameter1()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   unsafe void Goo($$
                """);
        }

        [Fact]
        public async Task TestInUnsafeParameter2()
        {
            await VerifyKeywordAsync(
                """
                unsafe class C {
                   void Goo($$
                """);
        }

        [Fact]
        public async Task TestNotInCast()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   void Goo() {
                     hr = GetRealProcAddress("CompareAssemblyIdentity", ($$
                """);
        }

        [Fact]
        public async Task TestNotInCast2()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   void Goo() {
                     hr = GetRealProcAddress("CompareAssemblyIdentity", ($$**)pfnCompareAssemblyIdentity);
                """);
        }

        [Fact]
        public async Task TestInUnsafeCast()
        {
            await VerifyKeywordAsync(
                """
                unsafe class C {
                   void Goo() {
                     hr = GetRealProcAddress("CompareAssemblyIdentity", ($$
                """);
        }

        [Fact]
        public async Task TestInUnsafeCast2()
        {
            await VerifyKeywordAsync(
                """
                unsafe class C {
                   void Goo() {
                     hr = GetRealProcAddress("CompareAssemblyIdentity", ($$**)pfnCompareAssemblyIdentity);
                """);
        }

        [Fact]
        public async Task TestInUnsafeConversionOperator()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   unsafe implicit operator int(C c) {
                     $$
                """);
        }

        [Fact]
        public async Task TestInUnsafeOperator()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   unsafe int operator ++(C c) {
                     $$
                """);
        }

        [Fact]
        public async Task TestInUnsafeConstructor()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   unsafe C() {
                     $$
                """);
        }

        [Fact]
        public async Task TestInUnsafeDestructor()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   unsafe ~C() {
                     $$
                """);
        }

        [Fact]
        public async Task TestInUnsafeProperty()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   unsafe int Goo {
                     get {
                       $$
                """);
        }

        [Fact]
        public async Task TestInUnsafeIndexer()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   unsafe int this[int i] {
                     get {
                       $$
                """);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [CombinatorialData]
        public async Task TestNotInDefault(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"default($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [CombinatorialData]
        public async Task TestInSizeOf(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"sizeof($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544347")]
        public async Task TestInUnsafeDefaultExpression()
        {
            await VerifyKeywordAsync(
                """
                unsafe class C
                {
                    static void Method1(void* p1 = default($$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544347")]
        public async Task TestNotInDefaultExpression()
        {
            await VerifyAbsenceAsync(
                """
                class C
                {
                    static void Method1(void* p1 = default($$
                """);
        }

        [Fact]
        public async Task TestAfterAsync()
            => await VerifyKeywordAsync(@"class c { async $$ }");

        [Fact]
        public async Task TestAfterAsyncAsType()
            => await VerifyKeywordAsync(@"class c { async async $$ }");

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8617")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    void M()
                    {
                        $$
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8617")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/14525")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction2()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    void M()
                    {
                        async $$
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8617")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction3()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    void M()
                    {
                        async async $$
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8617")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction4()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    void M()
                    {
                        var async $$
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8617")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction5()
        {
            await VerifyAbsenceAsync("""
                using System;
                class C
                {
                    void M(Action<int> a)
                    {
                        M(async $$ () => 
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8617")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction6()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    void M()
                    {
                        unsafe async $$
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8617")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction7()
        {
            await VerifyKeywordAsync("""
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
                }
                """);
        }

        [Fact]
        public async Task TestInFunctionPointer01()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<$$
                """);
        }

        [Fact]
        public async Task TestInFunctionPointer02()
        {
            await VerifyKeywordAsync("""
                class C<T>
                {
                    C<delegate*<$$
                """);
        }

        [Fact]
        public async Task TestInFunctionPointer03()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<int, $$
                """);
        }

        [Fact]
        public async Task TestInFunctionPointer04()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<int, v$$
                """);
        }

        [Fact]
        public async Task TestInFunctionPointerTypeAfterComma()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<int, $$
                """);
        }

        [Fact]
        public async Task TestInFunctionPointerTypeAfterModifier()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<ref $$
                """);
        }

        [Fact]
        public async Task TestNotAfterDelegateAsterisk()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    delegate*$$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43295")]
        public async Task TestAfterReadonlyInStruct()
        {
            await VerifyKeywordAsync("""
                struct S
                {
                    public readonly $$
                """);
        }

        [Fact]
        public async Task TestAfterReadonlyInRecordStruct()
        {
            await VerifyKeywordAsync("""
                record struct S
                {
                    public readonly $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43295")]
        public async Task TestNotAfterReadonlyInClass()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    public readonly $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67986")]
        public async Task TestInUsingUnsafeDirective()
        {
            await VerifyKeywordAsync("using unsafe T = $$");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67986")]
        public async Task TestNotInRegularUsingDirective()
        {
            await VerifyAbsenceAsync("using T = $$");
        }
    }
}
