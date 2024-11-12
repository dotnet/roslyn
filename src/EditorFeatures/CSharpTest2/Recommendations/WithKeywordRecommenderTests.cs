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
    public class WithKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestNotAfterWith()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = goo with $$"));
        }

        [Fact]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
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
        public async Task TestAfterExpr()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = goo $$"));
        }

        [Fact]
        public async Task TestAfterDottedName()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = goo.Current $$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543041")]
        public async Task TestNotAfterVarInForLoop()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"for (var $$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        public async Task TestNotBeforeFirstStringHole()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var x = "\{0}$$\{1}\{2}"
                """));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        public async Task TestNotBetweenStringHoles()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var x = "\{0}\{1}$$\{2}"
                """));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        public async Task TestNotAfterStringHoles()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var x = "\{0}\{1}\{2}$$"
                """));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        public async Task TestAfterLastStringHole()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var x = ""\{0}\{1}\{2}"" $$"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1736")]
        public async Task TestNotWithinNumericLiteral()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var x = .$$0;"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
        public async Task TestNotAfterAsync()
        {
            await VerifyAbsenceAsync(
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
