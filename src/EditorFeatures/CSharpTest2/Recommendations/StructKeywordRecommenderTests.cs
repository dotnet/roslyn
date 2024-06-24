// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class StructKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestAfterClass_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                int i = 0;
                $$
                """);
        }

        [Fact]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
        }

        [Fact]
        public async Task TestNotInGlobalUsingAlias()
        {
            await VerifyAbsenceAsync(
@"global using Goo = $$");
        }

        [Fact]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestInCompilationUnit()
        {
            await VerifyKeywordAsync(
@"$$");
        }

        [Fact]
        public async Task TestAfterExtern()
        {
            await VerifyKeywordAsync(
                """
                extern alias Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterUsing()
        {
            await VerifyKeywordAsync(
                """
                using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalUsing()
        {
            await VerifyKeywordAsync(
                """
                global using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterNamespace()
        {
            await VerifyKeywordAsync(
                """
                namespace N {}
                $$
                """);
        }

        [Fact]
        public async Task TestAfterFileScopedNamespace()
        {
            await VerifyKeywordAsync(
                """
                namespace N;
                $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
        public async Task TestFileKeywordInsideNamespace()
        {
            await VerifyKeywordAsync(
                """
                namespace N {
                file $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
        public async Task TestFileKeywordInsideNamespaceBeforeClass()
        {
            await VerifyKeywordAsync(
                """
                namespace N {
                file $$
                class C {}
                }
                """);
        }

        [Fact]
        public async Task TestAfterTypeDeclaration()
        {
            await VerifyKeywordAsync(
                """
                class C {}
                $$
                """);
        }

        [Fact]
        public async Task TestAfterDelegateDeclaration()
        {
            await VerifyKeywordAsync(
                """
                delegate void Goo();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterMethod()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  void Goo() {}
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterField()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  int i;
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterProperty()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  int i { get; }
                  $$
                """);
        }

        [Fact]
        public async Task TestNotBeforeUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                $$
                using Goo;
                """);
        }

        [Fact]
        public async Task TestNotBeforeGlobalUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                $$
                global using Goo;
                """);
        }

        [Fact]
        public async Task TestAfterReadonly()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"readonly $$");
        }

        [Fact]
        public async Task TestAfterRef()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"ref $$");
        }

        [Fact]
        public async Task TestAfterRefReadonly()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"ref readonly $$");
        }

        [Fact]
        public async Task TestAfterPublicRefReadonly()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"public ref readonly $$");
        }

        [Fact]
        public async Task TestAfterReadonlyRef()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"readonly ref $$");
        }

        [Fact]
        public async Task TestAfterInternalReadonlyRef()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"internal readonly ref $$");
        }

        [Fact]
        public async Task TestNotAfterReadonlyInMethod()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
@"class C { void M() { readonly $$ } }");
        }

        [Fact]
        public async Task TestNotAfterRefInMethod()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
@"class C { void M() { ref $$ } }");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                $$
                using Goo;
                """);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeGlobalUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                $$
                global using Goo;
                """);
        }

        [Fact]
        public async Task TestAfterAssemblyAttribute()
        {
            await VerifyKeywordAsync(
                """
                [assembly: goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterRootAttribute()
        {
            await VerifyKeywordAsync(
                """
                [goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  [goo]
                  $$
                """);
        }

        [Fact]
        public async Task TestInsideStruct()
        {
            await VerifyKeywordAsync(
                """
                struct S {
                   $$
                """);
        }

        [Fact]
        public async Task TestInsideInterface()
        {
            await VerifyKeywordAsync("""
                interface I {
                   $$
                """);
        }

        [Fact]
        public async Task TestInsideClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   $$
                """);
        }

        [Fact]
        public async Task TestAfterPartial()
        {
            await VerifyKeywordAsync(
@"partial $$");
        }

        [Fact]
        public async Task TestNotAfterAbstract()
            => await VerifyAbsenceAsync(@"abstract $$");

        [Fact]
        public async Task TestAfterInternal()
        {
            await VerifyKeywordAsync(
@"internal $$");
        }

        [Fact]
        public async Task TestAfterPublic()
        {
            await VerifyKeywordAsync(
@"public $$");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
        public async Task TestAfterFile()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"file $$");
        }

        [Fact]
        public async Task TestAfterPrivate()
        {
            await VerifyKeywordAsync(
@"private $$");
        }

        [Fact]
        public async Task TestAfterProtected()
        {
            await VerifyKeywordAsync(
@"protected $$");
        }

        [Fact]
        public async Task TestAfterRecord()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"record $$");
        }

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
        public async Task TestAfterClassTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
