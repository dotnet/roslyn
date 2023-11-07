// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class ProtectedKeywordRecommenderTests : KeywordRecommenderTests
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
            => await VerifyAbsenceAsync(@"$$");

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
        public async Task TestNotAfterFileScopedNamespace()
        {
            await VerifyAbsenceAsync("""
                namespace N;
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
            await VerifyAbsenceAsync(
                """
                $$
                using Goo;
                """);
        }

        [Fact]
        public async Task TestNotBeforeGlobalUsing()
        {
            await VerifyAbsenceAsync(
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
        public async Task TestNotInsideStruct()
        {
            await VerifyAbsenceAsync("""
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
        public async Task TestNotAfterProtected()
            => await VerifyAbsenceAsync(@"protected $$");

        [Fact]
        public async Task TestNotAfterSealed()
            => await VerifyAbsenceAsync(@"sealed $$");

        [Fact]
        public async Task TestNotAfterStatic()
            => await VerifyAbsenceAsync(@"static $$");

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
        public async Task TestAfterNestedInternal()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    internal $$
                """);
        }

        [Fact]
        public async Task TestNotAfterDelegate()
            => await VerifyAbsenceAsync(@"delegate $$");

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
        public async Task TestAfterNestedVirtual()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    virtual $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedOverride()
        {
            await VerifyKeywordAsync(
                """
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
                    int Goo { get; protected $$
                """);
        }

        [Fact]
        public async Task TestInPropertyAfterInternal()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    int Goo { get; internal $$
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
        public async Task TestNotInIndexerAfterAccessibility()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int this[int i] { get { } protected $$
                """);
        }

        [Fact]
        public async Task TestInIndexerAfterInternal()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    int this[int i] { get { } internal $$
                """);
        }
    }
}
