// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class OnKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestAfterJoinInExpr1()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a in e $$
            """));

    [Fact]
    public Task TestAfterJoinInExpr2()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a.b c in e $$
            """));

    [Fact]
    public Task TestNotAfterOn1()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a.b c in e on $$
            """));

    [Fact]
    public Task TestNotAfterOn2()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a.b c in e on o$$
            """));

    [Fact]
    public Task TestNotAfterOn3()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a.b c in e on o1 $$
            """));

    [Fact]
    public Task TestNotAfterOn4()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a.b c in e on o1 e$$
            """));

    [Fact]
    public Task TestNotAfterOn5()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a.b c in e on o1 equals $$
            """));

    [Fact]
    public Task TestNotAfterOn6()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a.b c in e on o1 equals o$$
            """));

    [Fact]
    public Task TestNotAfterIn()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a.b c in $$
            """));
}
