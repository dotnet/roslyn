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
    public class SwitchKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestBeforeStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                $$
                return true;
                """));
        }

        [Fact]
        public async Task TestAfterStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                return true;
                $$
                """));
        }

        [Fact]
        public async Task TestAfterBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                if (true) {
                }
                $$
                """));
        }

        [Fact]
        public async Task TestInsideSwitchBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (E) {
                  case 0:
                    $$
                """));
        }

        [Fact]
        public async Task TestNotAfterSwitch1()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"switch $$"));
        }

        [Fact]
        public async Task TestAfterExpression()
            => await VerifyKeywordAsync(AddInsideMethod(@"_ = expr $$"));

        [Fact]
        public async Task TestAfterExpression_InMethodWithArrowBody()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    bool M() => this $$
                }
                """);
        }

        [Fact]
        public async Task TestAfterForeachVar()
            => await VerifyAbsenceAsync(AddInsideMethod(@"foreach (var $$)"));

        [Fact]
        public async Task TestAfterTuple()
            => await VerifyKeywordAsync(AddInsideMethod(@"_ = (expr, expr) $$"));

        [Fact]
        public async Task TestNotAfterSwitch2()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"switch ($$"));
        }

        [Fact]
        public async Task TestNotInClass()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                  $$
                }
                """);
        }

        [Fact]
        public async Task TestAfterSwitch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                   default:
                }
                $$
                """));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8319")]
        public async Task TestNotAfterMethodReference()
        {
            await VerifyAbsenceAsync(
                """
                using System;

                class C {
                    void M() {
                        var v = Console.WriteLine $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8319")]
        public async Task TestNotAfterAnonymousMethod()
        {
            await VerifyAbsenceAsync(
                """
                using System;

                class C {
                    void M() {
                        Action a = delegate { } $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8319")]
        public async Task TestNotAfterLambda1()
        {
            await VerifyAbsenceAsync(
                """
                using System;

                class C {
                    void M() {
                        Action b = (() => 0) $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8319")]
        public async Task TestNotAfterLambda2()
        {
            await VerifyAbsenceAsync(
                """
                using System;

                class C {
                    void M() {
                        Action b = () => {} $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48573")]
        public async Task TestMissingAfterNumericLiteral()
        {
            await VerifyAbsenceAsync(
                """
                class C
                {
                    void M()
                    {
                        var x = 1$$
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48573")]
        public async Task TestMissingAfterNumericLiteralAndDot()
        {
            await VerifyAbsenceAsync(
                """
                class C
                {
                    void M()
                    {
                        var x = 1.$$
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48573")]
        public async Task TestMissingAfterNumericLiteralDotAndSpace()
        {
            await VerifyAbsenceAsync(
                """
                class C
                {
                    void M()
                    {
                        var x = 1. $$
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31367")]
        public async Task TestMissingInCaseClause1()
        {
            await VerifyAbsenceAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31367")]
        public async Task TestMissingInCaseClause2()
        {
            await VerifyAbsenceAsync(
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
        }
    }
}
