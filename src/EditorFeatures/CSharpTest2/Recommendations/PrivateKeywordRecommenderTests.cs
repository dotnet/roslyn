// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class PrivateKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotAfterFileScopedNamespace()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                namespace N;
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
            await VerifyAbsenceAsync(SourceCodeKind.Regular, """
                $$
                using Goo;
                """);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script, """
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

        // You can have an abstract private class.
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
        public async Task TestNotAfterInternal()
            => await VerifyAbsenceAsync(@"internal $$");

        [Fact]
        public async Task TestNotAfterPublic()
            => await VerifyAbsenceAsync(@"public $$");

        [Fact]
        public async Task TestNotAfterStaticPrivate()
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
        public async Task TestNotAfterProtected()
            => await VerifyAbsenceAsync(@"protected $$");

        [Fact]
        public async Task TestNotAfterSealed()
            => await VerifyAbsenceAsync(@"sealed $$");

        // You can have a 'sealed private class'.
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
        public async Task TestInProperty()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    int Goo { $$
                """);
        }

        [Fact]
        public async Task TestInPropertyAfterAccessor()
        {
            await VerifyKeywordAsync(
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
                    int Goo { get; private $$
                """);
        }

        [Fact]
        public async Task TestAfterRegion()
        {
            await VerifyKeywordAsync(
                """
                class C {
                #region Interop stuff
                    $$
                """);
        }

        [Fact]
        public async Task TestAfterTypeWithSemicolon()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    private enum PageAccess : int { PAGE_READONLY = 0x02 };
                    $$
                """);
        }

        [Fact]
        public async Task TestInIndexer()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    int this[int i] { $$
                """);
        }

        [Fact]
        public async Task TestInIndexerAfterAccessor()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    int this[int i] { get { } $$
                """);
        }

        [Fact]
        public async Task TestNotInIndexerAfterPrivateAccessibility()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int this[int i] { get { } private $$
                """);
        }

        [Fact]
        public async Task TestNotInIndexerAfterProtectedAccessibility()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    int this[int i] { get { } protected $$
                """);
        }

        [Fact]
        public async Task TestNotInIndexerAfterInternalAccessibility()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int this[int i] { get { } internal $$
                """);
        }
    }
}
