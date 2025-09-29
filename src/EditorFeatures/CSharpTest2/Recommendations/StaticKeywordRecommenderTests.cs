// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class StaticKeywordRecommenderTests : KeywordRecommenderTests
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32174")]
    public Task TestInEmptyStatement()
        => VerifyKeywordAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestInCompilationUnit()
        => VerifyKeywordAsync(
@"$$");

    [Fact]
    public Task TestAfterExtern()
        => VerifyKeywordAsync(
            """
            extern alias Goo;
            $$
            """);

    [Fact]
    public Task TestAfterUsing()
        => VerifyKeywordAsync(
            """
            using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterGlobalUsing()
        => VerifyKeywordAsync(
            """
            global using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterNamespace()
        => VerifyKeywordAsync(
            """
            namespace N {}
            $$
            """);

    [Fact]
    public Task TestAfterFileScopedNamespace()
        => VerifyKeywordAsync(
            """
            namespace N;
            $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
    public Task TestFileKeywordInsideNamespace()
        => VerifyKeywordAsync(
            """
            namespace N {
            file $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
    public Task TestFileKeywordInsideNamespaceBeforeClass()
        => VerifyKeywordAsync(
            """
            namespace N {
            file $$
            class C {}
            }
            """);

    [Fact]
    public Task TestAfterTypeDeclaration()
        => VerifyKeywordAsync(
            """
            class C {}
            $$
            """);

    [Fact]
    public Task TestAfterDelegateDeclaration()
        => VerifyKeywordAsync(
            """
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
        => VerifyKeywordAsync(
            """
            [assembly: goo]
            $$
            """);

    [Fact]
    public Task TestAfterRootAttribute()
        => VerifyKeywordAsync(
            """
            [goo]
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

    // This will be fixed once we have accessibility for members
    [Fact]
    public Task TestInsideStruct()
        => VerifyKeywordAsync(
            """
            struct S {
               $$
            """);

    [Fact]
    public Task TestInsideInterface()
        => VerifyKeywordAsync("""
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
    public async Task TestNotAfterAbstract()
        => await VerifyAbsenceAsync(@"abstract $$");

    [Fact]
    public Task TestAfterInternal()
        => VerifyKeywordAsync(
@"internal $$");

    [Fact]
    public Task TestAfterPublic()
        => VerifyKeywordAsync(
@"public $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
    public Task TestAfterFile()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
@"file $$");

    [Fact]
    public async Task TestNotAfterStaticPublic()
        => await VerifyAbsenceAsync(@"static public $$");

    [Fact]
    public async Task TestNotAfterPublicStatic()
        => await VerifyAbsenceAsync(@"public static $$");

    [Fact]
    public async Task TestNotAfterVirtualPublic()
        => await VerifyAbsenceAsync(@"virtual public $$");

    [Fact]
    public Task TestAfterPrivate()
        => VerifyKeywordAsync(
@"private $$");

    [Fact]
    public Task TestAfterProtected()
        => VerifyKeywordAsync(
@"protected $$");

    [Fact]
    public async Task TestNotAfterSealed()
        => await VerifyAbsenceAsync(@"sealed $$");

    [Fact]
    public async Task TestNotAfterStatic()
        => await VerifyAbsenceAsync(@"static $$");

    [Fact]
    public async Task TestNotAfterClass()
        => await VerifyAbsenceAsync(@"class $$");

    [Fact]
    public async Task TestNotAfterDelegate()
        => await VerifyAbsenceAsync(@"delegate $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
    public Task TestNotBetweenUsings()
        => VerifyWorkerAsync("""
            using Goo;
            $$
            using Bar;
            """, absent: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
    public Task TestNotBetweenGlobalUsings_01()
        => VerifyWorkerAsync("""
            global using Goo;
            $$
            using Bar;
            """, absent: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
    public Task TestNotBetweenGlobalUsings_02()
        => VerifyWorkerAsync("""
            global using Goo;
            $$
            global using Bar;
            """, absent: true);

    [Theory, CombinatorialData]
    public Task TestNotAfterNestedAbstract([CombinatorialValues("class", "struct", "record", "record struct", "record class")] string declarationKind)
        => VerifyAbsenceAsync(declarationKind + """
            C {
               abstract $$
            """);

    [Fact]
    public Task TestAfterNestedAbstractInInterface()
        => VerifyKeywordAsync("""
            interface C {
                abstract $$
            """);

    [Theory, CombinatorialData]
    public Task TestNotAfterNestedVirtual([CombinatorialValues("class", "struct", "record", "record struct", "record class")] string declarationKind)
        => VerifyAbsenceAsync(declarationKind + """
            C {
               virtual $$
            """);

    [Fact]
    public Task TestAfterNestedVirtualInInterface()
        => VerifyKeywordAsync("""
            interface C {
                virtual $$
            """);

    [Theory, CombinatorialData]
    public Task TestNotAfterNestedOverride([CombinatorialValues("class", "struct", "record", "record struct", "record class", "interface")] string declarationKind)
        => VerifyAbsenceAsync(declarationKind + """
            C {
               override $$
            """);

    [Theory, CombinatorialData]
    public Task TestNotAfterNestedStatic([CombinatorialValues("class", "struct", "record", "record struct", "record class", "interface")] string declarationKind)
        => VerifyAbsenceAsync(declarationKind + """
            C {
               static $$
            """);

    [Theory, CombinatorialData]
    public Task TestNotAfterNestedSealed([CombinatorialValues("class", "struct", "record", "record struct", "record class")] string declarationKind)
        => VerifyAbsenceAsync(declarationKind + """
            C {
               sealed $$
            """);

    [Fact]
    public Task TestAfterNestedSealedInInterface()
        => VerifyKeywordAsync("""
            interface C {
                sealed $$
            """);

    [Fact]
    public Task TestAfterNestedReadOnly()
        => VerifyKeywordAsync(
            """
            class C {
                readonly $$
            """);

    [Fact]
    public Task TestAfterAsync()
        => VerifyKeywordAsync(
            """
            class C {
                async $$
            """);

    [Fact]
    public Task TestAfterUsingInCompilationUnit()
        => VerifyKeywordAsync(
@"using $$");

    [Fact]
    public Task TestAfterGlobalUsingInCompilationUnit()
        => VerifyKeywordAsync(
@"global using $$");

    [Fact]
    public Task TestNotAfterUsingInMethodBody()
        => VerifyAbsenceAsync(
            """
            class C {
                void M() {
                    using $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32174")]
    public async Task TestLocalFunction()
        => await VerifyKeywordAsync(AddInsideMethod(@" $$ void local() { }"));

    [Fact]
    public Task TestInCase()
        => VerifyKeywordAsync(AddInsideMethod("""
            switch (i)
            {
                case 0:
                    $$
            """));

    [Fact]
    public Task TestInAssignment()
        => VerifyKeywordAsync(AddInsideMethod("""
            System.Action x = $$
            """));

    [Fact]
    public Task TestBeforeLambdaInAssignment()
        => VerifyKeywordAsync(AddInsideMethod("""
            System.Action x = $$ (x) => { }
            """));

    [Fact]
    public Task TestBeforeAnonymousMethodInAssignment()
        => VerifyKeywordAsync(AddInsideMethod("""
            System.Action x = $$ delegate(x) { }
            """));

    [Fact]
    public Task TestAfterAsyncInAssignment()
        => VerifyKeywordAsync(AddInsideMethod("""
            System.Action x = async $$
            """));

    [Fact]
    public Task TestBeforeAsyncInAssignment()
        => VerifyKeywordAsync(AddInsideMethod("""
            System.Action x = $$ async
            """));

    [Fact]
    public Task TestBeforeAsyncLambdaInAssignment()
        => VerifyKeywordAsync(AddInsideMethod("""
            System.Action x = $$ async (x) => { }
            """));

    [Fact]
    public Task TestAfterAsyncBeforeLambdaInAssignment()
        => VerifyKeywordAsync(AddInsideMethod("""
            System.Action x = async $$ (x) => { }
            """));

    [Fact]
    public Task TestAfterAsyncLambdaParamInAssignment()
        => VerifyKeywordAsync(AddInsideMethod("""
            System.Action x = async async $$ (x) => { }
            """));

    [Fact]
    public Task TestInCall()
        => VerifyKeywordAsync(AddInsideMethod("""
            M($$
            """));

    [Fact]
    public Task TestInIndexer()
        => VerifyKeywordAsync(AddInsideMethod("""
            this[$$
            """));

    [Fact]
    public Task TestInCallAfterArgumentLabel()
        => VerifyKeywordAsync(AddInsideMethod("""
            M(param: $$
            """));

    [Fact]
    public Task TestInCallAfterRef()
        => VerifyAbsenceAsync(AddInsideMethod("""
            M(ref $$
            """));

    [Fact]
    public Task TestInCallAfterIn()
        => VerifyAbsenceAsync(AddInsideMethod("""
            M(in $$
            """));

    [Fact]
    public Task TestInCallAfterOut()
        => VerifyAbsenceAsync(AddInsideMethod("""
            M(in $$
            """));

    [Fact]
    public Task TestInAttribute()
        => VerifyAbsenceAsync("""
            class C
            {
                [$$
                void M()
                {
                }
            }
            """);

    [Fact]
    public Task TestInAttributeArgument()
        => VerifyAbsenceAsync("""
            class C
            {
                [Attr($$
                void M()
                {
                }
            }
            """);

    [Fact]
    public async Task TestInFor()
        => await VerifyKeywordAsync(AddInsideMethod(@" for (int i = 0; i < 0; $$) "));

    [Fact]
    public Task TestAfterUsingKeywordBeforeTopLevelStatement()
        => VerifyKeywordAsync("""
            using $$
            var i = 1;
            """);

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
