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
    public class FromKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestInEmptySpace()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var v = $$"));
        }

        [Fact]
        public async Task TestAfterIdentifier()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var v = a$$"));
        }

        [Fact]
        public async Task TestNestedInQueryExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in $$"));
        }

        [Fact]
        public async Task TestAfterFrom()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var v = from x in y
                          $$
                """));
        }

        [Fact]
        public async Task TestAfterPreviousClause()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var v = from x in y
                          where x > y
                          $$
                """));
        }

        [Fact]
        public async Task TestAfterPreviousContinuationClause()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var v = from x in y
                          group x by y into g
                          $$
                """));
        }

        [Fact]
        public async Task TestNotAfterFrom1()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var v = from $$"));
        }

        [Fact]
        public async Task TestNotAfterFrom2()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var v = from a in y
                          from $$
                """));
        }

        [Fact]
        public async Task TestBetweenClauses()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var v = from x in y
                          $$
                          from z in w
                """));
        }

        [Fact]
        public async Task TestContinueInSelect()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var v = from x in y
                          select $$
                """));
        }

        [Fact]
        public async Task TestBetweenTokens()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var v =$$;"));
        }

        [Fact]
        public async Task TestNotInDeclaration()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"int $$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        public async Task TestNotInEnumMemberInitializer1()
        {
            await VerifyAbsenceAsync(
                """
                enum E {
                    a = $$
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        public async Task TestNotInConstMemberInitializer1()
        {
            await VerifyAbsenceAsync(
                """
                class E {
                    const int a = $$
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        public async Task TestInMemberInitializer1()
        {
            await VerifyKeywordAsync(
                """
                class E {
                    int a = $$
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        public async Task TestNotInTypeOf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"typeof($$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        public async Task TestNotInDefault()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"default($$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        public async Task TestNotInSizeOf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"sizeof($$"));
        }

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
        public async Task TestNotAfterOutInArgument()
        {
            var experimentalFeatures = new System.Collections.Generic.Dictionary<string, string>(); // no experimental features to enable
            await VerifyAbsenceAsync("""
                class C
                {
                    void M(out int x) { x = 42; }

                    void N()
                    {
                        M(out var $$
                """, options: Options.Regular.WithFeatures(experimentalFeatures), scriptOptions: Options.Script.WithFeatures(experimentalFeatures));
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
