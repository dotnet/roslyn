// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class IntoKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestNotInSelectMemberExpressionOnlyADot()
        => VerifyAbsenceAsync(AddInsideMethod(
@"var y = from x in new [] { 1,2,3 } select x.$$"));
    [Fact]
    public Task TestNotInSelectMemberExpression()
        => VerifyAbsenceAsync(AddInsideMethod(
@"var y = from x in new [] { 1,2,3 } select x.i$$"));
    [Fact]
    public Task TestAfterJoinRightExpr()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a in e on o1 equals o2 $$
            """));

    [Fact]
    public Task TestAfterJoinRightExpr_NotAfterInto()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a.b c in o1 equals o2 into $$
            """));

    [Fact]
    public Task TestNotAfterEquals()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a.b c in o1 equals $$
            """));

    [Fact]
    public Task TestAfterSelectClause()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      select z
                      $$
            """));

    [Fact]
    public Task TestAfterSelectClauseWithMemberExpression()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      select z.i
                      $$
            """));

    [Fact]
    public Task TestAfterSelectClause_NotAfterInto()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      select z
                      into $$
            """));

    [Fact]
    public Task TestAfterGroupClause()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      group z by w
                      $$
            """));

    [Fact]
    public Task TestAfterGroupClause_NotAfterInto()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      group z by w
                      into $$
            """));

    [Fact]
    public Task TestNotAfterSelect()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      select $$
            """));

    [Fact]
    public Task TestNotAfterGroupKeyword()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      group $$
            """));

    [Fact]
    public Task TestNotAfterGroupExpression()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      group x $$
            """));

    [Fact]
    public Task TestNotAfterGroupBy()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      group x by $$
            """));
}
