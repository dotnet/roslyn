// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class OverrideKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
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
            await VerifyAbsenceAsync("""
                extern alias Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterUsing()
        {
            await VerifyAbsenceAsync("""
                using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalUsing()
        {
            await VerifyAbsenceAsync("""
                global using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNamespace()
        {
            await VerifyAbsenceAsync("""
                namespace N {}
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterTypeDeclaration()
        {
            await VerifyAbsenceAsync("""
                class C {}
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterDelegateDeclaration()
        {
            await VerifyAbsenceAsync("""
                delegate void Goo();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterMethodInClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  void Goo() {}
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterFieldInClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  int i;
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterFieldInRecord()
        {
            await VerifyWorkerAsync(
                """
                record C {
                  int i;
                  $$
                """, absent: false, options: TestOptions.RegularPreview);
        }

        [Fact]
        public async Task TestAfterPropertyInClass()
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
            await VerifyAbsenceAsync("""
                [assembly: goo]
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterRootAttribute()
        {
            await VerifyAbsenceAsync("""
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
        public async Task TestNotInsideInterface()
        {
            await VerifyAbsenceAsync("""
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
        public async Task TestNotAfterPartial()
            => await VerifyAbsenceAsync(@"partial $$");

        [Fact]
        public async Task TestNotAfterAbstract()
            => await VerifyAbsenceAsync(@"abstract $$");

        [Fact]
        public async Task TestNotAfterInternal()
            => await VerifyAbsenceAsync(@"internal $$");

        [Fact]
        public async Task TestNotAfterPublic()
            => await VerifyAbsenceAsync(@"public $$");

        [Fact]
        public async Task TestNotAfterStaticInternal()
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
        public async Task TestNotAfterSealed()
            => await VerifyAbsenceAsync(@"sealed $$");

        [Fact]
        public async Task TestNotAfterStatic()
            => await VerifyAbsenceAsync(@"static $$");

        [Fact]
        public async Task TestNotAfterNestedStatic()
        {
            await VerifyAbsenceAsync("""
                class C {
                    static $$
                """);
        }

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
        public async Task TestNotAfterNestedPrivate()
        {
            await VerifyAbsenceAsync("""
                class C {
                    private $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedNew()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    new $$
                """);
        }

        [Fact]
        public async Task TestNotAfterDelegate()
            => await VerifyAbsenceAsync(@"delegate $$");

        [Fact]
        public async Task TestNotAfterNestedVirtual()
        {
            await VerifyAbsenceAsync("""
                class C {
                    virtual $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedOverride()
        {
            await VerifyAbsenceAsync("""
                class C {
                    override $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedSealed()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    sealed $$
                """);
        }

        [Fact]
        public async Task TestNotInProperty()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int Goo { $$
                """);
        }

        [Fact]
        public async Task TestNotInPropertyAfterAccessor()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int Goo { get; $$
                """);
        }

        [Fact]
        public async Task TestNotInPropertyAfterAccessibility()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int Goo { get; protected $$
                """);
        }

        [Fact]
        public async Task TestNotInPropertyAfterInternal()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int Goo { get; internal $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedAbstract()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    abstract $$
                """);
        }

        [Fact]
        public async Task TestAfterPrivateProtected()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    private protected $$
                """);
        }
    }
}
