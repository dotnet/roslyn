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
    public sealed class ExplicitKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestAfterClassDeclaration_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
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
            => await VerifyKeywordAsync(@"$$");

        [Fact]
        public async Task TestAfterExternDeclaration()
        {
            await VerifyKeywordAsync("""
                extern alias Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterUsingDeclaration()
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
        public async Task TestAfterNamespaceDeclaration()
        {
            await VerifyKeywordAsync("""
                namespace N {}
                $$
                """);
        }

        [Fact]
        public async Task TestAfterTypeDeclaration()
        {
            await VerifyKeywordAsync("""
                class C {}
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
            await VerifyKeywordAsync("""
                class C {
                  void Goo() {}
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterMethodInPartialType()
        {
            await VerifyKeywordAsync(
                """
                partial class C {
                  void Goo() {}
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterFieldInPartialClass()
        {
            await VerifyKeywordAsync(
                """
                partial class C {
                  int i;
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterPropertyInPartialClass()
        {
            await VerifyKeywordAsync(
                """
                partial class C {
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
            await VerifyKeywordAsync("""
                [goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedAttributeInPartialClass()
        {
            await VerifyKeywordAsync(
                """
                partial class C {
                  [goo]
                  $$
                """);
        }

        [Fact]
        public async Task TestInsideStruct()
        {
            await VerifyKeywordAsync("""
                struct S {
                   $$
                """);
        }

        [Fact]
        public async Task TestInsidePartialStruct()
        {
            await VerifyKeywordAsync(
                """
                partial struct S {
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
        public async Task TestInsidePartialClass()
        {
            await VerifyKeywordAsync(
                """
                partial class C {
                   $$
                """);
        }

        [Fact]
        public async Task TestAfterPartial()
            => await VerifyKeywordAsync(@"partial $$");

        [Fact]
        public async Task TestNotAfterAbstract()
            => await VerifyAbsenceAsync(@"abstract $$");

        [Fact]
        public async Task TestAfterInternal()
            => await VerifyKeywordAsync(@"internal $$");

        [Fact]
        public async Task TestAfterPublic()
            => await VerifyKeywordAsync(@"public $$");

        [Fact]
        public async Task TestAfterStaticPublic()
            => await VerifyKeywordAsync(@"static public $$");

        [Fact]
        public async Task TestAfterNestedStaticPublicInClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    static public $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedStaticPublicInInterface()
        {
            await VerifyKeywordAsync(
                """
                interface C {
                    static public $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedAbstractPublicInInterface()
        {
            await VerifyAbsenceAsync(
                """
                interface C {
                    abstract public $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedStaticAbstractPublicInInterface()
        {
            await VerifyKeywordAsync(
                """
                interface C {
                    static abstract public $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedAbstractStaticPublicInInterface()
        {
            await VerifyKeywordAsync(
                """
                interface C {
                    abstract static public $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedStaticAbstractInInterface()
        {
            await VerifyKeywordAsync(
                """
                interface C {
                    static abstract $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedAbstractStaticInInterface()
        {
            await VerifyKeywordAsync(
                """
                interface C {
                    abstract static $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedStaticInInterface()
        {
            await VerifyKeywordAsync(
                """
                interface C {
                    static $$
                """);
        }

        [Fact]
        public async Task TestAfterPublicStatic()
            => await VerifyKeywordAsync(@"public static $$");

        [Fact]
        public async Task TestAfterNestedPublicStaticInClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public static $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedPublicStaticInInterface()
        {
            await VerifyKeywordAsync(
                """
                interface C {
                    public static $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedPublicAbstractInInterface()
        {
            await VerifyAbsenceAsync(
                """
                interface C {
                    public abstract $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedPublicStaticAbstractInInterface()
        {
            await VerifyKeywordAsync(
                """
                interface C {
                    public static abstract $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedPublicAbstractStaticInInterface()
        {
            await VerifyKeywordAsync(
                """
                interface C {
                    public abstract static $$
                """);
        }

        [Fact]
        public async Task TestNotAfterInvalidPublic()
            => await VerifyAbsenceAsync(@"virtual public $$");

        [Fact]
        public async Task TestAfterPrivate()
            => await VerifyKeywordAsync(@"private $$");

        [Fact]
        public async Task TestAfterProtected()
            => await VerifyKeywordAsync(@"protected $$");

        [Fact]
        public async Task TestNotAfterSealed()
            => await VerifyAbsenceAsync(@"sealed $$");

        [Fact]
        public async Task TestAfterStatic()
            => await VerifyKeywordAsync(@"static $$");

        [Fact]
        public async Task TestNotAfterClass()
            => await VerifyAbsenceAsync(@"class $$");

        [Fact]
        public async Task TestNotAfterDelegate()
            => await VerifyAbsenceAsync(@"delegate $$");

        [Fact]
        public async Task TestNotBetweenUsings()
        {
            await VerifyAbsenceAsync(
                """
                using Goo;
                $$
                using Bar;
                """);
        }

        [Fact]
        public async Task TestNotBetweenGlobalUsings_01()
        {
            await VerifyAbsenceAsync(
                """
                global using Goo;
                $$
                using Bar;
                """);
        }

        [Fact]
        public async Task TestNotBetweenGlobalUsings_02()
        {
            await VerifyAbsenceAsync(
                """
                global using Goo;
                $$
                global using Bar;
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedAbstractInClass()
        {
            await VerifyAbsenceAsync("""
                class C {
                    abstract $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedVirtualInClass()
        {
            await VerifyAbsenceAsync("""
                class C {
                    virtual $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedOverrideInClass()
        {
            await VerifyAbsenceAsync("""
                class C {
                    override $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedSealedInClass()
        {
            await VerifyAbsenceAsync("""
                class C {
                    sealed $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedReadOnlyInClass()
        {
            await VerifyAbsenceAsync("""
                class C {
                    readonly $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544102")]
        public async Task TestAfterNestedUnsafeStaticPublicInClass()
        {
            await VerifyKeywordAsync("""
                class C {
                     unsafe static public $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedAbstractInInterface()
        {
            await VerifyAbsenceAsync("""
                interface C {
                    abstract $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedVirtualInInterface()
        {
            await VerifyAbsenceAsync("""
                interface C {
                    virtual $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedOverrideInInterface()
        {
            await VerifyAbsenceAsync("""
                interface C {
                    override $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedSealedInInterface()
        {
            await VerifyAbsenceAsync("""
                interface C {
                    sealed $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedReadOnlyInInterface()
        {
            await VerifyAbsenceAsync("""
                interface C {
                    readonly $$
                """);
        }

        [Fact]
        public async Task TestAfterUnsafeStaticAbstractInInterface()
        {
            await VerifyKeywordAsync("""
                interface C {
                     unsafe static abstract $$
                """);
        }

        [Fact]
        public async Task TestNotAfterExternStaticAbstractInInterface()
        {
            await VerifyAbsenceAsync("""
                interface C {
                     extern static abstract $$
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
