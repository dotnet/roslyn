// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ExternKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAtRoot()
        => VerifyKeywordAsync(
@"$$");

    [Fact]
    public Task TestAfterClass()
        => VerifyKeywordAsync(
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestAfterGlobalStatement()
        => VerifyKeywordAsync(
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestAfterGlobalVariableDeclaration()
        => VerifyKeywordAsync(
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

    [Theory, CombinatorialData]
    public Task TestInEmptyStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterStaticInStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"static $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterAttributesInStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"[Attr] $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterAttributesInSwitchCase(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (c)
            {
                case 0:
                     [Goo]
                     $$
            }
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterAttributesAndStaticInStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"[Attr] static $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestBetweenAttributesAndReturnStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            [Attr]
            $$
            return x;
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestBetweenAttributesAndLocalDeclarationStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            [Attr]
            $$
            x y = bar();
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestBetweenAttributesAndAwaitExpression(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            [Attr]
            $$
            await bar;
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestBetweenAttributesAndAssignmentStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            [Goo]
            $$
            y = bar();
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestBetweenAttributesAndCallStatement1(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            [Goo]
            $$
            bar();
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestBetweenAttributesAndCallStatement2(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            [Goo1]
            [Goo2]
            $$
            bar();
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotAfterExternInStatement(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"extern $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public async Task TestNotAfterExternKeyword()
        => await VerifyAbsenceAsync(@"extern $$");

    [Fact]
    public Task TestAfterPreviousExternAlias()
        => VerifyKeywordAsync(
            """
            extern alias Goo;
            $$
            """);

    [Fact]
    public Task TestAfterUsing()
        => VerifyKeywordAsync("""
            using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterGlobalUsing()
        => VerifyKeywordAsync(
            """
            global using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterNamespace()
        => VerifyKeywordAsync("""
            namespace N {}
            $$
            """);

    [Fact]
    public Task TestInsideNamespace()
        => VerifyKeywordAsync(
            """
            namespace N {
                $$
            """);

    [Fact]
    public Task TestInsideFileScopedNamespace()
        => VerifyKeywordAsync(
@"namespace N;$$");

    [Fact]
    public Task TestNotAfterExternKeyword_InsideNamespace()
        => VerifyAbsenceAsync("""
            namespace N {
                extern $$
            """);

    [Fact]
    public Task TestAfterPreviousExternAlias_InsideNamespace()
        => VerifyKeywordAsync(
            """
            namespace N {
               extern alias Goo;
               $$
            """);

    [Fact]
    public Task TestNotAfterUsing_InsideNamespace()
        => VerifyAbsenceAsync("""
            namespace N {
                using Goo;
                $$
            """);

    [Fact]
    public Task TestNotAfterMember_InsideNamespace()
        => VerifyAbsenceAsync("""
            namespace N {
                class C {}
                $$
            """);

    [Fact]
    public Task TestNotAfterNamespace_InsideNamespace()
        => VerifyAbsenceAsync("""
            namespace N {
                namespace N {}
                $$
            """);

    [Fact]
    public Task TestInClass()
        => VerifyKeywordAsync(
            """
            class C {
                $$
            """);

    [Fact]
    public Task TestInStruct()
        => VerifyKeywordAsync(
            """
            struct S {
                $$
            """);

    [Fact]
    public Task TestInInterface()
        => VerifyKeywordAsync(
            """
            interface I {
                $$
            """);

    [Fact]
    public Task TestNotAfterAbstract()
        => VerifyAbsenceAsync(
            """
            class C {
                abstract $$
            """);

    [Fact]
    public Task TestNotAfterExtern()
        => VerifyAbsenceAsync(
            """
            class C {
                extern $$
            """);

    [Fact]
    public Task TestAfterPublic()
        => VerifyKeywordAsync(
            """
            class C {
                public $$
            """);

    [Fact]
    public Task TestWithinExtension()
        => VerifyKeywordAsync(
            """
            static class C
            {
                extension(string s)
                {
                    $$
                }
            }
            """, CSharpNextParseOptions);
}
