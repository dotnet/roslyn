// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class VoidKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAtRoot()
        => VerifyKeywordAsync(
@"$$");

    [Fact]
    public Task TestAfterClass()
        => VerifyKeywordAsync(
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
    public Task TestNotAfterStackAlloc()
        => VerifyAbsenceAsync(
            """
            class C {
                 int* goo = stackalloc $$
            """);

    [Fact]
    public Task TestInFixedStatement()
        => VerifyKeywordAsync(
@"fixed ($$");

    [Fact]
    public Task TestInDelegateReturnType()
        => VerifyKeywordAsync(
@"public delegate $$");

    [Theory, CombinatorialData]
    public Task TestNotInCastType(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"var str = (($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotInCastType2(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"var str = (($$)items) as string;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInTypeOf(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"typeof($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestAfterExtern()
        => VerifyKeywordAsync("""
            extern alias Goo;
            $$
            """);

    [Fact]
    public Task TestAfterUsing()
        => VerifyKeywordAsync("""
            using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterGlobalUsing()
        => VerifyKeywordAsync("""
            global using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterNamespace()
        => VerifyKeywordAsync("""
            namespace N {}
            $$
            """);

    [Fact]
    public Task TestAfterDelegateDeclaration()
        => VerifyKeywordAsync("""
            delegate void Goo();
            $$
            """);

    [Fact]
    public Task TestAfterMethod()
        => VerifyKeywordAsync(
            """
            class C {
              void Goo() {}
              $$
            """);

    [Fact]
    public Task TestAfterField()
        => VerifyKeywordAsync(
            """
            class C {
              int i;
              $$
            """);

    [Fact]
    public Task TestAfterProperty()
        => VerifyKeywordAsync(
            """
            class C {
              int i { get; }
              $$
            """);

    [Fact]
    public Task TestNotBeforeUsing()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            $$
            using Goo;
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
    public Task TestNotBeforeUsing_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            $$
            using Goo;
            """);

    [Fact]
    public Task TestNotBeforeGlobalUsing()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            $$
            global using Goo;
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
    public Task TestNotBeforeGlobalUsing_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            $$
            global using Goo;
            """);

    [Fact]
    public Task TestAfterAssemblyAttribute()
        => VerifyKeywordAsync("""
            [assembly: goo]
            $$
            """);

    [Fact]
    public Task TestAfterRootAttribute()
        => VerifyKeywordAsync("""
            [goo]
            $$
            """);

    [Fact]
    public Task TestAfterMultipleRootAttributes()
        => VerifyKeywordAsync("""
            [goo][goo]
            $$
            """);

    [Fact]
    public Task TestAfterNestedAttribute()
        => VerifyKeywordAsync(
            """
            class C {
              [goo]
              $$
            """);

    [Fact]
    public Task TestInsideStruct()
        => VerifyKeywordAsync(
            """
            struct S {
               $$
            """);

    [Fact]
    public Task TestInsideInterface()
        => VerifyKeywordAsync(
            """
            interface I {
               $$
            """);

    [Fact]
    public Task TestInsideClass()
        => VerifyKeywordAsync(
            """
            class C {
               $$
            """);

    [Fact]
    public async Task TestNotAfterPartial()
        => await VerifyAbsenceAsync(@"partial $$");

    [Fact]
    public Task TestAfterNestedPartial()
        => VerifyKeywordAsync(
            """
            class C {
                partial $$
            """);

    [Fact]
    public async Task TestNotAfterAbstract()
        => await VerifyAbsenceAsync(@"abstract $$");

    [Fact]
    public Task TestAfterNestedAbstract()
        => VerifyKeywordAsync(
            """
            class C {
                abstract $$
            """);

    [Fact]
    public async Task TestNotAfterInternal()
        => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"internal $$");

    [Fact]
    public async Task TestAfterInternal_Interactive()
        => await VerifyKeywordAsync(SourceCodeKind.Script, @"internal $$");

    [Fact]
    public Task TestAfterNestedInternal()
        => VerifyKeywordAsync(
            """
            class C {
                internal $$
            """);

    [Fact]
    public async Task TestNotAfterPublic()
        => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"public $$");

    [Fact]
    public async Task TestAfterPublic_Interactive()
        => await VerifyKeywordAsync(SourceCodeKind.Script, @"public $$");

    [Fact]
    public Task TestAfterNestedPublic()
        => VerifyKeywordAsync(
            """
            class C {
                public $$
            """);

    [Fact]
    public Task TestNotAfterPrivate()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
@"private $$");

    [Fact]
    public Task TestAfterPrivate_Script()
        => VerifyKeywordAsync(SourceCodeKind.Script,
@"private $$");

    [Fact]
    public Task TestAfterNestedPrivate()
        => VerifyKeywordAsync(
            """
            class C {
                private $$
            """);

    [Fact]
    public Task TestNotAfterProtected()
        => VerifyAbsenceAsync(
@"protected $$");

    [Fact]
    public Task TestAfterNestedProtected()
        => VerifyKeywordAsync(
            """
            class C {
                protected $$
            """);

    [Fact]
    public async Task TestNotAfterSealed()
        => await VerifyAbsenceAsync(@"sealed $$");

    [Fact]
    public Task TestAfterNestedSealed()
        => VerifyKeywordAsync(
            """
            class C {
                sealed $$
            """);

    [Fact]
    public async Task TestAfterStatic()
        => await VerifyKeywordAsync(@"static $$");

    [Fact]
    public Task TestAfterStaticInClass()
        => VerifyKeywordAsync(
            """
            class C {
                static $$
            """);

    [Fact]
    public async Task TestNotAfterStaticPublic()
        => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"static public $$");

    [Fact]
    public async Task TestAfterStaticPublic_Interactive()
        => await VerifyKeywordAsync(SourceCodeKind.Script, @"static public $$");

    [Fact]
    public Task TestAfterNestedStaticPublic()
        => VerifyKeywordAsync(
            """
            class C {
                static public $$
            """);

    [Fact]
    public Task TestAfterDelegate()
        => VerifyKeywordAsync(
@"delegate $$");

    [Theory, CombinatorialData]
    public Task TestNotAfterAnonymousDelegate(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"var q = delegate $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotAfterEvent()
        => VerifyAbsenceAsync(
            """
            class C {
                event $$
            """);

    [Fact]
    public Task TestNotAfterVoid()
        => VerifyAbsenceAsync(
            """
            class C {
                void $$
            """);

    [Fact]
    public Task TestNotAfterNew()
        => VerifyAbsenceAsync(
@"new $$");

    [Fact]
    public Task TestAfterNestedNew()
        => VerifyKeywordAsync(
            """
            class C {
               new $$
            """);

    [Theory, CombinatorialData]
    public Task TestInUnsafeBlock(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            unsafe {
                $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestInUnsafeMethod()
        => VerifyKeywordAsync(
            """
            class C {
               unsafe void Goo() {
                 $$
            """);

    [Fact]
    public Task TestInUnsafeClass()
        => VerifyKeywordAsync(
            """
            unsafe class C {
               void Goo() {
                 $$
            """);

    [Fact]
    public Task TestNotInParameter()
        => VerifyAbsenceAsync(
            """
            class C {
               void Goo($$
            """);

    [Fact]
    public Task TestInUnsafeParameter1()
        => VerifyKeywordAsync(
            """
            class C {
               unsafe void Goo($$
            """);

    [Fact]
    public Task TestInUnsafeParameter2()
        => VerifyKeywordAsync(
            """
            unsafe class C {
               void Goo($$
            """);

    [Fact]
    public Task TestNotInCast()
        => VerifyAbsenceAsync(
            """
            class C {
               void Goo() {
                 hr = GetRealProcAddress("CompareAssemblyIdentity", ($$
            """);

    [Fact]
    public Task TestNotInCast2()
        => VerifyAbsenceAsync(
            """
            class C {
               void Goo() {
                 hr = GetRealProcAddress("CompareAssemblyIdentity", ($$**)pfnCompareAssemblyIdentity);
            """);

    [Fact]
    public Task TestInUnsafeCast()
        => VerifyKeywordAsync(
            """
            unsafe class C {
               void Goo() {
                 hr = GetRealProcAddress("CompareAssemblyIdentity", ($$
            """);

    [Fact]
    public Task TestInUnsafeCast2()
        => VerifyKeywordAsync(
            """
            unsafe class C {
               void Goo() {
                 hr = GetRealProcAddress("CompareAssemblyIdentity", ($$**)pfnCompareAssemblyIdentity);
            """);

    [Fact]
    public Task TestInUnsafeConversionOperator()
        => VerifyKeywordAsync(
            """
            class C {
               unsafe implicit operator int(C c) {
                 $$
            """);

    [Fact]
    public Task TestInUnsafeOperator()
        => VerifyKeywordAsync(
            """
            class C {
               unsafe int operator ++(C c) {
                 $$
            """);

    [Fact]
    public Task TestInUnsafeConstructor()
        => VerifyKeywordAsync(
            """
            class C {
               unsafe C() {
                 $$
            """);

    [Fact]
    public Task TestInUnsafeDestructor()
        => VerifyKeywordAsync(
            """
            class C {
               unsafe ~C() {
                 $$
            """);

    [Fact]
    public Task TestInUnsafeProperty()
        => VerifyKeywordAsync(
            """
            class C {
               unsafe int Goo {
                 get {
                   $$
            """);

    [Fact]
    public Task TestInUnsafeIndexer()
        => VerifyKeywordAsync(
            """
            class C {
               unsafe int this[int i] {
                 get {
                   $$
            """);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    [CombinatorialData]
    public Task TestNotInDefault(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"default($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    [CombinatorialData]
    public Task TestInSizeOf(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"sizeof($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544347")]
    public Task TestInUnsafeDefaultExpression()
        => VerifyKeywordAsync(
            """
            unsafe class C
            {
                static void Method1(void* p1 = default($$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544347")]
    public Task TestNotInDefaultExpression()
        => VerifyAbsenceAsync(
            """
            class C
            {
                static void Method1(void* p1 = default($$
            """);

    [Fact]
    public async Task TestAfterAsync()
        => await VerifyKeywordAsync(@"class c { async $$ }");

    [Fact]
    public async Task TestAfterAsyncAsType()
        => await VerifyKeywordAsync(@"class c { async async $$ }");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8617")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task TestLocalFunction()
        => VerifyKeywordAsync("""
            class C
            {
                void M()
                {
                    $$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8617")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/14525")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task TestLocalFunction2()
        => VerifyKeywordAsync("""
            class C
            {
                void M()
                {
                    async $$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8617")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task TestLocalFunction3()
        => VerifyKeywordAsync("""
            class C
            {
                void M()
                {
                    async async $$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8617")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task TestLocalFunction4()
        => VerifyAbsenceAsync("""
            class C
            {
                void M()
                {
                    var async $$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8617")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task TestLocalFunction5()
        => VerifyAbsenceAsync("""
            using System;
            class C
            {
                void M(Action<int> a)
                {
                    M(async $$ () => 
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8617")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task TestLocalFunction6()
        => VerifyKeywordAsync("""
            class C
            {
                void M()
                {
                    unsafe async $$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8617")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task TestLocalFunction7()
        => VerifyKeywordAsync("""
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

    [Fact]
    public Task TestInFunctionPointer01()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<$$
            """);

    [Fact]
    public Task TestInFunctionPointer02()
        => VerifyKeywordAsync("""
            class C<T>
            {
                C<delegate*<$$
            """);

    [Fact]
    public Task TestInFunctionPointer03()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<int, $$
            """);

    [Fact]
    public Task TestInFunctionPointer04()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<int, v$$
            """);

    [Fact]
    public Task TestInFunctionPointerTypeAfterComma()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<int, $$
            """);

    [Fact]
    public Task TestInFunctionPointerTypeAfterModifier()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<ref $$
            """);

    [Fact]
    public Task TestNotAfterDelegateAsterisk()
        => VerifyAbsenceAsync("""
            class C
            {
                delegate*$$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43295")]
    public Task TestAfterReadonlyInStruct()
        => VerifyKeywordAsync("""
            struct S
            {
                public readonly $$
            """);

    [Fact]
    public Task TestAfterReadonlyInRecordStruct()
        => VerifyKeywordAsync("""
            record struct S
            {
                public readonly $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43295")]
    public Task TestNotAfterReadonlyInClass()
        => VerifyAbsenceAsync("""
            class C
            {
                public readonly $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67986")]
    public Task TestInUsingUnsafeDirective()
        => VerifyKeywordAsync("using unsafe T = $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67986")]
    public Task TestNotInRegularUsingDirective()
        => VerifyAbsenceAsync("using T = $$");

    [Fact]
    public Task TestWithinExtension()
        => VerifyKeywordAsync(
            """
            static class C
            {
                extension(string s)
                {
                    $$
                }
            }
            """, CSharpNextParseOptions);
}
