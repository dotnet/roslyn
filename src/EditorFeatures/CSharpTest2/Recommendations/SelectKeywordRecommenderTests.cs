// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class SelectKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestNotAtEndOfPreviousClause()
        => VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y$$"));

    [Fact]
    public Task TestNewClause()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
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
    public Task TestAfterOrderByExpr()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      orderby x
                      $$
            """));

    [Fact]
    public Task TestAfterOrderByAscendingExpr()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      orderby x ascending
                      $$
            """));

    [Fact]
    public Task TestAfterOrderByDescendingExpr()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      orderby x descending
                      $$
            """));

    [Fact]
    public Task TestBetweenClauses()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      $$
                      from z in w
            """));

    [Fact]
    public Task TestNotAfterSelect()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      select $$
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
