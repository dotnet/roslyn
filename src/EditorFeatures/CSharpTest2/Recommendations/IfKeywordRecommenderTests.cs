// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class IfKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAtRoot_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");

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
    public Task TestAfterGlobalVariableDeclaration_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
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
    public Task TestNotInPreprocessor1()
        => VerifyAbsenceAsync(AddInsideMethod(
"#if $$"));

    [Fact]
    public Task TestEmptyStatement()
        => VerifyKeywordAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestAfterHash()
        => VerifyKeywordAsync(
@"#$$");

    [Fact]
    public Task TestAfterHashFollowedBySkippedTokens()
        => VerifyKeywordAsync(
            """
            #$$
            aeu
            """);

    [Fact]
    public Task TestAfterHashAndSpace()
        => VerifyKeywordAsync(
@"# $$");

    [Fact]
    public Task TestInsideMethod()
        => VerifyKeywordAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestBeforeStatement()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            $$
            return true;
            """));

    [Fact]
    public Task TestAfterStatement()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            return true;
            $$
            """));

    [Fact]
    public Task TestAfterBlock()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            if (true) {
            }
            $$
            """));

    [Fact]
    public Task TestNotAfterIf()
        => VerifyAbsenceAsync(AddInsideMethod(
@"if $$"));

    [Fact]
    public Task TestInCase()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (true) {
              case 0:
                $$
            }
            """));

    [Fact]
    public Task TestInCaseBlock()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (true) {
              case 0: {
                $$
              }
            }
            """));

    [Fact]
    public Task TestInDefaultCase()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (true) {
              default:
                $$
            }
            """));

    [Fact]
    public Task TestInDefaultCaseBlock()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (true) {
              default: {
                $$
              }
            }
            """));

    [Fact]
    public Task TestAfterLabel()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            label:
              $$
            """));

    [Fact]
    public Task TestNotAfterDoBlock()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            do {
            }
            $$
            """));

    [Fact]
    public Task TestInActiveRegion1()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            #if true
            $$
            """));

    [Fact]
    public Task TestInActiveRegion2()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            #if true

            $$
            """));

    [Fact]
    public Task TestAfterElse()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            if (goo) {
            } else $$
            """));

    [Fact]
    public Task TestAfterCatch()
        => VerifyAbsenceAsync(AddInsideMethod(
@"try {} catch $$"));

    [Fact]
    public Task TestAfterCatchDeclaration1()
        => VerifyAbsenceAsync(AddInsideMethod(
@"try {} catch (Exception) $$"));

    [Fact]
    public Task TestAfterCatchDeclaration2()
        => VerifyAbsenceAsync(AddInsideMethod(
@"try {} catch (Exception e) $$"));

    [Fact]
    public Task TestAfterCatchDeclarationEmpty()
        => VerifyAbsenceAsync(AddInsideMethod(
@"try {} catch () $$"));

    [Fact]
    public Task TestNotAfterTryBlock()
        => VerifyAbsenceAsync(AddInsideMethod(
@"try {} $$"));
}
