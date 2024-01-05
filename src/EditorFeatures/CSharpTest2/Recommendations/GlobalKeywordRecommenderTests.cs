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
    public class GlobalKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestInMethodBody()
            => await VerifyKeywordAsync(AddInsideMethod(@"$$"));

        [Fact]
        public async Task TestInClassDeclaration()
        {
            await VerifyKeywordAsync("""
                namespace goo
                {
                    class bar
                    {
                        $$
                    }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543628")]
        public async Task TestNotInEnumDeclaration()
            => await VerifyAbsenceAsync(@"enum Goo { $$ }");

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
        public async Task TestNotInObjectInitializerMemberContext()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    public int x, y;
                    void M()
                    {
                        var c = new C { x = 2, y = 3, $$
                """);
        }

        [Fact]
        public async Task TestAfterConstInMemberContext()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    const $$
                """);
        }

        [Fact]
        public async Task TestAfterRefInMemberContext()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    ref $$
                """);
        }

        [Fact]
        public async Task TestAfterRefReadonlyInMemberContext()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    ref readonly $$
                """);
        }

        [Fact]
        public async Task TestAfterConstInStatementContext()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"const $$"));
        }

        [Fact]
        public async Task TestAfterRefInStatementContext()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$"));
        }

        [Fact]
        public async Task TestAfterRefReadonlyInStatementContext()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$"));
        }

        [Fact]
        public async Task TestAfterConstLocalDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"const $$ int local;"));
        }

        [Fact]
        public async Task TestAfterRefLocalDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int local;"));
        }

        [Fact]
        public async Task TestAfterRefReadonlyLocalDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$ int local;"));
        }

        [Fact]
        public async Task TestAfterRefLocalFunction()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int Function();"));
        }

        [Fact]
        public async Task TestAfterRefReadonlyLocalFunction()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$ int Function();"));
        }

        [Fact]
        public async Task TestAfterRefExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$"));
        }

        [Fact]
        public async Task TestInFunctionPointerType()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<$$
                """);
        }

        [Fact]
        public async Task TestInFunctionPointerTypeAfterComma()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<int, $$
                """);
        }

        [Fact]
        public async Task TestInFunctionPointerTypeAfterModifier()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<ref $$
                """);
        }

        [Fact]
        public async Task TestNotAfterDelegateAsterisk()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    delegate*$$
                """);
        }

        [Fact]
        public async Task TestInCompilationUnit()
        {
            await VerifyKeywordAsync("""
                $$
                """);
        }

        [Fact]
        public async Task TestAfterExtern()
        {
            await VerifyKeywordAsync(
                """
                extern alias goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterPreviousUsing()
        {
            await VerifyKeywordAsync(
                """
                using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterPreviousGlobalUsing()
        {
            await VerifyKeywordAsync(
                """
                global using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestBeforeUsing()
        {
            await VerifyKeywordAsync(
                """
                $$
                using Goo;
                """);
        }

        [Fact]
        public async Task TestBeforeGlobalUsing()
        {
            await VerifyKeywordAsync(
                """
                $$
                global using Goo;
                """);
        }

        [Fact]
        public async Task TestAfterUsingAlias()
        {
            await VerifyKeywordAsync(
                """
                using Goo = Bar;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalUsingAlias()
        {
            await VerifyKeywordAsync(
                """
                using Goo = Bar;
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalKeyword()
        {
            await VerifyAbsenceAsync("""
                global $$
                """);
        }

        [Fact]
        public async Task TestNotAfterUsingKeyword()
        {
            await VerifyAbsenceAsync("""
                using $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalUsingKeyword()
        {
            await VerifyAbsenceAsync("""
                global using $$
                """);
        }

        [Fact]
        public async Task TestNotBeforeExtern()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                $$
                extern alias Goo;
                """);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeExtern_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                $$
                extern alias Goo;
                """);
        }

        [Fact]
        public async Task TestBetweenGlobalUsings_01()
        {
            await VerifyKeywordAsync(
                """
                global using Goo;
                $$
                global using Bar;
                """);
        }

        [Fact]
        public async Task TestBetweenUsings_02()
        {
            await VerifyKeywordAsync(
                """
                global using Goo;
                $$
                using Bar;
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalBetweenGlobalUsings_01()
        {
            await VerifyAbsenceAsync(
                """
                global using Goo;
                global $$
                global using Bar;
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalBetweenUsings_02()
        {
            await VerifyAbsenceAsync(
                """
                global using Goo;
                global $$
                using Bar;
                """);
        }

        [Fact]
        public async Task TestBeforeNamespace()
        {
            await VerifyKeywordAsync(
                """
                $$
                namespace NS
                {}
                """);
        }

        [Fact]
        public async Task TestBeforeClass()
        {
            await VerifyKeywordAsync(
                """
                $$
                class C1
                {}
                """);
        }

        [Fact]
        public async Task TestBeforeStatement()
        {
            await VerifyKeywordAsync(
                """
                $$
                Call();
                """);
        }

        [Fact]
        public async Task TestBeforeAttribute_01()
        {
            await VerifyKeywordAsync(
                """
                $$
                [Call()]
                """);
        }

        [Fact]
        public async Task TestBeforeAttribute_02()
        {
            await VerifyKeywordAsync(
                """
                $$
                [assembly: Call()]
                """);
        }

        [Fact]
        public async Task TestAfterScoped()
        {
            await VerifyKeywordAsync("scoped $$");
            await VerifyKeywordAsync(AddInsideMethod("scoped $$"));
        }

        [Fact]
        public async Task TestInEnumBaseList()
        {
            await VerifyKeywordAsync("enum E : $$");
        }

        #region Collection expressions

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_BeforeFirstElementToVar()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var x = [$$
                """));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_BeforeFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [$$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_AfterFirstElementToVar()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var x = [new object(), $$
                """));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_AfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_SpreadBeforeFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [.. $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_SpreadAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, .. $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenAtFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, ($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenSpreadAtFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [.. ($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenSpreadAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, .. ($$
                }
                """);
        }

        #endregion
    }
}
