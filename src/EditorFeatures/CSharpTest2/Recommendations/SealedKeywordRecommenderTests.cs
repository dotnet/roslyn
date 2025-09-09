// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class SealedKeywordRecommenderTests : KeywordRecommenderTests
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

    [Fact]
    public Task TestNotInEmptyStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
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
    public Task TestAfterMethodInClass()
        => VerifyKeywordAsync(
            """
            class C {
              void Goo() {}
              $$
            """);

    [Fact]
    public Task TestAfterFieldInClass()
        => VerifyKeywordAsync(
            """
            class C {
              int i;
              $$
            """);

    [Fact]
    public Task TestAfterPropertyInClass()
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
    public async Task TestNotAfterStaticInternal()
        => await VerifyAbsenceAsync(@"static internal $$");

    [Fact]
    public async Task TestNotAfterInternalStatic()
        => await VerifyAbsenceAsync(@"internal static $$");

    [Fact]
    public async Task TestNotAfterInvalidInternal()
        => await VerifyAbsenceAsync(@"virtual internal $$");

    [Fact]
    public async Task TestNotAfterClass()
        => await VerifyAbsenceAsync(@"class $$");

    [Fact]
    public Task TestAfterPrivate()
        => VerifyKeywordAsync(
@"private $$");

    [Fact]
    public async Task TestNotAfterSealed()
        => await VerifyAbsenceAsync(@"sealed $$");

    [Fact]
    public async Task TestNotAfterStatic()
        => await VerifyAbsenceAsync(@"static $$");

    [Theory, CombinatorialData]
    public Task TestNotAfterNestedStatic([CombinatorialValues("class", "struct", "record", "record struct", "record class")] string declarationKind)
        => VerifyAbsenceAsync(declarationKind + """
            C {
               static $$
            """);

    [Fact]
    public Task TestAfterNestedStaticInInterface()
        => VerifyKeywordAsync("""
            interface C {
                static $$
            """);

    [Fact]
    public Task TestAfterNestedInternal()
        => VerifyKeywordAsync(
            """
            class C {
                internal $$
            """);

    [Fact]
    public async Task TestNotAfterDelegate()
        => await VerifyAbsenceAsync(@"delegate $$");

    [Theory, CombinatorialData]
    public Task TestNotAfterNestedAbstract([CombinatorialValues("class", "struct", "record", "record struct", "record class", "interface")] string declarationKind)
        => VerifyAbsenceAsync(declarationKind + """
            C {
               abstract $$
            """);

    [Theory, CombinatorialData]
    public Task TestNotAfterNestedVirtual([CombinatorialValues("class", "struct", "record", "record struct", "record class", "interface")] string declarationKind)
        => VerifyAbsenceAsync(declarationKind + """
            C {
               virtual $$
            """);

    [Fact]
    public Task TestAfterNestedOverride()
        => VerifyKeywordAsync(
            """
            class C {
                override $$
            """);

    [Theory, CombinatorialData]
    public Task TestNotAfterNestedSealed([CombinatorialValues("class", "struct", "record", "record struct", "record class", "interface")] string declarationKind)
        => VerifyAbsenceAsync(declarationKind + """
            C {
               sealed $$
            """);

    [Fact]
    public Task TestNotInProperty()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo { $$
            """);

    [Fact]
    public Task TestNotInPropertyAfterAccessor()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo { get; $$
            """);

    [Fact]
    public Task TestNotInPropertyAfterAccessibility()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo { get; protected $$
            """);

    [Fact]
    public Task TestNotInPropertyAfterInternal()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo { get; internal $$
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
