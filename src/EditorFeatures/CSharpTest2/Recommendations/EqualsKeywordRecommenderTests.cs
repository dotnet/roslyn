// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class EqualsKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestAfterJoinLeftExpr()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a in e on o1 $$
            """));

    [Fact]
    public Task TestAfterJoinLeftExpr_NotAfterEquals()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a.b c in o1 equals $$
            """));

    [Fact]
    public Task TestAfterJoinLeftExpr_NotAfterIn1()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a.b c in $$
            """));

    [Fact]
    public Task TestAfterJoinLeftExpr_NotAfterIn2()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a.b c in y $$
            """));

    [Fact]
    public Task TestAfterJoinLeftExpr_NotAfterIn3()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a.b c in y on $$
            """));
}
