// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ByKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotAtRoot()
        => VerifyAbsenceAsync(
@"$$", options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotAfterClass_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalStatement()
        => VerifyAbsenceAsync(
            """
            System.Console.WriteLine();
            $$
            """, options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotAfterGlobalVariableDeclaration()
        => VerifyAbsenceAsync(
            """
            int i = 0;
            $$
            """, options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
@"using Goo = $$");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
@"global using Goo = $$");

    [Theory, CombinatorialData]
    public Task TestNotInEmptyStatement(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterGroupExpr(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      group a $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotAfterGroup(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      group $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotAfterBy(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      group a by $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
}
