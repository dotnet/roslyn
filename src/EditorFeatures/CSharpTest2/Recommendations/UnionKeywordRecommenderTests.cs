// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class UnionKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAfterGlobalStatement()
        => VerifyKeywordAsync(
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
@"using Goo = $$");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync("global using Goo = $$");

    [Fact]
    public Task TestNotInEmptyStatement()
        => VerifyAbsenceAsync(AddInsideMethod("$$"));

    [Fact]
    public Task TestInCompilationUnit()
        => VerifyKeywordAsync("$$");

    [Fact]
    public Task TestInCompilationUnit_LangVer14()
        => VerifyKeywordAsync("$$", CSharp14ParseOptions);

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
        => VerifyAbsenceAsync(
            """
            $$
            global using Goo;
            """);

    [Fact]
    public Task TestAfterReadonly()
        => VerifyKeywordAsync("readonly $$");

    [Fact]
    public Task TestNotAfterRef()
        => VerifyAbsenceAsync("ref $$");

    [Fact]
    public Task TestNotAfterRefReadonly()
        => VerifyAbsenceAsync("ref readonly $$");

    [Fact]
    public Task TestNotAfterPublicRefReadonly()
        => VerifyAbsenceAsync("public ref readonly $$");

    [Fact]
    public Task TestNotAfterReadonlyRef()
        => VerifyAbsenceAsync("readonly ref $$");

    [Fact]
    public Task TestNotAfterInternalReadonlyRef()
        => VerifyAbsenceAsync("internal readonly ref $$");

    [Fact]
    public Task TestNotAfterReadonlyInMethod()
        => VerifyAbsenceAsync("class C { void M() { readonly $$ } }");

    [Fact]
    public Task TestNotAfterRefInMethod()
        => VerifyAbsenceAsync("class C { void M() { ref $$ } }");

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
        => VerifyKeywordAsync("partial $$");

    [Fact]
    public Task TestNotAfterAbstract()
        => VerifyAbsenceAsync("abstract $$");

    [Fact]
    public Task TestAfterInternal()
        => VerifyKeywordAsync("internal $$");

    [Fact]
    public Task TestAfterPublic()
        => VerifyKeywordAsync("public $$");

    [Fact]
    public Task TestAfterFile()
        => VerifyKeywordAsync(SourceCodeKind.Regular, "file $$");

    [Fact]
    public Task TestAfterPrivate()
        => VerifyKeywordAsync("private $$");

    [Fact]
    public Task TestAfterProtected()
        => VerifyKeywordAsync("protected $$");

    [Fact]
    public Task TestNotAfterSealed()
        => VerifyAbsenceAsync("sealed $$");

    [Fact]
    public Task TestNotAfterStatic()
        => VerifyAbsenceAsync("static $$");

    [Fact]
    public Task TestNotAfterAbstractPublic()
        => VerifyAbsenceAsync("abstract public $$");

    [Fact]
    public Task TestNotAfterStruct()
        => VerifyAbsenceAsync("struct $$");

    [Fact]
    public Task TestNotInTypeParameterConstraint()
        => VerifyAbsenceAsync("class C<T> where T : $$");

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
            """);
}
