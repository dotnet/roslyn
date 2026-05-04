// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class UsingKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
@"using Goo = $$");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
@"global using Goo = $$");

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
    public Task TestAfterGlobalVariableDeclaration_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
            """
            int i = 0;
            $$
            """);

    [Theory, CombinatorialData]
    public Task TestInEmptyStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestAfterAwait()
        => VerifyKeywordAsync(
            """
            class C
            {
                async void M()
                {
                    await $$
                }
            }
            """);

    [Fact]
    public Task TestAfterAwaitInAssignment()
        => VerifyAbsenceAsync(
            """
            class C
            {
                async void M()
                {
                    _ = await $$
                }
            }
            """);

    [Fact]
    public Task TestAtRoot()
        => VerifyKeywordAsync(
@"$$");

    [Fact]
    public async Task TestNotAfterUsingKeyword()
        => await VerifyAbsenceAsync(@"using $$");

    [Fact]
    public Task TestAfterPreviousUsing()
        => VerifyKeywordAsync(
            """
            using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterPreviousGlobalUsing()
        => VerifyKeywordAsync(
            """
            global using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterExtern()
        => VerifyKeywordAsync(
            """
            extern alias goo;
            $$
            """);

    [Fact]
    public Task TestAfterGlobalAfterExtern()
        => VerifyKeywordAsync(
            """
            extern alias goo;
            global $$
            """);

    [Fact]
    public Task TestAfterGlobalAfterExternBeforeUsing_01()
        => VerifyKeywordAsync(
            """
            extern alias goo;
            global $$
            using Goo;
            """);

    [Fact]
    public Task TestAfterGlobalAfterExternBeforeUsing_02()
        => VerifyKeywordAsync(
            """
            extern alias goo;
            global $$
            global using Goo;
            """);

    [Fact]
    public Task TestBeforeUsing()
        => VerifyKeywordAsync(
            """
            $$
            using Goo;
            """);

    [Fact]
    public Task TestBeforeUsingAfterGlobal()
        => VerifyKeywordAsync(
            """
            global $$
            using Goo;
            """);

    [Fact]
    public Task TestAfterUsingAlias()
        => VerifyKeywordAsync(
            """
            using Goo = Bar;
            $$
            """);

    [Fact]
    public Task TestAfterGlobalUsingAlias()
        => VerifyKeywordAsync(
            """
            global using Goo = Bar;
            $$
            """);

    [Fact]
    public Task TestNotAfterNestedTypeDeclaration()
        => VerifyAbsenceAsync("""
            class A {
                class C {}
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
    public Task TestAfterFileScopedNamespace()
        => VerifyKeywordAsync(
            """
            namespace N;
            $$
            """);

    [Fact]
    public Task TestNotAfterUsingKeyword_InsideNamespace()
        => VerifyAbsenceAsync("""
            namespace N {
                using $$
            """);

    [Fact]
    public Task TestAfterPreviousUsing_InsideNamespace()
        => VerifyKeywordAsync(
            """
            namespace N {
               using Goo;
               $$
            """);

    [Fact]
    public Task TestBeforeUsing_InsideNamespace()
        => VerifyKeywordAsync(
            """
            namespace N {
                $$
                using Goo;
            """);

    [Fact]
    public Task TestNotAfterMember_InsideNamespace()
        => VerifyAbsenceAsync("""
            namespace N {
                class C {}
                $$
            """);

    [Fact]
    public Task TestNotAfterNestedMember_InsideNamespace()
        => VerifyAbsenceAsync("""
            namespace N {
                class A {
                  class C {}
                  $$
            """);

    [Fact]
    public Task TestNotBeforeExtern()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            $$
            extern alias Goo;
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
    public Task TestNotBeforeExtern_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            $$
            extern alias Goo;
            """);

    [Fact]
    public Task TestNotBeforeExternAfterGlobal()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            global $$
            extern alias Goo;
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
    public Task TestNotBeforeExternAfterGlobal_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            global $$
            extern alias Goo;
            """);

    [Theory, CombinatorialData]
    public Task TestBeforeStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            $$
            return true;
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            return true;
            $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterBlock(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            if (true) {
            }
            $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterIf(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            if (true) 
                $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterDo(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            do 
                $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterWhile(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            while (true) 
                $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterFor(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            for (int i = 0; i < 10; i++) 
                $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterForeach(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            foreach (var v in bar)
                $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotAfterUsing()
        => VerifyAbsenceAsync(
@"using $$");

    [Fact]
    public Task TestNotAfterGlobalUsing()
        => VerifyAbsenceAsync(
@"global using $$");

    [Fact]
    public Task TestNotInClass()
        => VerifyAbsenceAsync("""
            class C
            {
              $$
            }
            """);

    [Fact]
    public Task TestBetweenUsings_01()
        => VerifyKeywordAsync(
            """
            using Goo;
            $$
            using Bar;
            """);

    [Fact]
    public Task TestBetweenUsings_02()
        => VerifyKeywordAsync(
            """
            global using Goo;
            $$
            using Bar;
            """);

    [Fact]
    public Task TestAfterGlobalBetweenUsings_01()
        => VerifyKeywordAsync(
            """
            global using Goo;
            global $$
            using Bar;
            """);

    [Fact]
    public Task TestAfterGlobalBetweenUsings_02()
        => VerifyKeywordAsync(
            """
            global using Goo;
            global $$
            global using Bar;
            """);

    [Fact]
    public Task TestAfterGlobal()
        => VerifyKeywordAsync(
@"global $$");

    [Fact]
    public Task TestBeforeNamespace()
        => VerifyKeywordAsync(
            """
            $$
            namespace NS
            {}
            """);

    [Fact]
    public Task TestBeforeFileScopedNamespace()
        => VerifyKeywordAsync(
            """
            $$
            namespace NS;
            """);

    [Fact]
    public Task TestBeforeClass()
        => VerifyKeywordAsync(
            """
            $$
            class C1
            {}
            """);

    [Fact]
    public Task TestBeforeAttribute_01()
        => VerifyKeywordAsync(
            """
            $$
            [Call()]
            """);

    [Fact]
    public Task TestBeforeAttribute_02()
        => VerifyKeywordAsync(
            """
            $$
            [assembly: Call()]
            """);

    [Fact]
    public Task TestBeforeNamespaceAfterGlobal()
        => VerifyKeywordAsync(
            """
            global $$
            namespace NS
            {}
            """);

    [Fact]
    public Task TestBeforeClassAfterGlobal()
        => VerifyKeywordAsync(
            """
            global $$
            class C1
            {}
            """);

    [Fact]
    public Task TestBeforeStatementAfterGlobal()
        => VerifyKeywordAsync(
            """
            global $$
            Call();
            """);

    [Fact]
    public Task TestBeforeAttributeAfterGlobal_01()
        => VerifyKeywordAsync(
            """
            global $$
            [Call()]
            """);

    [Fact]
    public Task TestBeforeAttributeAfterGlobal_02()
        => VerifyKeywordAsync(
            """
            global $$
            [assembly: Call()]
            """);

    [Fact]
    public Task TestWithinExtension()
        => VerifyAbsenceAsync(
            """
            static class C
            {
                extension(string s)
                {
                    $$
                }
            }
            """,
            CSharpNextParseOptions,
            CSharpNextScriptParseOptions);
}
