// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class GlobalKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public async Task TestInMethodBody()
        => await VerifyKeywordAsync(AddInsideMethod(@"$$"));

    [Fact]
    public Task TestInClassDeclaration()
        => VerifyKeywordAsync("""
            namespace goo
            {
                class bar
                {
                    $$
                }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543628")]
    public async Task TestNotInEnumDeclaration()
        => await VerifyAbsenceAsync(@"enum Goo { $$ }");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
    public Task TestNotInObjectInitializerMemberContext()
        => VerifyAbsenceAsync("""
            class C
            {
                public int x, y;
                void M()
                {
                    var c = new C { x = 2, y = 3, $$
            """);

    [Fact]
    public Task TestAfterConstInMemberContext()
        => VerifyKeywordAsync(
            """
            class C {
                const $$
            """);

    [Fact]
    public Task TestAfterRefInMemberContext()
        => VerifyKeywordAsync(
            """
            class C {
                ref $$
            """);

    [Fact]
    public Task TestAfterRefReadonlyInMemberContext()
        => VerifyKeywordAsync(
            """
            class C {
                ref readonly $$
            """);

    [Fact]
    public Task TestAfterConstInStatementContext()
        => VerifyKeywordAsync(AddInsideMethod(
@"const $$"));

    [Fact]
    public Task TestAfterRefInStatementContext()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$"));

    [Fact]
    public Task TestAfterRefReadonlyInStatementContext()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$"));

    [Fact]
    public Task TestAfterConstLocalDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"const $$ int local;"));

    [Fact]
    public Task TestAfterRefLocalDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int local;"));

    [Fact]
    public Task TestAfterRefReadonlyLocalDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$ int local;"));

    [Fact]
    public Task TestAfterRefLocalFunction()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int Function();"));

    [Fact]
    public Task TestAfterRefReadonlyLocalFunction()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$ int Function();"));

    [Fact]
    public Task TestAfterRefExpression()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$"));

    [Fact]
    public Task TestInFunctionPointerType()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<$$
            """);

    [Fact]
    public Task TestInFunctionPointerTypeAfterComma()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<int, $$
            """);

    [Fact]
    public Task TestInFunctionPointerTypeAfterModifier()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<ref $$
            """);

    [Fact]
    public Task TestNotAfterDelegateAsterisk()
        => VerifyAbsenceAsync("""
            class C
            {
                delegate*$$
            """);

    [Fact]
    public Task TestInCompilationUnit()
        => VerifyKeywordAsync("""
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
    public Task TestBeforeUsing()
        => VerifyKeywordAsync(
            """
            $$
            using Goo;
            """);

    [Fact]
    public Task TestBeforeGlobalUsing()
        => VerifyKeywordAsync(
            """
            $$
            global using Goo;
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
            using Goo = Bar;
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalKeyword()
        => VerifyAbsenceAsync("""
            global $$
            """);

    [Fact]
    public Task TestNotAfterUsingKeyword()
        => VerifyAbsenceAsync("""
            using $$
            """);

    [Fact]
    public Task TestNotAfterGlobalUsingKeyword()
        => VerifyAbsenceAsync("""
            global using $$
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
    public Task TestBetweenGlobalUsings_01()
        => VerifyKeywordAsync(
            """
            global using Goo;
            $$
            global using Bar;
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
    public Task TestNotAfterGlobalBetweenGlobalUsings_01()
        => VerifyAbsenceAsync(
            """
            global using Goo;
            global $$
            global using Bar;
            """);

    [Fact]
    public Task TestNotAfterGlobalBetweenUsings_02()
        => VerifyAbsenceAsync(
            """
            global using Goo;
            global $$
            using Bar;
            """);

    [Fact]
    public Task TestBeforeNamespace()
        => VerifyKeywordAsync(
            """
            $$
            namespace NS
            {}
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
    public Task TestBeforeStatement()
        => VerifyKeywordAsync(
            """
            $$
            Call();
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
    public async Task TestAfterScoped()
    {
        await VerifyKeywordAsync("scoped $$");
        await VerifyKeywordAsync(AddInsideMethod("scoped $$"));
    }

    [Fact]
    public Task TestInEnumBaseList()
        => VerifyKeywordAsync("enum E : $$");

    #region Collection expressions

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_BeforeFirstElementToVar()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var x = [$$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_BeforeFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [$$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_AfterFirstElementToVar()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var x = [new object(), $$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_AfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_SpreadBeforeFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [.. $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_SpreadAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, .. $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenAtFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, ($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenSpreadAtFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [.. ($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenSpreadAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, .. ($$
            }
            """);

    #endregion

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
