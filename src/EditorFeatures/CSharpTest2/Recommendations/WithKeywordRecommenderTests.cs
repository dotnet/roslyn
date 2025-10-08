// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class WithKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotAfterWith()
        => VerifyAbsenceAsync(AddInsideMethod(
@"var q = goo with $$"));

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
    public Task TestAfterExpr()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = goo $$"));

    [Fact]
    public Task TestAfterDottedName()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = goo.Current $$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543041")]
    public Task TestNotAfterVarInForLoop()
        => VerifyAbsenceAsync(AddInsideMethod(
@"for (var $$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
    public Task TestNotBeforeFirstStringHole()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var x = "\{0}$$\{1}\{2}"
            """));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
    public Task TestNotBetweenStringHoles()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var x = "\{0}\{1}$$\{2}"
            """));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
    public Task TestNotAfterStringHoles()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var x = "\{0}\{1}\{2}$$"
            """));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
    public Task TestAfterLastStringHole()
        => VerifyKeywordAsync(AddInsideMethod(
@"var x = ""\{0}\{1}\{2}"" $$"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1736")]
    public Task TestNotWithinNumericLiteral()
        => VerifyAbsenceAsync(AddInsideMethod(
@"var x = .$$0;"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public Task TestNotAfterAsync()
        => VerifyAbsenceAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(async $$
                }

                void Bar(Func<int, string> f)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8319")]
    public Task TestNotAfterMethodReference()
        => VerifyAbsenceAsync(
            """
            using System;

            class C {
                void M() {
                    var v = Console.WriteLine $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8319")]
    public Task TestNotAfterAnonymousMethod()
        => VerifyAbsenceAsync(
            """
            using System;

            class C {
                void M() {
                    Action a = delegate { } $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8319")]
    public Task TestNotAfterLambda1()
        => VerifyAbsenceAsync(
            """
            using System;

            class C {
                void M() {
                    Action b = (() => 0) $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8319")]
    public Task TestNotAfterLambda2()
        => VerifyAbsenceAsync(
            """
            using System;

            class C {
                void M() {
                    Action b = () => {} $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48573")]
    public Task TestMissingAfterNumericLiteral()
        => VerifyAbsenceAsync(
            """
            class C
            {
                void M()
                {
                    var x = 1$$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48573")]
    public Task TestMissingAfterNumericLiteralAndDot()
        => VerifyAbsenceAsync(
            """
            class C
            {
                void M()
                {
                    var x = 1.$$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48573")]
    public Task TestMissingAfterNumericLiteralDotAndSpace()
        => VerifyAbsenceAsync(
            """
            class C
            {
                void M()
                {
                    var x = 1. $$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31367")]
    public Task TestMissingInCaseClause1()
        => VerifyAbsenceAsync(
            """
            class A
            {

            }

            class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case A $$
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31367")]
    public Task TestMissingInCaseClause2()
        => VerifyAbsenceAsync(
            """
            namespace N
            {
                class A
                {

                }
            }

            class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case N.A $$
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78800")]
    public Task TestAfterReturnExpression()
        => VerifyKeywordAsync(
            """
            class C
            {
                public static string EvaluateRangeVariable()
                {
                    return RandomValue() $$
                }

                public int RandomValue() => 0;
            }
            """);
}
