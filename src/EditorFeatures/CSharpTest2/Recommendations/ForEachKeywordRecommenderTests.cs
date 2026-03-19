// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ForEachKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAtRoot_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public Task TestAtRoot_TopLevelStatement()
        => VerifyKeywordAsync(
@"$$", options: CSharp9ParseOptions);

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
    public Task TestAfterStatement_TopLevelStatement()
        => VerifyKeywordAsync(
            """
            System.Console.WriteLine();
            $$
            """, options: CSharp9ParseOptions);

    [Fact]
    public Task TestAfterGlobalVariableDeclaration_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
            """
            int i = 0;
            $$
            """);

    [Fact]
    public Task TestAfterVariableDeclaration_TopLevelStatement()
        => VerifyKeywordAsync(
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
    public Task TestEmptyStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterAwait(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"await $$", isAsync: true, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestBeforeStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            $$
            return 0;
            """, returnType: "int", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            return true;
            $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterBlock(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            if (true) {
            }
            $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInsideForEach(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            foreach (var v in c)
                 $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInsideForEachInsideForEach(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            foreach (var v in c)
                 foreach (var v in c)
                    $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInsideForEachBlock(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            foreach (var v in c) {
                 $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotAfterForEach1(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"foreach $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotAfterForEach2(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"foreach ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotAfterForEach3(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotAfterForEach4(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var v $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotAfterForEach5(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var v in $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotAfterForEach6(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var v in c $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotInClass()
        => VerifyAbsenceAsync("""
            class C
            {
              $$
            }
            """);
}