@"class C<T> where T : $$");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TestNotAfterClassTypeParameterConstraintWhenNotDirectlyInConstraint()
        {
            await VerifyAbsenceAsync(
@"class C<T> where T : IList<$$");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TestAfterClassTypeParameterConstraint2()
        {
            await VerifyKeywordAsync(
                """
                class C<T>
                    where T : $$
                    where U : U
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TestNotAfterClassTypeParameterConstraintWhenNotDirectlyInConstraint2()
        {
            await VerifyAbsenceAsync(
                """
                class C<T>
                    where T : IList<$$
                    where U : U
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TestAfterMethodTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo<T>()
                      where T : $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TestNotAfterMethodTypeParameterConstraintWhenNotDirectlyInConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo<T>()
                      where T : IList<$$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TestAfterMethodTypeParameterConstraint2()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo<T>()
                      where T : $$
                      where U : T
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TestNotAfterMethodTypeParameterConstraintWhenNotDirectlyInConstraint2()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo<T>()
                      where T : IList<$$
                      where U : T
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64465")]
        public async Task TestNotAfterRecord_AbstractModifier()
        {
            await VerifyAbsenceAsync("abstract record $$");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64465")]
        public async Task TestNotAfterRecord_SealedModifier()
        {
            await VerifyAbsenceAsync("sealed record $$");
        }

        [Fact]
        public async Task TestAfterAllowsRefInTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C<T> where T : allows ref $$
                """);
        }

        [Fact]
        public async Task TestAfterAllowsRefInTypeParameterConstraint2()
        {
            await VerifyKeywordAsync(
                """
                class C<T>
                    where T : allows ref $$
                    where U : U
                """);
        }

        [Fact]
        public async Task TestAfterAllowsRefInMethodTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo<T>()
                      where T : allows ref $$
                """);
        }

        [Fact]
        public async Task TestAfterAllowsRefInMethodTypeParameterConstraint2()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo<T>()
                      where T : allows ref $$
                      where U : T
                """);
        }

        [Fact]
        public async Task TestAfterAllowsRefAfterClassTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C<T> where T : class, allows ref $$
                """);
        }

        [Fact]
        public async Task TestAfterStructTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C<T> where T : struct, allows ref $$
                """);
        }

        [Fact]
        public async Task TestAfterSimpleTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C<T> where T : IGoo, allows ref $$
                """);
        }

        [Fact]
        public async Task TestAfterConstructorTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C<T> where T : new(), allows ref $$
                """);
        }

        [Fact]
        public async Task TestNotAfterStructInTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : allows ref struct $$
                """);
        }

        [Fact]
        public async Task TestNotAfterStructInTypeParameterConstraint2()
        {
            await VerifyAbsenceAsync(
                """
                class C<T>
                    where T : allows ref struct $$
                    where U : U
                """);
        }

        [Fact]
        public async Task TestNotAfterStructAfterClassTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : class, allows ref struct $$
                """);
        }

        [Fact]
        public async Task TestNotAfterStructAfterStructTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : struct, allows ref struct $$
                """);
        }

        [Fact]
        public async Task TestNotAfterStructAfterSimpleTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : IGoo, allows ref struct $$
                """);
        }

        [Fact]
        public async Task TestNotAfterStructAfterConstructorTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : new(), allows ref struct $$
                """);
        }

        [Fact]
        public async Task TestAfterAllowsInTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : allows $$
                """);
        }

        [Fact]
        public async Task TestAfterAllowsInTypeParameterConstraint2()
        {
            await VerifyAbsenceAsync(
                """
                class C<T>
                    where T : allows $$
                    where U : U
                """);
        }

        [Fact]
        public async Task TestAfterAllowsInMethodTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo<T>()
                      where T : allows $$
                """);
        }

        [Fact]
        public async Task TestAfterAllowsInMethodTypeParameterConstraint2()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo<T>()
                      where T : allows $$
                      where U : T
                """);
        }

        [Fact]
        public async Task TestAfterAllowsAfterClassTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : class, allows $$
                """);
        }

        [Fact]
        public async Task TestNotInExtensionForType()
        {
            await VerifyAbsenceAsync(
                """
                implicit extension E for $$
                """);
        }

        [Fact]
        public async Task TestInsideExtension()
        {
            await VerifyKeywordAsync(
                """
                implicit extension E
                {
                    $$
                """);
        }
    }
}
