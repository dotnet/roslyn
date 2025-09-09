// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ClassKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestAfterPartial()
        => VerifyKeywordAsync(
@"partial $$");

    [Fact]
    public Task TestAfterAbstract()
        => VerifyKeywordAsync(
@"abstract $$");

    [Fact]
    public Task TestAfterInternal()
        => VerifyKeywordAsync(
@"internal $$");

    [Fact]
    public Task TestAfterStaticPublic()
        => VerifyKeywordAsync(
@"static public $$");

    [Fact]
    public Task TestAfterPublicStatic()
        => VerifyKeywordAsync(
@"public static $$");

    [Fact]
    public async Task TestNotAfterInvalidPublic()
        => await VerifyAbsenceAsync(@"virtual public $$");

    [Fact]
    public Task TestAfterPublic()
        => VerifyKeywordAsync(
@"public $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
    public Task TestAfterFile()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
@"file $$");

    [Fact]
    public Task TestAfterPrivate()
        => VerifyKeywordAsync(
@"private $$");

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
    public Task TestNotAfterStaticInUsingDirective()
        => VerifyAbsenceAsync(
@"using static $$");

    [Fact]
    public Task TestNotAfterStaticInGlobalUsingDirective()
        => VerifyAbsenceAsync(
@"global using static $$");

    [Fact]
    public async Task TestNotAfterClass()
        => await VerifyAbsenceAsync(@"class $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
    public Task TestNotBetweenUsings()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            using Goo;
            $$
            using Bar;
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
    public Task TestNotBetweenGlobalUsings_01()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            global using Goo;
            $$
            using Bar;
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
    public Task TestNotBetweenGlobalUsings_02()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            global using Goo;
            $$
            global using Bar;
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TestAfterClassTypeParameterConstraint()
        => VerifyKeywordAsync(
@"class C<T> where T : $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TestNotAfterClassTypeParameterConstraintWhenNotDirectlyInConstraint()
        => VerifyAbsenceAsync(
@"class C<T> where T : IList<$$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TestAfterClassTypeParameterConstraint2()
        => VerifyKeywordAsync(
            """
            class C<T>
                where T : $$
                where U : U
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TestNotAfterClassTypeParameterConstraintWhenNotDirectlyInConstraint2()
        => VerifyAbsenceAsync(
            """
            class C<T>
                where T : IList<$$
                where U : U
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TestAfterMethodTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo<T>()
                  where T : $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TestNotAfterMethodTypeParameterConstraintWhenNotDirectlyInConstraint()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo<T>()
                  where T : IList<$$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TestAfterMethodTypeParameterConstraint2()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo<T>()
                  where T : $$
                  where U : T
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
    public Task TestNotAfterMethodTypeParameterConstraintWhenNotDirectlyInConstraint2()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo<T>()
                  where T : IList<$$
                  where U : T
            """);

    [Fact]
    public Task TestAfterNew()
        => VerifyKeywordAsync(
            """
            class C {
                new $$
            """);

    [Fact]
    public Task TestAfterRecord()
        => VerifyKeywordAsync(
@"record $$");

    [Fact]
    public Task TestAfterAttributeFileScopedNamespace()
        => VerifyKeywordAsync(
@"namespace NS; [Attr] $$");

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
