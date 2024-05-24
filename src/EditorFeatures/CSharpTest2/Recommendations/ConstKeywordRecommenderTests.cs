// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class ConstKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestAtRoot()
        {
            await VerifyKeywordAsync(
@"$$");
        }

        [Fact]
        public async Task TestAfterClass()
        {
            await VerifyKeywordAsync(
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalStatement()
        {
            await VerifyKeywordAsync(
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalVariableDeclaration()
        {
            await VerifyKeywordAsync(
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

        [Theory, CombinatorialData]
        public async Task TestInEmptyStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestAfterExtern()
        {
            await VerifyKeywordAsync("""
                extern alias Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterUsing()
        {
            await VerifyKeywordAsync("""
                using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalUsing()
        {
            await VerifyKeywordAsync("""
                global using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterNamespace()
        {
            await VerifyKeywordAsync("""
                namespace N {}
                $$
                """);
        }

        [Fact]
        public async Task TestAfterDelegateDeclaration()
        {
            await VerifyKeywordAsync("""
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

        [Theory]
        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script, Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeUsing(SourceCodeKind sourceCodeKind)
        {
            await VerifyAbsenceAsync(sourceCodeKind,
                """
                $$
                using Goo;
                """);
        }

        [Theory]
        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script, Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeGlobalUsing(SourceCodeKind sourceCodeKind)
        {
            await VerifyAbsenceAsync(sourceCodeKind,
                """
                $$
                global using Goo;
                """);
        }

        [Fact]
        public async Task TestAfterAssemblyAttribute()
        {
            await VerifyKeywordAsync("""
                [assembly: goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterRootAttribute()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular, """
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
            => await VerifyAbsenceAsync(@"static $$");

        [Fact]
        public async Task TestNotAfterNestedStatic()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    static $$
                """);
        }

        [Fact]
        public async Task TestNotAfterStaticPublic()
            => await VerifyAbsenceAsync(@"static public $$");

        [Fact]
        public async Task TestNotAfterNestedStaticPublic()
        {
            await VerifyAbsenceAsync(
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
        public async Task TestInMethod()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   void Goo() {
                     $$
                """);
        }

        [Fact]
        public async Task TestInMethodNotAfterConst()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   void Goo() {
                     const $$
                """);
        }

        [Fact]
        public async Task TestInProperty()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   int Goo {
                     get {
                       $$
                """);
        }
    }
}
