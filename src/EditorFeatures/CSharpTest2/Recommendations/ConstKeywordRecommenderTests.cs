// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ConstKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestAfterGlobalVariableDeclaration()
        => VerifyKeywordAsync(
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

    [Theory, CombinatorialData]
    public Task TestInEmptyStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

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

    [Theory]
    [InlineData(SourceCodeKind.Regular)]
    [InlineData(SourceCodeKind.Script, Skip = "https://github.com/dotnet/roslyn/issues/9880")]
    public Task TestNotBeforeUsing(SourceCodeKind sourceCodeKind)
        => VerifyAbsenceAsync(sourceCodeKind,
            """
            $$
            using Goo;
            """);

    [Theory]
    [InlineData(SourceCodeKind.Regular)]
    [InlineData(SourceCodeKind.Script, Skip = "https://github.com/dotnet/roslyn/issues/9880")]
    public Task TestNotBeforeGlobalUsing(SourceCodeKind sourceCodeKind)
        => VerifyAbsenceAsync(sourceCodeKind,
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
        => VerifyKeywordAsync(SourceCodeKind.Regular, """
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
        => await VerifyAbsenceAsync(@"static $$");

    [Fact]
    public Task TestNotAfterNestedStatic()
        => VerifyAbsenceAsync(
            """
            class C {
                static $$
            """);

    [Fact]
    public async Task TestNotAfterStaticPublic()
        => await VerifyAbsenceAsync(@"static public $$");

    [Fact]
    public Task TestNotAfterNestedStaticPublic()
        => VerifyAbsenceAsync(
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
    public Task TestInMethod()
        => VerifyKeywordAsync(
            """
            class C {
               void Goo() {
                 $$
            """);

    [Fact]
    public Task TestInMethodNotAfterConst()
        => VerifyAbsenceAsync(
            """
            class C {
               void Goo() {
                 const $$
            """);

    [Fact]
    public Task TestInProperty()
        => VerifyKeywordAsync(
            """
            class C {
                int Goo {
                    get {
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
            """,
            CSharpNextParseOptions,
            CSharpNextScriptParseOptions);
}
