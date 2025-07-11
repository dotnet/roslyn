// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class StructKeywordRecommenderTests : KeywordRecommenderTests
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

    [Fact]
    public Task TestNotBeforeGlobalUsing()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            $$
            global using Goo;
            """);

    [Fact]
    public Task TestAfterReadonly()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
@"readonly $$");

    [Fact]
    public Task TestAfterRef()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
@"ref $$");

    [Fact]
    public Task TestAfterRefReadonly()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
@"ref readonly $$");

    [Fact]
    public Task TestAfterPublicRefReadonly()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
@"public ref readonly $$");

    [Fact]
    public Task TestAfterReadonlyRef()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
@"readonly ref $$");

    [Fact]
    public Task TestAfterInternalReadonlyRef()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
@"internal readonly ref $$");

    [Fact]
    public Task TestNotAfterReadonlyInMethod()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
@"class C { void M() { readonly $$ } }");

    [Fact]
    public Task TestNotAfterRefInMethod()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
@"class C { void M() { ref $$ } }");

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
    public Task TestNotBeforeUsing_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            $$
            using Goo;
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
    public Task TestAfterPrivate()
        => VerifyKeywordAsync(
@"private $$");

    [Fact]
    public Task TestAfterProtected()
        => VerifyKeywordAsync(
@"protected $$");

    [Fact]
    public Task TestAfterRecord()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
@"record $$");

    [Fact]
    public async Task TestNotAfterSealed()
        => await VerifyAbsenceAsync(@"sealed $$");

    [Fact]
    public async Task TestNotAfterStatic()
        => await VerifyAbsenceAsync(@"static $$");

    [Fact]
    public async Task TestNotAfterAbstractPublic()
        => await VerifyAbsenceAsync(@"abstract public $$");

    [Fact]
    public async Task TestNotAfterStruct()
        => await VerifyAbsenceAsync(@"struct $$");

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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64465")]
    public Task TestNotAfterRecord_AbstractModifier()
        => VerifyAbsenceAsync("abstract record $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64465")]
    public Task TestNotAfterRecord_SealedModifier()
        => VerifyAbsenceAsync("sealed record $$");

    [Fact]
    public Task TestAfterAllowsRefInTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C<T> where T : allows ref $$
            """);

    [Fact]
    public Task TestAfterAllowsRefInTypeParameterConstraint2()
        => VerifyKeywordAsync(
            """
            class C<T>
                where T : allows ref $$
                where U : U
            """);

    [Fact]
    public Task TestAfterAllowsRefInMethodTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo<T>()
                  where T : allows ref $$
            """);

    [Fact]
    public Task TestAfterAllowsRefInMethodTypeParameterConstraint2()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo<T>()
                  where T : allows ref $$
                  where U : T
            """);

    [Fact]
    public Task TestAfterAllowsRefAfterClassTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C<T> where T : class, allows ref $$
            """);

    [Fact]
    public Task TestAfterStructTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C<T> where T : struct, allows ref $$
            """);

    [Fact]
    public Task TestAfterSimpleTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C<T> where T : IGoo, allows ref $$
            """);

    [Fact]
    public Task TestAfterConstructorTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C<T> where T : new(), allows ref $$
            """);

    [Fact]
    public Task TestNotAfterStructInTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : allows ref struct $$
            """);

    [Fact]
    public Task TestNotAfterStructInTypeParameterConstraint2()
        => VerifyAbsenceAsync(
            """
            class C<T>
                where T : allows ref struct $$
                where U : U
            """);

    [Fact]
    public Task TestNotAfterStructAfterClassTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : class, allows ref struct $$
            """);

    [Fact]
    public Task TestNotAfterStructAfterStructTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : struct, allows ref struct $$
            """);

    [Fact]
    public Task TestNotAfterStructAfterSimpleTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : IGoo, allows ref struct $$
            """);

    [Fact]
    public Task TestNotAfterStructAfterConstructorTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : new(), allows ref struct $$
            """);

    [Fact]
    public Task TestAfterAllowsInTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : allows $$
            """);

    [Fact]
    public Task TestAfterAllowsInTypeParameterConstraint2()
        => VerifyAbsenceAsync(
            """
            class C<T>
                where T : allows $$
                where U : U
            """);

    [Fact]
    public Task TestAfterAllowsInMethodTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo<T>()
                  where T : allows $$
            """);

    [Fact]
    public Task TestAfterAllowsInMethodTypeParameterConstraint2()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo<T>()
                  where T : allows $$
                  where U : T
            """);

    [Fact]
    public Task TestAfterAllowsAfterClassTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : class, allows $$
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
