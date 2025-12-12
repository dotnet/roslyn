// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class VolatileKeywordRecommenderTests : KeywordRecommenderTests
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
        => VerifyKeywordAsync("""
            interface I {
               $$
            """);

    [Fact]
    public Task TestNotInsideEnum()
        => VerifyAbsenceAsync("""
            enum E {
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
    public Task TestNotAfterNestedSealed()
        => VerifyAbsenceAsync(
            """
            class C {
                sealed $$
            """);

    [Fact]
    public async Task TestNotAfterStatic()
        => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"static $$");

    [Fact]
    public async Task TestAfterStatic_Interactive()
        => await VerifyKeywordAsync(SourceCodeKind.Script, @"static $$");

    [Fact]
    public Task TestAfterNestedStatic()
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
    public Task TestNotAfterConst()
        => VerifyAbsenceAsync(
            """
            class C {
                const $$
            """);

    [Fact]
    public Task TestNotAfterVolatile()
        => VerifyAbsenceAsync(
            """
            class C {
                volatile $$
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

    [Fact]
    public Task TestNotInMethod()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo() {
                    $$
            """);

    [Fact]
    public Task TestWithinExtension()
        => VerifyAbsenceAsync(
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
