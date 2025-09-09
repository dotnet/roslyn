// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class UnsafeKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestAfterAbstract()
        => VerifyKeywordAsync(
@"abstract $$");

    [Fact]
    public Task TestAfterInternal()
        => VerifyKeywordAsync(
@"internal $$");

    [Fact]
    public async Task TestNotAfterUnsafe()
        => await VerifyAbsenceAsync(@"unsafe $$");

    [Fact]
    public async Task TestNotAfterStaticUnsafe()
        => await VerifyAbsenceAsync(@"static unsafe $$");

    [Fact]
    public async Task TestNotAfterUnsafeStatic()
        => await VerifyAbsenceAsync(@"unsafe static $$");

    [Fact]
    public Task TestAfterPrivate()
        => VerifyKeywordAsync(
@"private $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
    public Task TestAfterFile()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
@"file $$");

    [Fact]
    public Task TestAfterProtected()
        => VerifyKeywordAsync(
@"protected $$");

    [Fact]
    public Task TestAfterSealed()
        => VerifyKeywordAsync(
@"sealed $$");

    [Fact]
    public Task TestAfterStatic()
        => VerifyKeywordAsync(
@"static $$");

    [Fact]
    public async Task TestNotAfterClass()
        => await VerifyAbsenceAsync(@"class $$");

    [Fact]
    public async Task TestNotAfterDelegate()
        => await VerifyAbsenceAsync(@"delegate $$");

    [Fact]
    public Task TestAfterNestedAbstract()
        => VerifyKeywordAsync(
            """
            class C {
                abstract $$
            """);

    [Fact]
    public Task TestAfterNestedVirtual()
        => VerifyKeywordAsync(
            """
            class C {
                virtual $$
            """);

    [Fact]
    public Task TestAfterNestedOverride()
        => VerifyKeywordAsync(
            """
            class C {
                override $$
            """);

    [Fact]
    public Task TestAfterNestedSealed()
        => VerifyKeywordAsync(
            """
            class C {
                sealed $$
            """);

    [Fact]
    public Task TestBeforeStatement()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            $$
            return true;
            """));

    [Fact]
    public Task TestAfterStatement()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            return true;
            $$
            """));

    [Fact]
    public Task TestAfterBlock()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            if (true) {
            }
            $$
            """));

    [Fact]
    public Task TestInsideSwitchBlock()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (E) {
              case 0:
                $$
            """));

    [Fact]
    public Task TestNotAfterUnsafe_InMethod()
        => VerifyAbsenceAsync(AddInsideMethod(
@"unsafe $$"));

    [Fact]
    public Task TestAfterInterfaceModifiers()
        => VerifyKeywordAsync(
@"public $$ interface IBinaryDocumentMemoryBlock {");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67984")]
    public Task TestAfterUsingKeywordInUsingDirective()
        => VerifyKeywordAsync("using $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67984")]
    public Task TestAfterStaticKeywordInUsingDirective()
        => VerifyKeywordAsync("using static $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67984")]
    public Task TestAfterUsingKeywordInGlobalUsingDirective()
        => VerifyKeywordAsync("global using $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67984")]
    public Task TestAfterStaticKeywordInGlobalUsingDirective()
        => VerifyKeywordAsync("global using static $$");

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
