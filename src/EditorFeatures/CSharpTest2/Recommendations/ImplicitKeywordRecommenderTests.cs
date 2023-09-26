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
    public class ImplicitKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotAfterMethod()
        {
            await VerifyAbsenceAsync("""
                class C {
                  void Goo() {}
                  $$
                """);
        }

        [Fact]
        public async Task TestNotAfterMethodInPartialType()
        {
            await VerifyAbsenceAsync(
                """
                partial class C {
                  void Goo() {}
                  $$
                """);
        }

        [Fact]
        public async Task TestNotAfterFieldInPartialClass()
        {
            await VerifyAbsenceAsync(
                """
                partial class C {
                  int i;
                  $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPropertyInPartialClass()
        {
            await VerifyAbsenceAsync(
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
        public async Task TestNotAfterNestedAttributeInPartialClass()
        {
            await VerifyAbsenceAsync(
                """
                partial class C {
                  [goo]
                  $$
                """);
        }

        // This will be fixed once we have accessibility for members
        [Fact]
        public async Task TestNotInsideStruct()
        {
            await VerifyAbsenceAsync("""
                struct S {
                   $$
                """);
        }

        // This will be fixed once we have accessibility for members
        [Fact]
        public async Task TestNotInsidePartialStruct()
        {
            await VerifyAbsenceAsync(
                """
                partial struct S {
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
        public async Task TestNotInsidePartialClass()
        {
            await VerifyAbsenceAsync(
                """
                partial class C {
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
        public async Task TestNotAfterStaticPublic()
            => await VerifyAbsenceAsync(@"static public $$");

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
        public async Task TestNotAfterNestedStaticPublicInInterface()
        {
            await VerifyAbsenceAsync(
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
        public async Task TestNotAfterNestedStaticInInterface()
        {
            await VerifyAbsenceAsync(
                """
                interface C {
                    static $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPublicStatic()
            => await VerifyAbsenceAsync(@"public static $$");

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
        public async Task TestNotAfterNestedPublicStaticInInterface()
        {
            await VerifyAbsenceAsync(
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544103")]
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
    }
}
