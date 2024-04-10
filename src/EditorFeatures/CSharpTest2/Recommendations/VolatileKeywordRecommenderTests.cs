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
    public class VolatileKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotInCompilationUnit()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"$$");

        [Fact]
        public async Task TestNotAfterExtern()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, """
                extern alias Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterExtern_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, """
                extern alias Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, """
                using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterUsing_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, """
                using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, """
                global using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalUsing_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, """
                global using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNamespace()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, """
                namespace N {}
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterTypeDeclaration()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, """
                class C {}
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterDelegateDeclaration()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, """
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
        public async Task TestNotAfterAssemblyAttribute()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, """
                [assembly: goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterAssemblyAttribute_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, """
                [assembly: goo]
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterRootAttribute()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, """
                [goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterRootAttribute_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, """
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
        public async Task TestNotInsideEnum()
        {
            await VerifyAbsenceAsync("""
                enum E {
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
        public async Task TestAfterNestedInternal()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    internal $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPublic()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"public $$");

        [Fact]
        public async Task TestAfterPublic_Interactive()
            => await VerifyKeywordAsync(SourceCodeKind.Script, @"public $$");

        [Fact]
        public async Task TestAfterNestedPublic()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPrivate()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
@"private $$");
        }

        [Fact]
        public async Task TestAfterPrivate_Script()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"private $$");
        }

        [Fact]
        public async Task TestAfterNestedPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    private $$
                """);
        }

        [Fact]
        public async Task TestNotAfterProtected()
        {
            await VerifyAbsenceAsync(
@"protected $$");
        }

        [Fact]
        public async Task TestAfterNestedProtected()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    protected $$
                """);
        }

        [Fact]
        public async Task TestNotAfterSealed()
            => await VerifyAbsenceAsync(@"sealed $$");

        [Fact]
        public async Task TestNotAfterNestedSealed()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    sealed $$
                """);
        }

        [Fact]
        public async Task TestNotAfterStatic()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"static $$");

        [Fact]
        public async Task TestAfterStatic_Interactive()
            => await VerifyKeywordAsync(SourceCodeKind.Script, @"static $$");

        [Fact]
        public async Task TestAfterNestedStatic()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    static $$
                """);
        }

        [Fact]
        public async Task TestNotAfterStaticPublic()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"static public $$");

        [Fact]
        public async Task TestAfterStaticPublic_Interactive()
            => await VerifyKeywordAsync(SourceCodeKind.Script, @"static public $$");

        [Fact]
        public async Task TestAfterNestedStaticPublic()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    static public $$
                """);
        }

        [Fact]
        public async Task TestNotAfterDelegate()
            => await VerifyAbsenceAsync(@"delegate $$");

        [Fact]
        public async Task TestNotAfterEvent()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    event $$
                """);
        }

        [Fact]
        public async Task TestNotAfterConst()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    const $$
                """);
        }

        [Fact]
        public async Task TestNotAfterVolatile()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    volatile $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNew()
        {
            await VerifyAbsenceAsync(
@"new $$");
        }

        [Fact]
        public async Task TestAfterNestedNew()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   new $$
                """);
        }

        [Fact]
        public async Task TestNotInMethod()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   void Goo() {
                     $$
                """);
        }
    }
}
