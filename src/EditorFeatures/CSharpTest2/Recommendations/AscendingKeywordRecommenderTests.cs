// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class AscendingKeywordRecommenderTests : KeywordRecommenderTests
{
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
    public Task TestAfterOrderByExpr()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      orderby x $$
            """));

    [Fact]
    public Task TestAfterSecondExpr()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      orderby x, y $$
            """));

    [Fact]
    public Task TestBetweenExprs()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      orderby x, y $$, z
            """));

    [Fact]
    public Task TestNotAfterDot()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      orderby x.$$
            """));

    [Fact]
    public Task TestNotAfterComma()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      orderby x, $$
            """));
}
