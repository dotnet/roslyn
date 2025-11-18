// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ImplicitKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotAtRoot_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public Task TestNotAfterClass_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalStatement_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalVariableDeclaration_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            int i = 0;
            $$
            """);

    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
@"using Goo = $$");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
@"global using Goo = $$");

    [Fact]
    public Task TestNotInEmptyStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public async Task TestNotInCompilationUnit()
        => await VerifyAbsenceAsync(@"$$");

    [Fact]
    public Task TestNotAfterExtern()
        => VerifyAbsenceAsync("""
            extern alias Goo;
            $$
            """);

    [Fact]
    public Task TestNotAfterUsing()
        => VerifyAbsenceAsync("""
            using Goo;
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalUsing()
        => VerifyAbsenceAsync("""
            global using Goo;
            $$
            """);

    [Fact]
    public Task TestNotAfterNamespace()
        => VerifyAbsenceAsync("""
            namespace N {}
            $$
            """);

    [Fact]
    public Task TestNotAfterTypeDeclaration()
        => VerifyAbsenceAsync("""
            class C {}
            $$
            """);

    [Fact]
    public Task TestNotAfterDelegateDeclaration()
        => VerifyAbsenceAsync("""
            delegate void Goo();
            $$
            """);

    [Fact]
    public Task TestNotAfterMethod()
        => VerifyAbsenceAsync("""
            class C {
              void Goo() {}
              $$
            """);

    [Fact]
    public Task TestNotAfterMethodInPartialType()
        => VerifyAbsenceAsync(
            """
            partial class C {
              void Goo() {}
              $$
            """);

    [Fact]
    public Task TestNotAfterFieldInPartialClass()
        => VerifyAbsenceAsync(
            """
            partial class C {
              int i;
              $$
            """);

    [Fact]
    public Task TestNotAfterPropertyInPartialClass()
        => VerifyAbsenceAsync(
            """
            partial class C {
              int i { get; }
              $$
            """);

    [Fact]
    public Task TestNotBeforeUsing()
        => VerifyAbsenceAsync(
            """
            $$
            using Goo;
            """);

    [Fact]
    public Task TestNotBeforeGlobalUsing()
        => VerifyAbsenceAsync(
            """
            $$
            global using Goo;
            """);

    [Fact]
    public Task TestNotAfterAssemblyAttribute()
        => VerifyAbsenceAsync("""
            [assembly: goo]
            $$
            """);

    [Fact]
    public Task TestNotAfterRootAttribute()
        => VerifyAbsenceAsync("""
            [goo]
            $$
            """);

    [Fact]
    public Task TestNotAfterNestedAttributeInPartialClass()
        => VerifyAbsenceAsync(
            """
            partial class C {
              [goo]
              $$
            """);

    // This will be fixed once we have accessibility for members
    [Fact]
    public Task TestNotInsideStruct()
        => VerifyAbsenceAsync("""
            struct S {
               $$
            """);

    // This will be fixed once we have accessibility for members
    [Fact]
    public Task TestNotInsidePartialStruct()
        => VerifyAbsenceAsync(
            """
            partial struct S {
               $$
            """);

    [Fact]
    public Task TestNotInsideInterface()
        => VerifyAbsenceAsync("""
            interface I {
               $$
            """);

    [Fact]
    public Task TestNotInsidePartialClass()
        => VerifyAbsenceAsync(
            """
            partial class C {
               $$
            """);

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
    public Task TestAfterNestedStaticPublicInClass()
        => VerifyKeywordAsync(
            """
            class C {
                static public $$
            """);

    [Fact]
    public Task TestNotAfterNestedStaticPublicInInterface()
        => VerifyAbsenceAsync(
            """
            interface C {
                static public $$
            """);

    [Fact]
    public Task TestNotAfterNestedAbstractPublicInInterface()
        => VerifyAbsenceAsync(
            """
            interface C {
                abstract public $$
            """);

    [Fact]
    public Task TestAfterNestedStaticAbstractPublicInInterface()
        => VerifyKeywordAsync(
            """
            interface C {
                static abstract public $$
            """);

    [Fact]
    public Task TestAfterNestedAbstractStaticPublicInInterface()
        => VerifyKeywordAsync(
            """
            interface C {
                abstract static public $$
            """);

    [Fact]
    public Task TestAfterNestedStaticAbstractInInterface()
        => VerifyKeywordAsync(
            """
            interface C {
                static abstract $$
            """);

    [Fact]
    public Task TestAfterNestedAbstractStaticInInterface()
        => VerifyKeywordAsync(
            """
            interface C {
                abstract static $$
            """);

    [Fact]
    public Task TestNotAfterNestedStaticInInterface()
        => VerifyAbsenceAsync(
            """
            interface C {
                static $$
            """);

    [Fact]
    public async Task TestNotAfterPublicStatic()
        => await VerifyAbsenceAsync(@"public static $$");

    [Fact]
    public Task TestAfterNestedPublicStaticInClass()
        => VerifyKeywordAsync(
            """
            class C {
                public static $$
            """);

    [Fact]
    public Task TestNotAfterNestedPublicStaticInInterface()
        => VerifyAbsenceAsync(
            """
            interface C {
                public static $$
            """);

    [Fact]
    public Task TestNotAfterNestedPublicAbstractInInterface()
        => VerifyAbsenceAsync(
            """
            interface C {
                public abstract $$
            """);

    [Fact]
    public Task TestAfterNestedPublicStaticAbstractInInterface()
        => VerifyKeywordAsync(
            """
            interface C {
                public static abstract $$
            """);

    [Fact]
    public Task TestAfterNestedPublicAbstractStaticInInterface()
        => VerifyKeywordAsync(
            """
            interface C {
                public abstract static $$
            """);

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
    public Task TestNotBetweenUsings()
        => VerifyAbsenceAsync(
            """
            using Goo;
            $$
            using Bar;
            """);

    [Fact]
    public Task TestNotBetweenGlobalUsings_01()
        => VerifyAbsenceAsync(
            """
            global using Goo;
            $$
            using Bar;
            """);

    [Fact]
    public Task TestNotBetweenGlobalUsings_02()
        => VerifyAbsenceAsync(
            """
            global using Goo;
            $$
            global using Bar;
            """);

    [Fact]
    public Task TestNotAfterNestedAbstractInClass()
        => VerifyAbsenceAsync("""
            class C {
                abstract $$
            """);

    [Fact]
    public Task TestNotAfterNestedVirtualInClass()
        => VerifyAbsenceAsync("""
            class C {
                virtual $$
            """);

    [Fact]
    public Task TestNotAfterNestedOverrideInClass()
        => VerifyAbsenceAsync("""
            class C {
                override $$
            """);

    [Fact]
    public Task TestNotAfterNestedSealedInClass()
        => VerifyAbsenceAsync("""
            class C {
                sealed $$
            """);

    [Fact]
    public Task TestNotAfterNestedReadOnlyInClass()
        => VerifyAbsenceAsync("""
            class C {
                readonly $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544103")]
    public Task TestAfterNestedUnsafeStaticPublicInClass()
        => VerifyKeywordAsync("""
            class C {
                 unsafe static public $$
            """);

    [Fact]
    public Task TestNotAfterNestedAbstractInInterface()
        => VerifyAbsenceAsync("""
            interface C {
                abstract $$
            """);

    [Fact]
    public Task TestNotAfterNestedVirtualInInterface()
        => VerifyAbsenceAsync("""
            interface C {
                virtual $$
            """);

    [Fact]
    public Task TestNotAfterNestedOverrideInInterface()
        => VerifyAbsenceAsync("""
            interface C {
                override $$
            """);

    [Fact]
    public Task TestNotAfterNestedSealedInInterface()
        => VerifyAbsenceAsync("""
            interface C {
                sealed $$
            """);

    [Fact]
    public Task TestNotAfterNestedReadOnlyInInterface()
        => VerifyAbsenceAsync("""
            interface C {
                readonly $$
            """);

    [Fact]
    public Task TestAfterUnsafeStaticAbstractInInterface()
        => VerifyKeywordAsync("""
            interface C {
                 unsafe static abstract $$
            """);

    [Fact]
    public Task TestNotAfterExternStaticAbstractInInterface()
        => VerifyAbsenceAsync("""
            interface C {
                 extern static abstract $$
            """);
}
