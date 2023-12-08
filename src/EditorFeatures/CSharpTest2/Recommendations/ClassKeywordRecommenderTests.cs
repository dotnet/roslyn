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
    public class ClassKeywordRecommenderTests : KeywordRecommenderTests
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

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
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
        public async Task TestAfterAbstract()
        {
            await VerifyKeywordAsync(
@"abstract $$");
        }

        [Fact]
        public async Task TestAfterInternal()
        {
            await VerifyKeywordAsync(
@"internal $$");
        }

        [Fact]
        public async Task TestAfterStaticPublic()
        {
            await VerifyKeywordAsync(
@"static public $$");
        }

        [Fact]
        public async Task TestAfterPublicStatic()
        {
            await VerifyKeywordAsync(
@"public static $$");
        }

        [Fact]
        public async Task TestNotAfterInvalidPublic()
            => await VerifyAbsenceAsync(@"virtual public $$");

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
        public async Task TestAfterSealed()
        {
            await VerifyKeywordAsync(
@"sealed $$");
        }

        [Fact]
        public async Task TestAfterStatic()
        {
            await VerifyKeywordAsync(
@"static $$");
        }

        [Fact]
        public async Task TestNotAfterStaticInUsingDirective()
        {
            await VerifyAbsenceAsync(
@"using static $$");
        }

        [Fact]
        public async Task TestNotAfterStaticInGlobalUsingDirective()
        {
            await VerifyAbsenceAsync(
@"global using static $$");
        }

        [Fact]
        public async Task TestNotAfterClass()
            => await VerifyAbsenceAsync(@"class $$");

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
        public async Task TestNotBetweenUsings()
        {
            // Recommendation in scripting is not stable. See https://github.com/dotnet/roslyn/issues/32214
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                using Goo;
                $$
                using Bar;
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
        public async Task TestNotBetweenGlobalUsings_01()
        {
            // Recommendation in scripting is not stable. See https://github.com/dotnet/roslyn/issues/32214
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                global using Goo;
                $$
                using Bar;
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
        public async Task TestNotBetweenGlobalUsings_02()
        {
            // Recommendation in scripting is not stable. See https://github.com/dotnet/roslyn/issues/32214
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                global using Goo;
                $$
                global using Bar;
                """);
        }

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

        [Fact]
        public async Task TestAfterNew()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    new $$
                """);
        }

        [Fact]
        public async Task TestAfterRecord()
        {
            await VerifyKeywordAsync(
@"record $$");
        }

        [Fact]
        public async Task TestAfterAttributeFileScopedNamespace()
        {
            await VerifyKeywordAsync(
@"namespace NS; [Attr] $$");
        }
    }
}
