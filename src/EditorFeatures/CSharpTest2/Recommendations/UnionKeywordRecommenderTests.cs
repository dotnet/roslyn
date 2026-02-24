// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class UnionKeywordRecommenderTests : KeywordRecommenderTests
{
    private static readonly CSharpParseOptions s_options = CSharpNextParseOptions;
    private static readonly CSharpParseOptions s_scriptOptions = CSharpNextScriptParseOptions;

    [Fact]
    public Task TestAtRoot_Interactive()
        => VerifyWorkerAsync(@"$$", absent: false, options: s_scriptOptions);

    [Fact]
    public Task TestAfterClass_Interactive()
        => VerifyWorkerAsync(
            """
            class C { }
            $$
            """, absent: false, options: s_scriptOptions);

    [Fact]
    public Task TestAfterGlobalStatement()
        => VerifyKeywordAsync(
            """
            System.Console.WriteLine();
            $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterGlobalVariableDeclaration_Interactive()
        => VerifyWorkerAsync(
            """
            int i = 0;
            $$
            """, absent: false, options: s_scriptOptions);

    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
@"using Goo = $$", s_options, s_scriptOptions);

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
@"global using Goo = $$", s_options, s_scriptOptions);

    [Fact]
    public Task TestNotInEmptyStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$"), s_options, s_scriptOptions);

    [Fact]
    public Task TestInCompilationUnit()
        => VerifyKeywordAsync(
@"$$", s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterExtern()
        => VerifyKeywordAsync(
            """
            extern alias Goo;
            $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterUsing()
        => VerifyKeywordAsync(
            """
            using Goo;
            $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterGlobalUsing()
        => VerifyKeywordAsync(
            """
            global using Goo;
            $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterNamespace()
        => VerifyKeywordAsync(
            """
            namespace N {}
            $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterFileScopedNamespace()
        => VerifyKeywordAsync(
            """
            namespace N;
            $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterTypeDeclaration()
        => VerifyKeywordAsync(
            """
            class C {}
            $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterDelegateDeclaration()
        => VerifyKeywordAsync(
            """
            delegate void Goo();
            $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterMethod()
        => VerifyKeywordAsync(
            """
            class C {
              void Goo() {}
              $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterField()
        => VerifyKeywordAsync(
            """
            class C {
              int i;
              $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterProperty()
        => VerifyKeywordAsync(
            """
            class C {
              int i { get; }
              $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestNotBeforeUsing()
        => VerifyWorkerAsync(
            """
            $$
            using Goo;
            """, absent: true, options: s_options);

    [Fact]
    public Task TestNotBeforeGlobalUsing()
        => VerifyWorkerAsync(
            """
            $$
            global using Goo;
            """, absent: true, options: s_options);

    [Fact]
    public Task TestNotAfterReadonly()
        => VerifyWorkerAsync(
@"readonly $$", absent: true, options: s_options);

    [Fact]
    public Task TestNotAfterRef()
        => VerifyWorkerAsync(
@"ref $$", absent: true, options: s_options);

    [Fact]
    public Task TestNotAfterRefReadonly()
        => VerifyWorkerAsync(
@"ref readonly $$", absent: true, options: s_options);

    [Fact]
    public Task TestNotAfterPublicRefReadonly()
        => VerifyWorkerAsync(
@"public ref readonly $$", absent: true, options: s_options);

    [Fact]
    public Task TestNotAfterReadonlyRef()
        => VerifyWorkerAsync(
@"readonly ref $$", absent: true, options: s_options);

    [Fact]
    public Task TestNotAfterInternalReadonlyRef()
        => VerifyWorkerAsync(
@"internal readonly ref $$", absent: true, options: s_options);

    [Fact]
    public Task TestNotAfterReadonlyInMethod()
        => VerifyWorkerAsync(
@"class C { void M() { readonly $$ } }", absent: true, options: s_options);

    [Fact]
    public Task TestNotAfterRefInMethod()
        => VerifyWorkerAsync(
@"class C { void M() { ref $$ } }", absent: true, options: s_options);

    [Fact]
    public Task TestAfterAssemblyAttribute()
        => VerifyKeywordAsync(
            """
            [assembly: goo]
            $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterRootAttribute()
        => VerifyKeywordAsync(
            """
            [goo]
            $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterNestedAttribute()
        => VerifyKeywordAsync(
            """
            class C {
              [goo]
              $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestInsideStruct()
        => VerifyKeywordAsync(
            """
            struct S {
               $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestInsideInterface()
        => VerifyKeywordAsync("""
            interface I {
               $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestInsideClass()
        => VerifyKeywordAsync(
            """
            class C {
               $$
            """, s_options, s_scriptOptions);

    [Fact]
    public Task TestNotAfterPartial()
        => VerifyAbsenceAsync(
@"partial $$", s_options, s_scriptOptions);

    [Fact]
    public Task TestNotAfterAbstract()
        => VerifyAbsenceAsync(@"abstract $$", s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterInternal()
        => VerifyKeywordAsync(
@"internal $$", s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterPublic()
        => VerifyKeywordAsync(
@"public $$", s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterFile()
        => VerifyWorkerAsync(
@"file $$", absent: false, options: s_options);

    [Fact]
    public Task TestAfterPrivate()
        => VerifyKeywordAsync(
@"private $$", s_options, s_scriptOptions);

    [Fact]
    public Task TestAfterProtected()
        => VerifyKeywordAsync(
@"protected $$", s_options, s_scriptOptions);

    [Fact]
    public Task TestNotAfterSealed()
        => VerifyAbsenceAsync(@"sealed $$", s_options, s_scriptOptions);

    [Fact]
    public Task TestNotAfterStatic()
        => VerifyAbsenceAsync(@"static $$", s_options, s_scriptOptions);

    [Fact]
    public Task TestNotAfterAbstractPublic()
        => VerifyAbsenceAsync(@"abstract public $$", s_options, s_scriptOptions);

    [Fact]
    public Task TestNotAfterStruct()
        => VerifyAbsenceAsync(@"struct $$", s_options, s_scriptOptions);

    [Fact]
    public Task TestNotInTypeParameterConstraint()
        => VerifyAbsenceAsync(
@"class C<T> where T : $$", s_options, s_scriptOptions);

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
