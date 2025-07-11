// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class FinallyKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestAfterTry()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            try {
            } $$
            """));

    [Fact]
    public Task TestAfterTryCatch()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            try {
            } catch {
            } $$
            """));

    [Fact]
    public Task TestNotAfterFinallyBlock()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            try {
            } finally {
            } $$
            """));

    [Fact]
    public Task TestNotInStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestNotAfterBlock()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            if (true)
            {
                Console.WriteLine();
            }
            $$
            """));

    [Fact]
    public Task TestNotAfterFinallyKeyword()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            try {
            } finally $$
            """));

    [Fact]
    public Task TestNotInClass()
        => VerifyAbsenceAsync("""
            class C {
                $$
            }
            """);
}
