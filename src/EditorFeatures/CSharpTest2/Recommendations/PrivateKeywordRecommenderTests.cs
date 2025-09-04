// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class PrivateKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestNotAfterFileScopedNamespace()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            namespace N;
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
        => VerifyAbsenceAsync(SourceCodeKind.Regular, """
            $$
            using Goo;
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
    public Task TestNotBeforeUsing_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script, """
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

    // You can have an abstract private class.
    [Fact]
    public Task TestAfterNestedAbstract()
        => VerifyKeywordAsync(
            """
            class C {
                abstract $$
            """);

    [Fact]
    public async Task TestNotAfterInternal()
        => await VerifyAbsenceAsync(@"internal $$");

    [Fact]
    public async Task TestNotAfterPublic()
        => await VerifyAbsenceAsync(@"public $$");

    [Fact]
    public async Task TestNotAfterStaticPrivate()
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
    public async Task TestNotAfterPrivate()
        => await VerifyAbsenceAsync(@"private $$");

    [Fact]
    public async Task TestNotAfterProtected()
        => await VerifyAbsenceAsync(@"protected $$");

    [Fact]
    public async Task TestNotAfterSealed()
        => await VerifyAbsenceAsync(@"sealed $$");

    // You can have a 'sealed private class'.
    [Fact]
    public Task TestAfterNestedSealed()
        => VerifyKeywordAsync(
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
    public async Task TestNotAfterDelegate()
        => await VerifyAbsenceAsync(@"delegate $$");

    [Fact]
    public Task TestNotAfterNestedVirtual()
        => VerifyAbsenceAsync("""
            class C {
                virtual $$
            """);

    [Fact]
    public Task TestNotAfterNestedOverride()
        => VerifyAbsenceAsync("""
            class C {
                override $$
            """);

    [Fact]
    public Task TestInProperty()
        => VerifyKeywordAsync(
            """
            class C {
                int Goo { $$
            """);

    [Fact]
    public Task TestInPropertyAfterAccessor()
        => VerifyKeywordAsync(
            """
            class C {
                int Goo { get; $$
            """);

    [Fact]
    public Task TestNotInPropertyAfterAccessibility()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo { get; private $$
            """);

    [Fact]
    public Task TestAfterRegion()
        => VerifyKeywordAsync(
            """
            class C {
            #region Interop stuff
                $$
            """);

    [Fact]
    public Task TestAfterTypeWithSemicolon()
        => VerifyKeywordAsync(
            """
            class C {
                private enum PageAccess : int { PAGE_READONLY = 0x02 };
                $$
            """);

    [Fact]
    public Task TestInIndexer()
        => VerifyKeywordAsync(
            """
            class C {
                int this[int i] { $$
            """);

    [Fact]
    public Task TestInIndexerAfterAccessor()
        => VerifyKeywordAsync(
            """
            class C {
                int this[int i] { get { } $$
            """);

    [Fact]
    public Task TestNotInIndexerAfterPrivateAccessibility()
        => VerifyAbsenceAsync(
            """
            class C {
                int this[int i] { get { } private $$
            """);

    [Fact]
    public Task TestNotInIndexerAfterProtectedAccessibility()
        => VerifyKeywordAsync(
            """
            class C {
                int this[int i] { get { } protected $$
            """);

    [Fact]
    public Task TestNotInIndexerAfterInternalAccessibility()
        => VerifyAbsenceAsync(
            """
            class C {
                int this[int i] { get { } internal $$
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
