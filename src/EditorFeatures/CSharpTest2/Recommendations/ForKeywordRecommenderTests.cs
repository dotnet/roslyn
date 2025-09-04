﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ForKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestEmptyStatement()
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
    public Task TestInsideFor()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            for (;;)
                 $$
            """));

    [Fact]
    public Task TestInsideForInsideFor()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            for (;;)
                 for (;;)
                    $$
            """));

    [Fact]
    public Task TestInsideForBlock()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            for (;;) {
                 $$
            """));

    [Fact]
    public Task TestNotAfterFor1()
        => VerifyAbsenceAsync(AddInsideMethod(
@"for $$"));

    [Fact]
    public Task TestNotAfterFor2()
        => VerifyAbsenceAsync(AddInsideMethod(
@"for ($$"));

    [Fact]
    public Task TestNotAfterFor3()
        => VerifyAbsenceAsync(AddInsideMethod(
@"for (;$$"));

    [Fact]
    public Task TestNotAfterFor4()
        => VerifyAbsenceAsync(AddInsideMethod(
@"for (;;$$"));

    [Fact]
    public Task TestNotInClass()
        => VerifyAbsenceAsync("""
            class C
            {
              $$
            }
            """);
}
