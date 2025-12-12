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
<<<<<<< HEAD
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
=======
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
>>>>>>> upstream/features/collection-expression-arguments
            """
            class C { }
            $$
            """);
<<<<<<< HEAD
    }

    [Fact]
    public async Task TestNotAfterGlobalStatement_Interactive()
    {
        await VerifyAbsenceAsync(SourceCodeKind.Script,
=======

    [Fact]
    public Task TestNotAfterGlobalStatement_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
>>>>>>> upstream/features/collection-expression-arguments
            """
            System.Console.WriteLine();
            $$
            """);
<<<<<<< HEAD
    }

    [Fact]
    public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
    {
        await VerifyAbsenceAsync(SourceCodeKind.Script,
=======

    [Fact]
    public Task TestNotAfterGlobalVariableDeclaration_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
>>>>>>> upstream/features/collection-expression-arguments
            """
            int i = 0;
            $$
            """);
<<<<<<< HEAD
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
=======

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
>>>>>>> upstream/features/collection-expression-arguments
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
<<<<<<< HEAD
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8319")]
    public async Task TestNotAfterMethodReference()
    {
        await VerifyAbsenceAsync(
=======

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8319")]
    public Task TestNotAfterMethodReference()
        => VerifyAbsenceAsync(
>>>>>>> upstream/features/collection-expression-arguments
            """
            using System;

            class C {
                void M() {
                    var v = Console.WriteLine $$
            """);
<<<<<<< HEAD
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8319")]
    public async Task TestNotAfterAnonymousMethod()
    {
        await VerifyAbsenceAsync(
=======

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8319")]
    public Task TestNotAfterAnonymousMethod()
        => VerifyAbsenceAsync(
>>>>>>> upstream/features/collection-expression-arguments
            """
            using System;

            class C {
                void M() {
                    Action a = delegate { } $$
            """);
<<<<<<< HEAD
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8319")]
    public async Task TestNotAfterLambda1()
    {
        await VerifyAbsenceAsync(
=======

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8319")]
    public Task TestNotAfterLambda1()
        => VerifyAbsenceAsync(
>>>>>>> upstream/features/collection-expression-arguments
            """
            using System;

            class C {
                void M() {
                    Action b = (() => 0) $$
            """);
<<<<<<< HEAD
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8319")]
    public async Task TestNotAfterLambda2()
    {
        await VerifyAbsenceAsync(
=======

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8319")]
    public Task TestNotAfterLambda2()
        => VerifyAbsenceAsync(
>>>>>>> upstream/features/collection-expression-arguments
            """
            using System;

            class C {
                void M() {
                    Action b = () => {} $$
            """);
<<<<<<< HEAD
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48573")]
    public async Task TestMissingAfterNumericLiteral()
    {
        await VerifyAbsenceAsync(
=======

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48573")]
    public Task TestMissingAfterNumericLiteral()
        => VerifyAbsenceAsync(
>>>>>>> upstream/features/collection-expression-arguments
            """
            class C
            {
                void M()
                {
                    var x = 1$$
                }
            }
            """);
<<<<<<< HEAD
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48573")]
    public async Task TestMissingAfterNumericLiteralAndDot()
    {
        await VerifyAbsenceAsync(
=======

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48573")]
    public Task TestMissingAfterNumericLiteralAndDot()
        => VerifyAbsenceAsync(
>>>>>>> upstream/features/collection-expression-arguments
            """
            class C
            {
                void M()
                {
                    var x = 1.$$
                }
            }
            """);
<<<<<<< HEAD
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48573")]
    public async Task TestMissingAfterNumericLiteralDotAndSpace()
    {
        await VerifyAbsenceAsync(
=======

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48573")]
    public Task TestMissingAfterNumericLiteralDotAndSpace()
        => VerifyAbsenceAsync(
>>>>>>> upstream/features/collection-expression-arguments
            """
            class C
            {
                void M()
                {
                    var x = 1. $$
                }
            }
            """);
<<<<<<< HEAD
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31367")]
    public async Task TestMissingInCaseClause1()
    {
        await VerifyAbsenceAsync(
=======

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31367")]
    public Task TestMissingInCaseClause1()
        => VerifyAbsenceAsync(
>>>>>>> upstream/features/collection-expression-arguments
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
<<<<<<< HEAD
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31367")]
    public async Task TestMissingInCaseClause2()
    {
        await VerifyAbsenceAsync(
=======

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31367")]
    public Task TestMissingInCaseClause2()
        => VerifyAbsenceAsync(
>>>>>>> upstream/features/collection-expression-arguments
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
<<<<<<< HEAD
    }

    [Fact]
    public async Task TestInCollectionExpression1()
    {
        await VerifyKeywordAsync(
            """
            var v = [$$];
            """);
    }

    [Fact]
    public async Task TestInCollectionExpression2()
    {
        await VerifyKeywordAsync(
            """
            var v = [$$, 1];
            """);
    }

    [Fact]
    public async Task TestInCollectionExpression3()
    {
        await VerifyAbsenceAsync(
            """
            var v = [1, $$];
            """);
    }

    [Fact]
    public async Task TestInCollectionExpression4()
    {
        await VerifyAbsenceAsync(
            """
            var v = [1, $$
            """);
    }

    [Fact]
    public async Task TestInCollectionExpression5()
    {
        await VerifyKeywordAsync(
            """
            void M()
            {
                Goo([$$
            }
            """);
    }

    [Fact]
    public async Task TestInCollectionExpression6()
    {
        await VerifyKeywordAsync(
            """
            void M()
            {
                Goo([$$]);
            }
            """);
    }
=======

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
>>>>>>> upstream/features/collection-expression-arguments
}
