// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class FromKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAtRoot_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public Task TestAfterClass_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
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
    public Task TestInEmptySpace()
        => VerifyKeywordAsync(AddInsideMethod(
@"var v = $$"));

    [Fact]
    public Task TestAfterIdentifier()
        => VerifyKeywordAsync(AddInsideMethod(
@"var v = a$$"));

    [Fact]
    public Task TestNestedInQueryExpression()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in $$"));

    [Fact]
    public Task TestAfterFrom()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var v = from x in y
                      $$
            """));

    [Fact]
    public Task TestAfterPreviousClause()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var v = from x in y
                      where x > y
                      $$
            """));

    [Fact]
    public Task TestAfterPreviousContinuationClause()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var v = from x in y
                      group x by y into g
                      $$
            """));

    [Fact]
    public Task TestNotAfterFrom1()
        => VerifyAbsenceAsync(AddInsideMethod(
@"var v = from $$"));

    [Fact]
    public Task TestNotAfterFrom2()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var v = from a in y
                      from $$
            """));

    [Fact]
    public Task TestBetweenClauses()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var v = from x in y
                      $$
                      from z in w
            """));

    [Fact]
    public Task TestContinueInSelect()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var v = from x in y
                      select $$
            """));

    [Fact]
    public Task TestBetweenTokens()
        => VerifyKeywordAsync(AddInsideMethod(
@"var v =$$;"));

    [Fact]
    public Task TestNotInDeclaration()
        => VerifyAbsenceAsync(AddInsideMethod(
@"int $$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestNotInEnumMemberInitializer1()
        => VerifyAbsenceAsync(
            """
            enum E {
                a = $$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestNotInConstMemberInitializer1()
        => VerifyAbsenceAsync(
            """
            class E {
                const int a = $$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestInMemberInitializer1()
        => VerifyKeywordAsync(
            """
            class E {
                int a = $$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestNotInTypeOf()
        => VerifyAbsenceAsync(AddInsideMethod(
@"typeof($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestNotInDefault()
        => VerifyAbsenceAsync(AddInsideMethod(
@"default($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestNotInSizeOf()
        => VerifyAbsenceAsync(AddInsideMethod(
@"sizeof($$"));

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
}
