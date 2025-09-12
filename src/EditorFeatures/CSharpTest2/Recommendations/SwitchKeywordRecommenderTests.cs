// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class SwitchKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestEmptyStatement()
        => VerifyKeywordAsync(AddInsideMethod(
            @"$$"));

    [Fact]
    public Task TestBeforeStatement()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            $$
            return true;
            """));

    [Fact]
    public Task TestAfterStatement()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            return true;
            $$
            """));

    [Fact]
    public Task TestAfterBlock()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            if (true) {
            }
            $$
            """));

    [Fact]
    public Task TestInsideSwitchBlock()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (E) {
              case 0:
                $$
            """));

    [Fact]
    public Task TestNotAfterSwitch1()
        => VerifyAbsenceAsync(AddInsideMethod(
            @"switch $$"));

    [Fact]
    public async Task TestAfterExpression()
        => await VerifyKeywordAsync(AddInsideMethod(@"_ = expr $$"));

    [Fact]
    public Task TestAfterExpression_InMethodWithArrowBody()
        => VerifyKeywordAsync("""
            class C
            {
                bool M() => this $$
            }
            """);

    [Fact]
    public async Task TestAfterForeachVar()
        => await VerifyAbsenceAsync(AddInsideMethod(@"foreach (var $$)"));

    [Fact]
    public async Task TestAfterTuple()
        => await VerifyKeywordAsync(AddInsideMethod(@"_ = (expr, expr) $$"));

    [Fact]
    public Task TestNotAfterSwitch2()
        => VerifyAbsenceAsync(AddInsideMethod(
            @"switch ($$"));

    [Fact]
    public Task TestNotInClass()
        => VerifyAbsenceAsync("""
            class C
            {
              $$
            }
            """);

    [Fact]
    public Task TestAfterSwitch()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
               default:
            }
            $$
            """));

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
