// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class EventKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestAfterGlobalStatement_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
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
    public Task TestNotInEmptyStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public async Task TestNotInCompilationUnit()
        => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"$$");

    [Fact]
    public Task TestNotAfterExtern()
        => VerifyAbsenceAsync(SourceCodeKind.Regular, """
            extern alias Goo;
            $$
            """);

    [Fact]
    public Task TestAfterExtern_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script, """
            extern alias Goo;
            $$
            """);

    [Fact]
    public Task TestNotAfterUsing()
        => VerifyAbsenceAsync(SourceCodeKind.Regular, """
            using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterUsing_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script, """
            using Goo;
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalUsing()
        => VerifyAbsenceAsync(SourceCodeKind.Regular, """
            global using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterGlobalUsing_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script, """
            global using Goo;
            $$
            """);

    [Fact]
    public Task TestNotAfterNamespace()
        => VerifyAbsenceAsync(SourceCodeKind.Regular, """
            namespace N {}
            $$
            """);

    [Fact]
    public Task TestNotAfterTypeDeclaration()
        => VerifyAbsenceAsync(SourceCodeKind.Regular, """
            class C {}
            $$
            """);

    [Fact]
    public Task TestNotAfterDelegateDeclaration()
        => VerifyAbsenceAsync(SourceCodeKind.Regular, """
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
    public Task TestAfterEvent()
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
    public Task TestNotAfterAssemblyAttribute()
        => VerifyAbsenceAsync(SourceCodeKind.Regular, """
            [assembly: goo]
            $$
            """);

    [Fact]
    public Task TestAfterAssemblyAttribute_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script, """
            [assembly: goo]
            $$
            """);

    [Fact]
    public Task TestNotAfterRootAttribute()
        => VerifyAbsenceAsync(SourceCodeKind.Regular, """
            [goo]
            $$
            """);

    [Fact]
    public Task TestAfterRootAttribute_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script, """
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
    public Task TestInsideRecord()
        => VerifyWorkerAsync(
            """
            record C(int i, int j) {
               $$
            """, absent: false, options: TestOptions.RegularPreview);

    [Theory, CombinatorialData]
    public Task TestPartialMember(
        [CombinatorialValues("class", "record", "struct", "interface")] string kind)
        => VerifyKeywordAsync(
            $$"""
            {{kind}} C {
               partial $$
            """);

    [Fact]
    public async Task TestNotAfterPartial()
        => await VerifyAbsenceAsync(@"partial $$");

    [Fact]
    public async Task TestNotAfterAbstract()
        => await VerifyAbsenceAsync(@"abstract $$");

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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543975")]
    public Task TestAfterUnsafe()
        => VerifyKeywordAsync(
            """
            class C {
                unsafe $$
            """);

    [Fact]
    public async Task TestNotAfterStatic()
        => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"static $$");

    [Fact]
    public async Task TestAfterStatic_Interactive()
        => await VerifyKeywordAsync(SourceCodeKind.Script, @"static $$");

    [Fact]
    public Task TestAfterStatic()
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
    public async Task TestNotAfterDelegate()
        => await VerifyAbsenceAsync(@"delegate $$");

    [Fact]
    public Task TestNotAfterEvent()
        => VerifyAbsenceAsync(
            """
            class C {
                event $$
            """);

    [Fact]
    public Task TestNotAfterNested()
        => VerifyAbsenceAsync(
@"new $$");

    [Fact]
    public Task TestAfterNestedNew()
        => VerifyKeywordAsync(
            """
            class C {
               new $$
            """);

    [Fact]
    public Task TestInAttributeInsideClass()
        => VerifyKeywordAsync(
            """
            class C {
                [$$
            """);

    [Fact]
    public Task TestInAttributeAfterAttributeInsideClass()
        => VerifyKeywordAsync(
            """
            class C {
                [Goo]
                [$$
            """);

    [Fact]
    public Task TestInAttributeAfterMethod()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo() {
                }
                [$$
            """);

    [Fact]
    public Task TestInAttributeAfterProperty()
        => VerifyKeywordAsync(
            """
            class C {
                int Goo {
                    get;
                }
                [$$
            """);

    [Fact]
    public Task TestInAttributeAfterField()
        => VerifyKeywordAsync(
            """
            class C {
                int Goo;
                [$$
            """);

    [Fact]
    public Task TestInAttributeAfterEvent()
        => VerifyKeywordAsync(
            """
            class C {
                event Action<int> Goo;
                [$$
            """);

    [Fact]
    public Task TestNotInOuterAttribute()
        => VerifyAbsenceAsync(
@"[$$");

    [Fact]
    public Task TestNotInParameterAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo([$$
            """);

    [Fact]
    public Task TestNotInPropertyAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo { [$$
            """);

    [Fact]
    public Task TestNotInEventAttribute()
        => VerifyAbsenceAsync(
            """
            class C {
                event Action<int> Goo { [$$
            """);

    [Fact]
    public Task TestNotInTypeParameters()
        => VerifyAbsenceAsync(
@"class C<[$$");

    [Fact]
    public Task TestInInterface()
        => VerifyKeywordAsync(
            """
            interface I {
                [$$
            """);

    [Fact]
    public Task TestInStruct()
        => VerifyKeywordAsync(
            """
            struct S {
                [$$
            """);

    [Fact]
    public Task TestNotInEnum()
        => VerifyAbsenceAsync(
            """
            enum E {
                [$$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68399")]
    public Task TestNotInRecordParameterAttribute()
        => VerifyAbsenceAsync(
            """
            record R([$$] int i) { }
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
                """,
            CSharpNextParseOptions,
            CSharpNextScriptParseOptions);
}
